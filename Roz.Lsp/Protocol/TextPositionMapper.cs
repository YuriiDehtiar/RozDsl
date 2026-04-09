using System;

namespace Roz.Lsp.Protocol;

internal static class TextPositionMapper
{
    public static Position FromOffset(string text, int offset)
    {
        text ??= string.Empty;

        if (offset < 0)
        {
            offset = 0;
        }

        if (offset > text.Length)
        {
            offset = text.Length;
        }

        var line = 0;
        var character = 0;
        var i = 0;

        while (i < offset)
        {
            var ch = text[i];

            if (ch == '\r')
            {
                if (i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                line++;
                character = 0;
            }
            else if (ch == '\n')
            {
                line++;
                character = 0;
            }
            else
            {
                character++;
            }

            i++;
        }

        return new Position
        {
            Line = line,
            Character = character
        };
    }

    public static int ToOffset(string text, Position position)
    {
        text ??= string.Empty;

        var targetLine = Math.Max(0, position.Line);
        var targetCharacter = Math.Max(0, position.Character);

        var line = 0;
        var character = 0;
        var i = 0;

        while (i < text.Length)
        {
            if (line == targetLine && character == targetCharacter)
            {
                return i;
            }

            var ch = text[i];

            if (ch == '\r')
            {
                if (i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }

                line++;
                character = 0;
                i++;
                continue;
            }

            if (ch == '\n')
            {
                line++;
                character = 0;
                i++;
                continue;
            }

            if (line == targetLine)
            {
                character++;
            }

            i++;
        }

        return text.Length;
    }

    public static Range FromSpan(string text, int start, int length)
    {
        if (length < 0)
        {
            length = 0;
        }

        var rangeStart = FromOffset(text, start);
        var rangeEnd = FromOffset(text, start + length);

        return new Range
        {
            Start = rangeStart,
            End = rangeEnd
        };
    }
}