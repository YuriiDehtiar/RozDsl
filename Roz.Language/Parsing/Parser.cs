// Roz.Language/Parsing/Parser.cs
//
// Assumptions about AST types (adjust if yours differ):
// - Roz.Language.Ast.RozDocument has a constructor: RozDocument(IReadOnlyList<ServiceDecl> services)
// - Roz.Language.Ast.ServiceDecl has a constructor: ServiceDecl(string name)
//   and writable properties: string? Image, int? Replicas, List<PortMapping> Ports (or similar)
// - Roz.Language.Ast.PortMapping has a constructor: PortMapping(int hostPort, int containerPort)
//
// If your AST classes are not like this yet, tell me what constructors/properties you have
// (or paste them) and I’ll adapt Parser to match.

using System;
using System.Collections.Generic;

using Roz.Language.Ast;
using Roz.Language.Diagnostics;

namespace Roz.Language.Parsing;

internal sealed class Parser
{
    private readonly List<Token> _tokens = new();
    private readonly DiagnosticBag _diagnostics;
    private int _pos;

    public Parser(string text, DiagnosticBag diagnostics)
    {
        _diagnostics = diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));

        // Lexer already skips trivia by default (emitTrivia=false),
        // but we filter again to be safe.
        var lexer = new Lexer(text ?? string.Empty, _diagnostics, emitTrivia: false);
        foreach (var t in lexer.LexAll())
        {
            if (t.Kind is TokenKind.Whitespace or TokenKind.NewLine or TokenKind.LineComment)
                continue;

            // BadToken already reported by lexer; skip it to keep parser cleaner.
            if (t.Kind == TokenKind.BadToken)
                continue;

            _tokens.Add(t);

            if (t.Kind == TokenKind.EndOfFile)
                break;
        }

        // Ensure EOF exists.
        if (_tokens.Count == 0 || _tokens[^1].Kind != TokenKind.EndOfFile)
            _tokens.Add(new Token(TokenKind.EndOfFile, string.Empty, new TextSpan(0, 0)));
    }

    public RozDocument ParseDocument()
    {
        var services = new List<ServiceDecl>();

        while (Current.Kind != TokenKind.EndOfFile)
        {
            if (Current.Kind == TokenKind.ServiceKeyword)
            {
                var svc = ParseServiceDecl();
                if (svc != null)
                    services.Add(svc);
                continue;
            }

            // Unexpected token at top level
            _diagnostics.Report(
                "ROZ110",
                $"Очікувалось ключове слово 'service', але отримано {Describe(Current)}.",
                Current.Span);

            Advance();
            SynchronizeTopLevel();
        }

        return new RozDocument(services);
    }

    private ServiceDecl? ParseServiceDecl()
    {
        // service <identifier> { ... }
        Expect(TokenKind.ServiceKeyword, "Очікувалось ключове слово 'service'.");

        var nameTok = Expect(TokenKind.Identifier, "Очікувалась назва сервісу (ідентифікатор) після 'service'.");
        string name = nameTok.Text;

        var openBrace = Expect(TokenKind.OpenBrace, "Очікувалась '{' після назви сервісу.");

        var svc = new ServiceDecl(name);

        bool seenImage = false;
        bool seenReplicas = false;

        // Parse fields until }
        while (Current.Kind != TokenKind.CloseBrace && Current.Kind != TokenKind.EndOfFile)
        {
            if (Current.Kind == TokenKind.ImageKeyword)
            {
                Advance(); // consume 'image'
                var strTok = Expect(TokenKind.String, "Очікувався рядок у лапках після 'image'.");

                if (seenImage)
                {
                    _diagnostics.Report("ROZ121", "Поле 'image' задане більше одного разу.", strTok.Span);
                }
                else
                {
                    svc.Image = UnquoteString(strTok.Text, strTok.Span);
                    seenImage = true;
                }

                continue;
            }

            if (Current.Kind == TokenKind.ReplicasKeyword)
            {
                Advance(); // consume 'replicas'
                var numTok = Expect(TokenKind.Number, "Очікувалось число після 'replicas'.");

                if (seenReplicas)
                {
                    _diagnostics.Report("ROZ122", "Поле 'replicas' задане більше одного разу.", numTok.Span);
                }
                else
                {
                    svc.Replicas = ParseIntOrReport(numTok, "ROZ130", "Некоректне значення 'replicas'. Очікувалось ціле число.");
                    seenReplicas = true;
                }

                continue;
            }

            if (Current.Kind == TokenKind.PortKeyword)
            {
                Advance(); // consume 'port'
                var hostTok = Expect(TokenKind.Number, "Очікувалось число (host port) після 'port'.");
                Expect(TokenKind.Colon, "Очікувався ':' між портами (формат host:container).");
                var contTok = Expect(TokenKind.Number, "Очікувалось число (container port) після ':'.");

                int host = ParseIntOrReport(hostTok, "ROZ131", "Некоректний host port. Очікувалось ціле число.");
                int cont = ParseIntOrReport(contTok, "ROZ132", "Некоректний container port. Очікувалось ціле число.");

                svc.Ports.Add(new PortMapping(host, cont));
                continue;
            }

            // Unknown field/token inside service block
            _diagnostics.Report(
                "ROZ120",
                $"Невідоме поле або токен у блоці service: {Describe(Current)}. Очікувалось: image/replicas/port або '}}'.",
                Current.Span);

            Advance();
            SynchronizeInsideService();
        }

        // close brace
        if (Current.Kind == TokenKind.CloseBrace)
        {
            Advance();
        }
        else
        {
            // Missing '}' - report, but continue
            _diagnostics.Report("ROZ111", "Очікувалась '}' наприкінці блоку service.", openBrace.Span);
        }

        return svc;
    }

    // -------------------- error recovery --------------------

    private void SynchronizeTopLevel()
    {
        // Skip tokens until we see 'service' or EOF
        while (Current.Kind != TokenKind.EndOfFile && Current.Kind != TokenKind.ServiceKeyword)
            Advance();
    }

    private void SynchronizeInsideService()
    {
        // Skip until a "good" boundary inside a service block
        while (Current.Kind != TokenKind.EndOfFile &&
               Current.Kind != TokenKind.CloseBrace &&
               Current.Kind != TokenKind.ImageKeyword &&
               Current.Kind != TokenKind.ReplicasKeyword &&
               Current.Kind != TokenKind.PortKeyword)
        {
            Advance();
        }
    }

    // -------------------- helpers --------------------

    private Token Current => Peek(0);

    private Token Peek(int offset)
    {
        int index = _pos + offset;
        if (index < 0) index = 0;
        if (index >= _tokens.Count) index = _tokens.Count - 1;
        return _tokens[index];
    }

    private Token Advance()
    {
        var cur = Current;
        _pos = Math.Min(_pos + 1, _tokens.Count);
        return cur;
    }

    private Token Expect(TokenKind kind, string message)
    {
        if (Current.Kind == kind)
            return Advance();

        _diagnostics.Report("ROZ100", message, Current.Span);

        // Recovery strategy:
        // - Do NOT consume EOF.
        // - Otherwise consume the unexpected token to make progress.
        if (Current.Kind != TokenKind.EndOfFile)
            return Advance();

        return Current;
    }

    private static string Describe(Token t)
    {
        if (t.Kind == TokenKind.EndOfFile) return "кінець файлу (EOF)";
        if (string.IsNullOrEmpty(t.Text)) return t.Kind.ToString();
        return $"{t.Kind} ('{t.Text}')";
    }

    private int ParseIntOrReport(Token tok, string code, string message)
    {
        if (int.TryParse(tok.Text, out int v))
            return v;

        _diagnostics.Report(code, message, tok.Span);
        return 0;
    }

    private string? UnquoteString(string tokenText, TextSpan span)
    {
        // tokenText includes quotes: "abc"
        if (tokenText.Length >= 2 && tokenText[0] == '"' && tokenText[^1] == '"')
        {
            var inner = tokenText.Substring(1, tokenText.Length - 2);
            return Unescape(inner);
        }

        // If lexer produced a malformed string token, keep raw and also report.
        _diagnostics.Report("ROZ140", "Некоректний рядок: очікувались подвійні лапки на початку і в кінці.", span);
        return tokenText;
    }

    private static string Unescape(string s)
    {
        // Minimal unescape for v1: \" \\ \n \r \t
        if (s.IndexOf('\\') < 0)
            return s;

        var result = new System.Text.StringBuilder(s.Length);
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c != '\\' || i == s.Length - 1)
            {
                result.Append(c);
                continue;
            }

            char n = s[++i];
            result.Append(n switch
            {
                '"' => '"',
                '\\' => '\\',
                'n' => '\n',
                'r' => '\r',
                't' => '\t',
                _ => n // unknown escape: keep as-is
            });
        }

        return result.ToString();
    }
}
