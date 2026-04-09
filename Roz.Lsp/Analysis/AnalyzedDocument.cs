using Roz.Language;

namespace Roz.Lsp.Documents;

internal sealed class AnalyzedDocument
{
    public AnalyzedDocument(TextDocumentState document, CompilationResult compilation)
    {
        Document = document;
        Compilation = compilation;
    }

    public TextDocumentState Document { get; }

    public CompilationResult Compilation { get; }
}