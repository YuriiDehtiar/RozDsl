using System;
using System.Collections.Generic;

namespace Roz.Lsp.Documents;

internal sealed class DocumentStore
{
    private readonly Dictionary<string, TextDocumentState> _documents =
        new(StringComparer.OrdinalIgnoreCase);

    public int Count => _documents.Count;

    public void Open(Uri uri, string text, int version)
    {
        var key = GetKey(uri);
        _documents[key] = new TextDocumentState(uri, text, version);
    }

    public bool TryGet(Uri uri, out TextDocumentState? document)
    {
        var key = GetKey(uri);
        return _documents.TryGetValue(key, out document);
    }

    public bool Update(Uri uri, string text, int version)
    {
        var key = GetKey(uri);

        if (!_documents.TryGetValue(key, out var document))
        {
            return false;
        }

        document.Update(text, version);
        return true;
    }

    public bool Close(Uri uri)
    {
        var key = GetKey(uri);
        return _documents.Remove(key);
    }

    private static string GetKey(Uri uri)
    {
        if (uri is null)
        {
            throw new ArgumentNullException(nameof(uri));
        }

        return uri.AbsoluteUri;
    }
}