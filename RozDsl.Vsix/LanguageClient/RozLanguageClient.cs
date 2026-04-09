using Microsoft.VisualStudio.LanguageServer.Client;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace RozDsl.Vsix.LanguageClient
{
    [Export(typeof(ILanguageClient))]
    [ContentType("roz")]
    internal sealed class RozLanguageClient : ILanguageClient
    {
        private AsyncEventHandler<EventArgs> _startAsync;
        private AsyncEventHandler<EventArgs> _stopAsync;

        public string Name
        {
            get { return "RozDsl Language Client"; }
        }

        public IEnumerable<string> ConfigurationSections
        {
            get { return null; }
        }

        public object InitializationOptions
        {
            get { return null; }
        }

        public IEnumerable<string> FilesToWatch
        {
            get { return null; }
        }

        public bool ShowNotificationOnInitializeFailed
        {
            get { return true; }
        }

        public object CustomMessageTarget
        {
            get { return null; }
        }

        public event AsyncEventHandler<EventArgs> StartAsync
        {
            add { _startAsync += value; }
            remove { _startAsync -= value; }
        }

        public event AsyncEventHandler<EventArgs> StopAsync
        {
            add { _stopAsync += value; }
            remove { _stopAsync -= value; }
        }

        public Task<Connection> ActivateAsync(CancellationToken token)
        {
            try
            {
                var assemblyPath = Assembly.GetExecutingAssembly().Location;
                var extensionFolder = Path.GetDirectoryName(assemblyPath);
                var serverPath = Path.Combine(extensionFolder, "Roz.Lsp.exe");

                VsixLogger.Info("ActivateAsync called");
                VsixLogger.Info("VSIX assembly path: " + assemblyPath);
                VsixLogger.Info("Extension folder: " + extensionFolder);
                VsixLogger.Info("Expected server path: " + serverPath);

                if (!File.Exists(serverPath))
                {
                    throw new FileNotFoundException("Roz.Lsp.exe was not found.", serverPath);
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = serverPath,
                    Arguments = "",
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                var process = new Process();
                process.StartInfo = startInfo;
                process.EnableRaisingEvents = true;
                process.Exited += delegate
                {
                    try
                    {
                        VsixLogger.Info("Server process exited. Exit code: " + process.ExitCode);
                    }
                    catch (Exception ex)
                    {
                        VsixLogger.Error(ex, "Failed while handling server process exit");
                    }
                };

                var started = process.Start();
                VsixLogger.Info("Server process start result: " + started);

                try
                {
                    VsixLogger.Info("Server process id: " + process.Id);
                }
                catch (Exception ex)
                {
                    VsixLogger.Error(ex, "Failed to read server process id");
                }

                Task.Run(() =>
                {
                    try
                    {
                        string line;
                        while ((line = process.StandardError.ReadLine()) != null)
                        {
                            VsixLogger.Info("LSP STDERR: " + line);
                        }
                    }
                    catch (Exception ex)
                    {
                        VsixLogger.Error(ex, "Failed while reading server stderr");
                    }
                });

                var connection = new Connection(
                    process.StandardOutput.BaseStream,
                    process.StandardInput.BaseStream);

                VsixLogger.Info("Connection created successfully");

                return Task.FromResult(connection);
            }
            catch (Exception ex)
            {
                VsixLogger.Error(ex, "ActivateAsync failed");
                throw;
            }
        }

        public Task OnLoadedAsync()
        {
            VsixLogger.Info("OnLoadedAsync called");

            if (_startAsync != null)
            {
                VsixLogger.Info("Invoking StartAsync");
                return _startAsync.InvokeAsync(this, EventArgs.Empty);
            }

            VsixLogger.Info("StartAsync delegate is null");
            return Task.CompletedTask;
        }

        public Task OnServerInitializedAsync()
        {
            VsixLogger.Info("OnServerInitializedAsync called");
            return Task.CompletedTask;
        }

        public Task OnServerInitializeFailedAsync(Exception e)
        {
            VsixLogger.Error(e, "OnServerInitializeFailedAsync(Exception) called");
            return Task.CompletedTask;
        }

        public Task<InitializationFailureContext> OnServerInitializeFailedAsync(
            ILanguageClientInitializationInfo initializationInfo)
        {
            VsixLogger.Error("OnServerInitializeFailedAsync(ILanguageClientInitializationInfo) called");
            return Task.FromResult<InitializationFailureContext>(null);
        }
    }
}