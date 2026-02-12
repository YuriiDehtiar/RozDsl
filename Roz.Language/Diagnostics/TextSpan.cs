// Roz.Language/Diagnostics/TextSpan.cs
using System;

namespace Roz.Language.Diagnostics;

/// <summary>
/// Represents a region in the source text: [Start, Start+Length).
/// We keep it as a small immutable value type.
/// </summary>
public readonly struct TextSpan : IEquatable<TextSpan>
{
    public TextSpan(int start, int length)
    {
        if (start < 0) throw new ArgumentOutOfRangeException(nameof(start), "Start must be >= 0.");
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), "Length must be >= 0.");

        Start = start;
        Length = length;
    }

    public int Start { get; }

    public int Length { get; }

    public int End => Start + Length;

    public bool IsEmpty => Length == 0;

    public override string ToString() => $"[{Start}..{End})";

    public bool Equals(TextSpan other) => Start == other.Start && Length == other.Length;

    public override bool Equals(object? obj) => obj is TextSpan other && Equals(other);

    public override int GetHashCode() => HashCode.Combine(Start, Length);

    public static bool operator ==(TextSpan left, TextSpan right) => left.Equals(right);

    public static bool operator !=(TextSpan left, TextSpan right) => !(left == right);
}
