using System;
using Roz.Lsp.Analysis;
using Roz.Lsp.Documents;

namespace Roz.Lsp.Protocol;

internal sealed class LspSession
{
    private readonly DocumentPipeline _pipeline;
    private bool _shutdownRequested;

    public LspSession(DocumentPipeline pipeline)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
    }

    public InitializeResult Initialize(InitializeParams request)
    {
        FileLogger.Info("LSP initialize called");

        if (!string.IsNullOrWhiteSpace(request.RootUri))
        {
            FileLogger.Info($"RootUri: {request.RootUri}");
        }

        return new InitializeResult
        {
            Capabilities = new ServerCapabilities
            {
                TextDocumentSync = LspConstants.TextDocumentSyncFull,
                HoverProvider = true,
                DefinitionProvider = true,
                DocumentSymbolProvider = true,
                CompletionProvider = new CompletionOptions
                {
                    ResolveProvider = false,
                    TriggerCharacters = Array.Empty<string>(),
                    AllCommitCharacters = Array.Empty<string>()
                }
            }
        };
    }

    public object? Shutdown()
    {
        FileLogger.Info("LSP shutdown called");
        _shutdownRequested = true;
        return null;
    }

    public bool Exit()
    {
        FileLogger.Info("LSP exit called");

        if (_shutdownRequested)
        {
            FileLogger.Info("Exit after shutdown: normal");
        }
        else
        {
            FileLogger.Info("Exit before shutdown: abnormal");
        }

        return true;
    }

    public AnalyzedDocument DidOpen(DidOpenTextDocumentParams request)
    {
        var document = request.TextDocument;

        FileLogger.Info("LSP didOpen called");
        FileLogger.Info($"didOpen Uri: {document.Uri}");
        FileLogger.Info($"didOpen Version: {document.Version}");

        return _pipeline.OpenAndAnalyze(document.Uri, document.Text, document.Version);
    }

    public AnalyzedDocument DidChange(DidChangeTextDocumentParams request)
    {
        FileLogger.Info("LSP didChange called");
        FileLogger.Info($"didChange Uri: {request.TextDocument.Uri}");
        FileLogger.Info($"didChange Version: {request.TextDocument.Version}");

        if (request.ContentChanges.Length == 0)
        {
            throw new InvalidOperationException("didChange must contain at least one content change.");
        }

        var latestText = request.ContentChanges[^1].Text ?? string.Empty;

        return _pipeline.UpdateAndAnalyze(
            request.TextDocument.Uri,
            latestText,
            request.TextDocument.Version);
    }

    public bool DidClose(DidCloseTextDocumentParams request)
    {
        FileLogger.Info("LSP didClose called");
        FileLogger.Info($"didClose Uri: {request.TextDocument.Uri}");

        return _pipeline.Close(request.TextDocument.Uri);
    }

    public CompletionList GetCompletion(CompletionParams request)
    {
        FileLogger.Info("LSP completion called");
        FileLogger.Info($"completion Uri: {request.TextDocument.Uri}");
        FileLogger.Info($"completion Position: {request.Position.Line}:{request.Position.Character}");

        if (!_pipeline.TryGetDocument(request.TextDocument.Uri, out var document) || document is null)
        {
            return new CompletionList
            {
                IsIncomplete = false,
                Items = Array.Empty<CompletionItem>()
            };
        }

        var offset = TextPositionMapper.ToOffset(document.Text, request.Position);
        var prefix = document.Text[..offset];

        if (IsInsideString(prefix))
        {
            return new CompletionList
            {
                IsIncomplete = false,
                Items = Array.Empty<CompletionItem>()
            };
        }

        var braceDepth = GetBraceDepth(prefix);

        if (braceDepth <= 0)
        {
            return new CompletionList
            {
                IsIncomplete = false,
                Items =
                [
                    new CompletionItem
                {
                    Label = "service",
                    Kind = CompletionItemKinds.Keyword,
                    Detail = "Top-level service declaration",
                    Documentation = "Declares a service block.",
                    InsertText = "service $1 {\n  $0\n}",
                    SortText = "001",
                    InsertTextFormat = InsertTextFormats.Snippet
                }
                ]
            };
        }

        var currentBlockText = GetCurrentBlockPrefix(prefix);
        var items = new List<CompletionItem>();

        if (!ContainsProperty(currentBlockText, "image"))
        {
            items.Add(new CompletionItem
            {
                Label = "image",
                Kind = CompletionItemKinds.Property,
                Detail = "Docker image",
                Documentation = "Docker image reference. Expected string literal.",
                InsertText = "image \"$1\"",
                SortText = "001",
                InsertTextFormat = InsertTextFormats.Snippet
            });
        }

        if (!ContainsProperty(currentBlockText, "replicas"))
        {
            items.Add(new CompletionItem
            {
                Label = "replicas",
                Kind = CompletionItemKinds.Property,
                Detail = "Replica count",
                Documentation = "Number of replicas. Expected positive integer.",
                InsertText = "replicas $1",
                SortText = "002",
                InsertTextFormat = InsertTextFormats.Snippet
            });
        }

        items.Add(new CompletionItem
        {
            Label = "port",
            Kind = CompletionItemKinds.Property,
            Detail = "Port mapping",
            Documentation = "Port mapping in host:container form.",
            InsertText = "port $1:$2",
            SortText = "003",
            InsertTextFormat = InsertTextFormats.Snippet
        });

        return new CompletionList
        {
            IsIncomplete = false,
            Items = items.ToArray()
        };
    }

    public Hover? GetHover(HoverParams request)
    {
        FileLogger.Info("LSP hover called");
        FileLogger.Info($"hover Uri: {request.TextDocument.Uri}");
        FileLogger.Info($"hover Position: {request.Position.Line}:{request.Position.Character}");

        if (!_pipeline.TryGetDocument(request.TextDocument.Uri, out var document) || document is null)
        {
            return null;
        }

        var offset = TextPositionMapper.ToOffset(document.Text, request.Position);
        var word = GetWordAt(document.Text, offset, out var start, out var length);

        if (string.IsNullOrWhiteSpace(word))
        {
            return null;
        }

        string? markdown = word switch
        {
            "service" => "Declares a service block.",
            "image" => "Docker image reference. Expected string literal.",
            "replicas" => "Number of replicas. Expected positive integer.",
            "port" => "Port mapping in `host:container` form.",
            _ => null
        };

        if (markdown is null)
        {
            return null;
        }

        return new Hover
        {
            Contents = new MarkupContent
            {
                Kind = "markdown",
                Value = markdown
            },
            Range = TextPositionMapper.FromSpan(document.Text, start, length)
        };
    }


    public DocumentSymbol[] GetDocumentSymbols(DocumentSymbolParams request)
    {
        FileLogger.Info("LSP documentSymbol called");
        FileLogger.Info($"documentSymbol Uri: {request.TextDocument.Uri}");

        if (!_pipeline.TryGetDocument(request.TextDocument.Uri, out var document) || document is null)
        {
            return Array.Empty<DocumentSymbol>();
        }

        var text = document.Text;
        var lines = text.Split('\n');

        var symbols = new List<DocumentSymbol>();

        string? currentServiceName = null;
        int currentServiceStartLine = -1;
        var currentChildren = new List<DocumentSymbol>();

        for (int i = 0; i < lines.Length; i++)
        {
            var rawLine = lines[i];
            var line = rawLine.Trim();

            if (line.StartsWith("service ", StringComparison.Ordinal))
            {
                if (currentServiceName is not null)
                {
                    symbols.Add(CreateServiceSymbol(
                        currentServiceName,
                        currentServiceStartLine,
                        i - 1,
                        currentChildren.ToArray()));
                }

                currentChildren = new List<DocumentSymbol>();
                currentServiceStartLine = i;

                var afterKeyword = line.Substring("service ".Length);
                var braceIndex = afterKeyword.IndexOf('{');
                currentServiceName = braceIndex >= 0
                    ? afterKeyword[..braceIndex].Trim()
                    : afterKeyword.Trim();

                continue;
            }

            if (currentServiceName is not null)
            {
                if (line.StartsWith("image ", StringComparison.Ordinal))
                {
                    currentChildren.Add(CreatePropertySymbol("image", i, rawLine));
                }
                else if (line.StartsWith("replicas ", StringComparison.Ordinal))
                {
                    currentChildren.Add(CreatePropertySymbol("replicas", i, rawLine));
                }
                else if (line.StartsWith("port ", StringComparison.Ordinal))
                {
                    currentChildren.Add(CreatePropertySymbol("port", i, rawLine));
                }
                else if (line == "}")
                {
                    symbols.Add(CreateServiceSymbol(
                        currentServiceName,
                        currentServiceStartLine,
                        i,
                        currentChildren.ToArray()));

                    currentServiceName = null;
                    currentServiceStartLine = -1;
                    currentChildren = new List<DocumentSymbol>();
                }
            }
        }

        if (currentServiceName is not null)
        {
            symbols.Add(CreateServiceSymbol(
                currentServiceName,
                currentServiceStartLine,
                Math.Max(currentServiceStartLine, lines.Length - 1),
                currentChildren.ToArray()));
        }

        return symbols.ToArray();
    }

    public Location[] GetDefinition(DefinitionParams request)
    {
        FileLogger.Info("LSP definition called");
        FileLogger.Info($"definition Uri: {request.TextDocument.Uri}");
        FileLogger.Info($"definition Position: {request.Position.Line}:{request.Position.Character}");

        if (!_pipeline.TryGetDocument(request.TextDocument.Uri, out var document) || document is null)
        {
            return Array.Empty<Location>();
        }

        var text = document.Text;
        var offset = TextPositionMapper.ToOffset(text, request.Position);
        var word = GetWordAt(text, offset, out _, out _);

        if (string.IsNullOrWhiteSpace(word))
        {
            return Array.Empty<Location>();
        }

        var lines = text.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var rawLine = lines[i];
            var line = rawLine.Trim();

            if (!line.StartsWith("service ", StringComparison.Ordinal))
            {
                continue;
            }

            var serviceKeywordIndex = rawLine.IndexOf("service ", StringComparison.Ordinal);
            if (serviceKeywordIndex < 0)
            {
                continue;
            }

            var nameStart = serviceKeywordIndex + "service ".Length;

            while (nameStart < rawLine.Length && char.IsWhiteSpace(rawLine[nameStart]))
            {
                nameStart++;
            }

            var nameEnd = nameStart;
            while (nameEnd < rawLine.Length && (char.IsLetterOrDigit(rawLine[nameEnd]) || rawLine[nameEnd] == '_'))
            {
                nameEnd++;
            }

            if (nameEnd <= nameStart)
            {
                continue;
            }

            var declaredName = rawLine.Substring(nameStart, nameEnd - nameStart);

            if (!string.Equals(declaredName, word, StringComparison.Ordinal))
            {
                continue;
            }

            return
            [
                new Location
            {
                Uri = request.TextDocument.Uri,
                Range = new Range
                {
                    Start = new Position { Line = i, Character = nameStart },
                    End = new Position { Line = i, Character = nameEnd }
                }
            }
            ];
        }

        return Array.Empty<Location>();
    }


    private static bool IsInsideString(string text)
    {
        var inside = false;

        for (var i = 0; i < text.Length; i++)
        {
            if (text[i] == '"' && (i == 0 || text[i - 1] != '\\'))
            {
                inside = !inside;
            }
        }

        return inside;
    }

    private static string GetCurrentBlockPrefix(string prefix)
    {
        var lastOpenBrace = prefix.LastIndexOf('{');

        if (lastOpenBrace < 0)
        {
            return prefix;
        }

        return prefix[(lastOpenBrace + 1)..];
    }

    private static bool ContainsProperty(string text, string propertyName)
    {
        var lines = text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();

            if (trimmed.StartsWith(propertyName + " ", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string? GetWordAt(string text, int offset, out int start, out int length)
    {
        start = 0;
        length = 0;

        if (string.IsNullOrEmpty(text))
        {
            return null;
        }

        if (offset < 0)
        {
            offset = 0;
        }

        if (offset >= text.Length)
        {
            offset = text.Length - 1;
        }

        if (!IsWordChar(text[offset]) && offset > 0 && IsWordChar(text[offset - 1]))
        {
            offset--;
        }

        if (!IsWordChar(text[offset]))
        {
            return null;
        }

        var left = offset;
        var right = offset;

        while (left > 0 && IsWordChar(text[left - 1]))
        {
            left--;
        }

        while (right + 1 < text.Length && IsWordChar(text[right + 1]))
        {
            right++;
        }

        start = left;
        length = right - left + 1;

        return text.Substring(start, length);
    }

    private static bool IsWordChar(char ch)
    {
        return char.IsLetterOrDigit(ch) || ch == '_';
    }

    private static int GetBraceDepth(string text)
    {
        var depth = 0;

        foreach (var ch in text)
        {
            if (ch == '{')
            {
                depth++;
            }
            else if (ch == '}')
            {
                depth = Math.Max(0, depth - 1);
            }
        }

        return depth;
    }

    private static DocumentSymbol CreateServiceSymbol(
    string serviceName,
    int startLine,
    int endLine,
    DocumentSymbol[] children)
    {
        var serviceNameStart = "service ".Length;
        var serviceNameEnd = serviceNameStart + serviceName.Length;

        return new DocumentSymbol
        {
            Name = serviceName,
            Kind = SymbolKinds.Object,
            Range = new Range
            {
                Start = new Position { Line = Math.Max(0, startLine), Character = 0 },
                End = new Position { Line = Math.Max(startLine, endLine), Character = 1 }
            },
            SelectionRange = new Range
            {
                Start = new Position { Line = Math.Max(0, startLine), Character = serviceNameStart },
                End = new Position { Line = Math.Max(0, startLine), Character = serviceNameEnd }
            },
            Children = children
        };
    }

    private static DocumentSymbol CreatePropertySymbol(string name, int line, string rawLine)
    {
        var startCharacter = Math.Max(0, rawLine.IndexOf(name, StringComparison.Ordinal));
        var endCharacter = startCharacter + name.Length;

        return new DocumentSymbol
        {
            Name = name,
            Kind = SymbolKinds.Property,
            Range = new Range
            {
                Start = new Position { Line = line, Character = startCharacter },
                End = new Position { Line = line, Character = endCharacter }
            },
            SelectionRange = new Range
            {
                Start = new Position { Line = line, Character = startCharacter },
                End = new Position { Line = line, Character = endCharacter }
            },
            Children = Array.Empty<DocumentSymbol>()
        };
    }
}