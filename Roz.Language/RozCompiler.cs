// Roz.Language/RozCompiler.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

using Roz.Language.Ast;
using Roz.Language.Diagnostics;
using Roz.Language.Parsing;
using Roz.Language.Validation;

namespace Roz.Language;

/// <summary>
/// Єдина "точка входу" для зовнішнього коду (CLI/тести).
/// Потік обробки:
///   Text -> Lexer -> Parser -> AST -> SemanticValidator -> (опційно) JSON
/// Повертає CompilationResult: Diagnostics + Json (якщо без помилок).
/// </summary>
public sealed class RozCompiler
{
    /// <summary>
    /// Компіляція з тексту (вміст файлу .roz).
    /// </summary>
    public CompilationResult CompileText(string text)
    {
        var diagnostics = new DiagnosticBag();

        // 1) Parse -> AST
        var parser = new Parser(text ?? string.Empty, diagnostics);
        RozDocument doc = parser.ParseDocument();

        // 2) Semantic validation
        var validator = new SemanticValidator();
        validator.Validate(doc, diagnostics);

        // 3) CodeGen (JSON) — тільки якщо немає помилок
        string? json = null;
        if (!diagnostics.HasErrors)
            json = GenerateJson(doc);

        return new CompilationResult(json, diagnostics.Items);
    }

    /// <summary>
    /// Компіляція з файлу .roz (за шляхом).
    /// </summary>
    public CompilationResult CompileFile(string path)
    {
        var diagnostics = new DiagnosticBag();

        if (string.IsNullOrWhiteSpace(path))
        {
            diagnostics.Report("ROZ900", "Не задано шлях до файлу.", new TextSpan(0, 0));
            return new CompilationResult(null, diagnostics.Items);
        }

        string text;
        try
        {
            text = File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            diagnostics.Report("ROZ901", $"Не вдалося прочитати файл: {ex.Message}", new TextSpan(0, 0));
            return new CompilationResult(null, diagnostics.Items);
        }

        // Далі — той самий конвеєр
        var parser = new Parser(text, diagnostics);
        RozDocument doc = parser.ParseDocument();

        var validator = new SemanticValidator();
        validator.Validate(doc, diagnostics);

        string? json = null;
        if (!diagnostics.HasErrors)
            json = GenerateJson(doc);

        return new CompilationResult(json, diagnostics.Items);
    }

    // ----------------------- JSON generation (v1) -----------------------

    private static string GenerateJson(RozDocument doc)
    {
        // Простий і прозорий формат для першої версії.
        // Пізніше можна винести в CodeGen/JsonGenerator і ускладнювати схему.
        var services = new List<object>(doc.Services.Count);

        foreach (var s in doc.Services)
        {
            var ports = new List<object>(s.Ports.Count);
            foreach (var p in s.Ports)
            {
                ports.Add(new
                {
                    host = p.HostPort,
                    container = p.ContainerPort
                });
            }

            services.Add(new
            {
                name = s.Name,
                image = s.Image ?? string.Empty,
                replicas = s.Replicas ?? 0,
                ports = ports
            });
        }

        var root = new { services = services };

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        return JsonSerializer.Serialize(root, options);
    }
}
