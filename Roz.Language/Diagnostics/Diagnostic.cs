// Roz.Language/Diagnostics/Diagnostic.cs
using System;

namespace Roz.Language.Diagnostics;

/// <summary>
/// A single diagnostic message produced by the language engine.
/// For v1 we keep it minimal: Code + Message + Span.
/// </summary>
public sealed class Diagnostic
{
    public Diagnostic(string code, string message, TextSpan span)
    {
        Code = string.IsNullOrWhiteSpace(code) ? "ROZ000" : code;
        Message = message ?? string.Empty;
        Span = span;
    }

    /// <summary>Stable machine-readable code (e.g., ROZ001).</summary>
    public string Code { get; }

    /// <summary>Human-readable message.</summary>
    public string Message { get; }

    /// <summary>Location in the source text.</summary>
    public TextSpan Span { get; }

    public override string ToString()
        => $"{Code}: {Message} @ {Span.Start} (+{Span.Length})";
}
