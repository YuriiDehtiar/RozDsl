// Roz.Cli/Program.cs
using System;
using System.IO;

using Roz.Language;

internal static class Program
{
    // Usage:
    //   Roz.Cli <input.roz> [output.json]
    //
    // Examples:
    //   Roz.Cli Samples/demo.roz
    //   Roz.Cli Samples/demo.roz out.json
    public static int Main(string[] args)
    {
        if (args.Length == 0 || args[0] is "-h" or "--help" or "/?")
        {
            PrintHelp();
            return 1;
        }

        string inputPath = args[0];
        string outputPath = args.Length >= 2
            ? args[1]
            : Path.ChangeExtension(inputPath, ".json");

        var compiler = new RozCompiler();
        var result = compiler.CompileFile(inputPath);

        if (result.HasErrors)
        {
            Console.WriteLine("❌ Помилки компіляції .roz:");
            foreach (var d in result.Diagnostics)
            {
                // Мінімальний вивід: код + повідомлення + позиція
                Console.WriteLine($"{d.Code}: {d.Message} {d.Span}");
            }
            return 2;
        }

        try
        {
            File.WriteAllText(outputPath, result.Json ?? string.Empty);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Не вдалося записати файл '{outputPath}': {ex.Message}");
            return 3;
        }

        Console.WriteLine($"✅ OK. Згенеровано: {outputPath}");
        return 0;
    }

    private static void PrintHelp()
    {
        Console.WriteLine("Roz.Cli — компілятор .roz -> JSON");
        Console.WriteLine();
        Console.WriteLine("Використання:");
        Console.WriteLine("  Roz.Cli <input.roz> [output.json]");
        Console.WriteLine();
        Console.WriteLine("Приклади:");
        Console.WriteLine("  Roz.Cli Samples/demo.roz");
        Console.WriteLine("  Roz.Cli Samples/demo.roz out.json");
    }
}
