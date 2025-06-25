// <copyright file="TextLineMetrics.cs">Copyright (c) Peter Rosser.</copyright>

namespace Rosser.Extensions.JsonSchemaLanguageServer.Text;

using System;
using System.Diagnostics;
using System.Text;

[DebuggerDisplay("Line: {LineIndex}, bOffset: {ByteOffset}, bCount: {ByteCount}, cCount: {CharCount}")]
public readonly struct TextLineMetrics(string line, int lineIndex, int byteOffset, int byteCount) : IEquatable<TextLineMetrics>
{
    public string Line { get; } = line;
    public int LineIndex { get; } = lineIndex;
    public int ByteOffset { get; } = byteOffset;
    public int ByteCount { get; } = byteCount;
    public int CharCount => this.Line.Length;

    public bool Equals(TextLineMetrics other)
    {
        bool isEqual =
            this.LineIndex == other.LineIndex &&
            this.ByteOffset == other.ByteOffset &&
            this.ByteCount == other.ByteCount &&
            (other.Line?.Equals(this.Line) ?? false);

        return isEqual;
    }

    public override bool Equals(object? obj) => obj is TextLineMetrics other && this.Equals(other);

    public override int GetHashCode()
        => HashCode.Combine(this.Line, this.LineIndex, this.ByteOffset, this.ByteCount);

    public int GetCharOffset(int byteOffsetFromStartOfText)
    {
        int localByteOffset = byteOffsetFromStartOfText - this.ByteOffset;
        byte[] utf8Bytes = Encoding.UTF8.GetBytes(this.Line);
        int charsBeforeOffset = Encoding.UTF8.GetCharCount(utf8Bytes, 0, localByteOffset - 1);
        return charsBeforeOffset + 1;
    }
    public static bool operator ==(TextLineMetrics left, TextLineMetrics right) => left.Equals(right);

    public static bool operator !=(TextLineMetrics left, TextLineMetrics right) => !(left == right);
}
