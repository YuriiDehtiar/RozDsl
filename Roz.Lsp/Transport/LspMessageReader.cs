using System;
using System.IO;
using System.Text;

namespace Roz.Lsp.Transport;

internal sealed class LspMessageReader
{
    private static readonly Encoding Utf8 = new UTF8Encoding(false);

    public string? Read(Stream stream)
    {
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }

        var contentLength = ReadContentLength(stream);

        if (contentLength is null)
        {
            return null;
        }

        var payloadBytes = ReadExactly(stream, contentLength.Value);
        return Utf8.GetString(payloadBytes);
    }

    private static int? ReadContentLength(Stream stream)
    {
        string? line;
        int? contentLength = null;

        while (true)
        {
            line = ReadAsciiLine(stream);

            if (line is null)
            {
                return null;
            }

            if (line.Length == 0)
            {
                break;
            }

            const string prefix = "Content-Length:";

            if (line.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                var valueText = line.Substring(prefix.Length).Trim();

                if (!int.TryParse(valueText, out var length) || length < 0)
                {
                    throw new InvalidDataException($"Invalid Content-Length header: '{line}'");
                }

                contentLength = length;
            }
        }

        if (contentLength is null)
        {
            throw new InvalidDataException("Missing Content-Length header.");
        }

        return contentLength;
    }

    private static string? ReadAsciiLine(Stream stream)
    {
        using var buffer = new MemoryStream();

        while (true)
        {
            var b = stream.ReadByte();

            if (b < 0)
            {
                if (buffer.Length == 0)
                {
                    return null;
                }

                break;
            }

            if (b == '\r')
            {
                var next = stream.ReadByte();

                if (next == '\n')
                {
                    break;
                }

                throw new InvalidDataException("Invalid header line ending. Expected CRLF.");
            }

            buffer.WriteByte((byte)b);
        }

        return Encoding.ASCII.GetString(buffer.ToArray());
    }

    private static byte[] ReadExactly(Stream stream, int length)
    {
        var buffer = new byte[length];
        var offset = 0;

        while (offset < length)
        {
            var read = stream.Read(buffer, offset, length - offset);

            if (read <= 0)
            {
                throw new EndOfStreamException("Unexpected end of stream while reading LSP payload.");
            }

            offset += read;
        }

        return buffer;
    }
}