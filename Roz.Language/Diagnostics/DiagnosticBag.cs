// Roz.Language/Diagnostics/DiagnosticBag.cs
using System;
using System.Collections.Generic;

namespace Roz.Language.Diagnostics;

/// <summary>
/// Collects diagnostics produced by lexer/parser/validator.
/// Keep it internal so only the language engine uses it directly.
/// Public API should expose diagnostics via CompilationResult.
/// </summary>
internal sealed class DiagnosticBag
{
    private readonly List<Diagnostic> _items = new();

    public IReadOnlyList<Diagnostic> Items => _items;

    public bool HasErrors
    {
        get
        {
            // Treat all diagnostics as errors for v1 (simple).
            // If you later add Severity, filter here.
            return _items.Count > 0;
        }
    }

    public void Add(Diagnostic diagnostic)
    {
        if (diagnostic is null) throw new ArgumentNullException(nameof(diagnostic));
        _items.Add(diagnostic);
    }

    public void AddRange(IEnumerable<Diagnostic> diagnostics)
    {
        if (diagnostics is null) throw new ArgumentNullException(nameof(diagnostics));
        foreach (var d in diagnostics)
            Add(d);
    }

    /// <summary>
    /// Adds a diagnostic with code/message/span.
    /// This is the main helper used by Lexer/Parser/Validator.
    /// </summary>
    public void Report(string code, string message, TextSpan span)
    {
        if (string.IsNullOrWhiteSpace(code))
            code = "ROZ000";

        if (message is null)
            message = string.Empty;

        Add(new Diagnostic(code, message, span));
    }

    /// <summary>
    /// Convenience helper: report at an explicit start/length.
    /// </summary>
    public void Report(string code, string message, int start, int length)
    {
        Report(code, message, new TextSpan(start, length));
    }

    public Diagnostic[] ToArray() => _items.ToArray();

    public void Clear() => _items.Clear();
}
