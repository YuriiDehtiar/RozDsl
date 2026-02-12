// Roz.Language/Parsing/Token.cs
using System;

using Roz.Language.Diagnostics;

namespace Roz.Language.Parsing;

/// <summary>
/// A single lexical token produced by Lexer and consumed by Parser.
/// Stores kind + original text + source span.
/// </summary>
internal sealed class Token
{
    public Token(TokenKind kind, string text, TextSpan span)
    {
        Kind = kind;
        Text = text ?? string.Empty;
        Span = span;
    }

    public TokenKind Kind { get; }

    /// <summary>Original token text as it appears in the source (not decoded).</summary>
    public string Text { get; }

    /// <summary>Location in the source text.</summary>
    public TextSpan Span { get; }

    public override string ToString()
        => $"{Kind} '{Text}' {Span}";
}
