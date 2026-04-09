using System;

namespace Roz.Lsp.Documents;

internal sealed class TextDocumentState
{
    public TextDocumentState(Uri uri, string text, int version)
    {
        Uri = uri ?? throw new ArgumentNullException(nameof(uri));
        Text = text ?? string.Empty;
        Version = version;
    }

    public Uri Uri { get; }

    public string Text { get; private set; }

    public int Version { get; private set; }

    public void Update(string text, int version)
    {
        Text = text ?? string.Empty;
        Version = version;
    }
}