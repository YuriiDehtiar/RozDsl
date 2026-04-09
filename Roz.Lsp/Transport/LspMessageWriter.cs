using System;
using System.IO;
using System.Text;

namespace Roz.Lsp.Transport;

internal sealed class LspMessageWriter
{
    private static readonly Encoding Utf8 = new UTF8Encoding(false);

    public void Write(Stream stream, string json)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        if (json is null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        var payloadBytes = Utf8.GetBytes(json);
        var header = $"Content-Length: {payloadBytes.Length}\r\n\r\n";
        var headerBytes = Encoding.ASCII.GetBytes(header);

        stream.Write(headerBytes, 0, headerBytes.Length);
        stream.Write(payloadBytes, 0, payloadBytes.Length);
        stream.Flush();
    }
}