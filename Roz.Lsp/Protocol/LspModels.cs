using System;

namespace Roz.Lsp.Protocol;

internal sealed class InitializeParams
{
    public string? RootUri { get; set; }
}

internal sealed class InitializeResult
{
    public ServerCapabilities Capabilities { get; set; } = new();
}

internal sealed class ServerCapabilities
{
    public int TextDocumentSync { get; set; }
    public bool HoverProvider { get; set; }
    public bool DefinitionProvider { get; set; }
    public bool DocumentSymbolProvider { get; set; }
    public CompletionOptions CompletionProvider { get; set; }
}

internal sealed class TextDocumentItem
{
    public Uri Uri { get; set; } = null!;
    public string LanguageId { get; set; } = string.Empty;
    public int Version { get; set; }
    public string Text { get; set; } = string.Empty;
}

internal sealed class DidOpenTextDocumentParams
{
    public TextDocumentItem TextDocument { get; set; } = null!;
}

internal sealed class VersionedTextDocumentIdentifier
{
    public Uri Uri { get; set; } = null!;
    public int Version { get; set; }
}

internal sealed class TextDocumentContentChangeEvent
{
    public string Text { get; set; } = string.Empty;
}

internal sealed class DidChangeTextDocumentParams
{
    public VersionedTextDocumentIdentifier TextDocument { get; set; } = null!;
    public TextDocumentContentChangeEvent[] ContentChanges { get; set; } = Array.Empty<TextDocumentContentChangeEvent>();
}

internal sealed class TextDocumentIdentifier
{
    public Uri Uri { get; set; } = null!;
}

internal sealed class DidCloseTextDocumentParams
{
    public TextDocumentIdentifier TextDocument { get; set; } = null!;
}

internal sealed class Position
{
    public int Line { get; set; }
    public int Character { get; set; }
}

internal sealed class Range
{
    public Position Start { get; set; } = new();
    public Position End { get; set; } = new();
}

internal sealed class LspDiagnostic
{
    public Range Range { get; set; } = new();
    public int Severity { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

internal sealed class PublishDiagnosticsParams
{
    public Uri Uri { get; set; } = null!;
    public LspDiagnostic[] Diagnostics { get; set; } = Array.Empty<LspDiagnostic>();
}

internal sealed class CompletionParams
{
    public TextDocumentIdentifier TextDocument { get; set; } = null!;
    public Position Position { get; set; } = new();
}

internal sealed class CompletionItem
{
    public string Label { get; set; } = string.Empty;
    public int Kind { get; set; }
    public string? Detail { get; set; }
    public string? Documentation { get; set; }
    public string? InsertText { get; set; }
    public string? SortText { get; set; }
    public int InsertTextFormat { get; set; }
}

internal sealed class CompletionList
{
    public bool IsIncomplete { get; set; }
    public CompletionItem[] Items { get; set; } = Array.Empty<CompletionItem>();
}

internal static class CompletionItemKinds
{
    public const int Keyword = 14;
    public const int Property = 10;
}

internal static class InsertTextFormats
{
    public const int PlainText = 1;
    public const int Snippet = 2;
}

internal sealed class HoverParams
{
    public TextDocumentIdentifier TextDocument { get; set; } = null!;
    public Position Position { get; set; } = new();
}

internal sealed class MarkupContent
{
    public string Kind { get; set; } = "markdown";
    public string Value { get; set; } = string.Empty;
}

internal sealed class Hover
{
    public MarkupContent Contents { get; set; } = new();
    public Range? Range { get; set; }
}

internal sealed class DocumentSymbolParams
{
    public TextDocumentIdentifier TextDocument { get; set; } = null!;
}

internal sealed class DocumentSymbol
{
    public string Name { get; set; } = string.Empty;
    public int Kind { get; set; }
    public Range Range { get; set; } = new();
    public Range SelectionRange { get; set; } = new();
    public DocumentSymbol[] Children { get; set; } = Array.Empty<DocumentSymbol>();
}

internal static class SymbolKinds
{
    public const int Object = 19;
    public const int Property = 7;
}

internal sealed class DefinitionParams
{
    public TextDocumentIdentifier TextDocument { get; set; } = null!;
    public Position Position { get; set; } = new();
}

internal sealed class Location
{
    public Uri Uri { get; set; } = null!;
    public Range Range { get; set; } = new();
}

internal sealed class CompletionOptions
{
    public bool ResolveProvider { get; set; }

    public string[] TriggerCharacters { get; set; } = Array.Empty<string>();

    public string[] AllCommitCharacters { get; set; } = Array.Empty<string>();
}