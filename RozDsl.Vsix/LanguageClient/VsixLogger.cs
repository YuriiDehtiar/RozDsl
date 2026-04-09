using System;
using System.IO;

namespace RozDsl.Vsix.LanguageClient
{
    internal static class VsixLogger
    {
        private static readonly object Sync = new object();

        private static string LogDirectory
        {
            get
            {
                return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "RozDsl",
                    "Logs");
            }
        }

        private static string LogFilePath
        {
            get
            {
                return Path.Combine(LogDirectory, "roz-vsix.log");
            }
        }

        public static void Info(string message)
        {
            Write("INFO", message);
        }

        public static void Error(string message)
        {
            Write("ERROR", message);
        }

        public static void Error(Exception ex, string message)
        {
            Write("ERROR", message + Environment.NewLine + ex);
        }

        private static void Write(string level, string message)
        {
            try
            {
                lock (Sync)
                {
                    Directory.CreateDirectory(LogDirectory);

                    var line =
                        "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] [" + level + "] " + message + Environment.NewLine;

                    File.AppendAllText(LogFilePath, line);
                }
            }
            catch
            {
            }
        }
    }
}