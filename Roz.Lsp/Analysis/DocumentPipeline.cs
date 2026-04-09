using System;
using Roz.Lsp.Documents;

namespace Roz.Lsp.Analysis;

internal sealed class DocumentPipeline
{
    private readonly DocumentStore _store;
    private readonly DocumentAnalyzer _analyzer;

    public DocumentPipeline(DocumentStore store, DocumentAnalyzer analyzer)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _analyzer = analyzer ?? throw new ArgumentNullException(nameof(analyzer));
    }

    public AnalyzedDocument OpenAndAnalyze(Uri uri, string text, int version)
    {
        _store.Open(uri, text, version);
        return AnalyzeExisting(uri);
    }

    public AnalyzedDocument UpdateAndAnalyze(Uri uri, string text, int version)
    {
        var updated = _store.Update(uri, text, version);

        if (!updated)
        {
            _store.Open(uri, text, version);
        }

        return AnalyzeExisting(uri);
    }

    public bool Close(Uri uri)
    {
        return _store.Close(uri);
    }

    private AnalyzedDocument AnalyzeExisting(Uri uri)
    {
        if (!_store.TryGet(uri, out var document) || document is null)
        {
            throw new InvalidOperationException($"Document not found: {uri}");
        }

        var compilation = _analyzer.Analyze(document.Text);
        return new AnalyzedDocument(document, compilation);
    }

    public bool TryGetDocument(Uri uri, out TextDocumentState? document)
    {
        return _store.TryGet(uri, out document);
    }
}