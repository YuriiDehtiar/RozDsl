using System;
using System.IO;

namespace Roz.Lsp;

internal static class FileLogger
{
    private static readonly object Sync = new();

    private static readonly string LogDirectory =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RozDsl",
            "Logs");

    private static readonly string LogFilePath =
        Path.Combine(LogDirectory, "roz-lsp.log");

    public static void Info(string message)
    {
        Write("INFO", message);
    }

    public static void Error(string message)
    {
        Write("ERROR", message);
    }

    public static void Error(Exception ex, string? message = null)
    {
        var text = message is null
            ? ex.ToString()
            : message + Environment.NewLine + ex;

        Write("ERROR", text);
    }

    private static void Write(string level, string message)
    {
        try
        {
            lock (Sync)
            {
                Directory.CreateDirectory(LogDirectory);

                var line =
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}{Environment.NewLine}";

                File.AppendAllText(LogFilePath, line);
            }
        }
        catch
        {
        }
    }
}