// Roz.Language/Parsing/Lexer.cs
using System;
using System.Collections.Generic;

using Roz.Language.Diagnostics;

namespace Roz.Language.Parsing;

/// <summary>
/// Лексер: перетворює сирий текст .roz у потік Token.
/// За замовчуванням пропускає trivia (пробіли/переноси/коментарі).
/// Якщо emitTrivia=true — повертає Whitespace/NewLine/LineComment як токени.
/// </summary>
internal sealed class Lexer
{
    private readonly string _text;
    private readonly DiagnosticBag _diagnostics;
    private readonly bool _emitTrivia;
    private int _pos;

    public Lexer(string text, DiagnosticBag diagnostics, bool emitTrivia = false)
    {
        _text = text ?? string.Empty;
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));
        _emitTrivia = emitTrivia;
        _pos = 0;
    }

    public Token NextToken()
    {
        while (true)
        {
            if (_pos >= _text.Length)
                return new Token(TokenKind.EndOfFile, string.Empty, new TextSpan(_pos, 0));

            char c = Current;

            // -------- newline (\r\n або \n або \r) --------
            if (c == '\r' || c == '\n')
            {
                int start = _pos;

                if (c == '\r' && Peek(1) == '\n')
                    _pos += 2;
                else
                    _pos += 1;

                var span = new TextSpan(start, _pos - start);
                var txt = Slice(start, span.Length);

                if (_emitTrivia)
                    return new Token(TokenKind.NewLine, txt, span);

                continue; // пропускаємо
            }

            // -------- whitespace (без newline) --------
            if (char.IsWhiteSpace(c))
            {
                int start = _pos;

                while (_pos < _text.Length)
                {
                    char w = _text[_pos];
                    if (w == '\r' || w == '\n' || !char.IsWhiteSpace(w))
                        break;
                    _pos++;
                }

                var span = new TextSpan(start, _pos - start);
                var txt = Slice(start, span.Length);

                if (_emitTrivia)
                    return new Token(TokenKind.Whitespace, txt, span);

                continue; // пропускаємо
            }

            // -------- line comment: // ... --------
            if (c == '/' && Peek(1) == '/')
            {
                int start = _pos;
                _pos += 2;

                while (_pos < _text.Length)
                {
                    char cc = _text[_pos];
                    if (cc == '\r' || cc == '\n')
                        break;
                    _pos++;
                }

                var span = new TextSpan(start, _pos - start);
                var txt = Slice(start, span.Length);

                if (_emitTrivia)
                    return new Token(TokenKind.LineComment, txt, span);

                continue; // пропускаємо
            }

            // -------- identifier / keyword --------
            if (IsIdentifierStart(c))
            {
                int start = _pos;
                _pos++;

                while (_pos < _text.Length && IsIdentifierPart(_text[_pos]))
                    _pos++;

                var span = new TextSpan(start, _pos - start);
                var txt = Slice(start, span.Length);

                return new Token(GetKeywordKind(txt), txt, span);
            }

            // -------- number --------
            if (char.IsDigit(c))
            {
                int start = _pos;
                _pos++;

                while (_pos < _text.Length && char.IsDigit(_text[_pos]))
                    _pos++;

                var span = new TextSpan(start, _pos - start);
                var txt = Slice(start, span.Length);

                return new Token(TokenKind.Number, txt, span);
            }

            // -------- string "..." --------
            if (c == '"')
            {
                int start = _pos;
                _pos++; // пропустили відкриваючу "

                bool terminated = false;

                while (_pos < _text.Length)
                {
                    char sc = _text[_pos];

                    // прості escape-послідовності: \" \\ \n \r \t
                    if (sc == '\\')
                    {
                        _pos++; // '\'
                        if (_pos < _text.Length)
                            _pos++; // наступний символ (як є)
                        continue;
                    }

                    if (sc == '"')
                    {
                        _pos++; // закриваюча "
                        terminated = true;
                        break;
                    }

                    // v1: не дозволяємо рядок через перенос
                    if (sc == '\r' || sc == '\n')
                        break;

                    _pos++;
                }

                var span = new TextSpan(start, _pos - start);
                var txt = Slice(start, span.Length);

                if (!terminated)
                    _diagnostics.Report("ROZ001", "Незакритий рядок: очікувалась '\"'.", span);

                return new Token(TokenKind.String, txt, span);
            }

            // -------- punctuation --------
            {
                int start = _pos;
                _pos++;

                TokenKind kind = c switch
                {
                    '{' => TokenKind.OpenBrace,
                    '}' => TokenKind.CloseBrace,
                    ':' => TokenKind.Colon,
                    _ => TokenKind.BadToken
                };

                var span = new TextSpan(start, 1);
                var txt = Slice(start, 1);

                if (kind == TokenKind.BadToken)
                    _diagnostics.Report("ROZ002", $"Невідомий символ: '{txt}'.", span);

                return new Token(kind, txt, span);
            }
        }
    }

    public IEnumerable<Token> LexAll()
    {
        while (true)
        {
            var t = NextToken();
            yield return t;
            if (t.Kind == TokenKind.EndOfFile)
                yield break;
        }
    }

    // ---------------- helpers ----------------

    private char Current => _pos < _text.Length ? _text[_pos] : '\0';

    private char Peek(int offset)
    {
        int i = _pos + offset;
        return (i >= 0 && i < _text.Length) ? _text[i] : '\0';
    }

    private string Slice(int start, int length)
    {
        if (length <= 0) return string.Empty;
        return _text.Substring(start, length);
    }

    private static bool IsIdentifierStart(char c)
        => char.IsLetter(c) || c == '_';

    private static bool IsIdentifierPart(char c)
        => char.IsLetterOrDigit(c) || c == '_' || c == '-';

    private static TokenKind GetKeywordKind(string text) => text switch
    {
        "service" => TokenKind.ServiceKeyword,
        "image" => TokenKind.ImageKeyword,
        "replicas" => TokenKind.ReplicasKeyword,
        "port" => TokenKind.PortKeyword,
        _ => TokenKind.Identifier
    };
}
