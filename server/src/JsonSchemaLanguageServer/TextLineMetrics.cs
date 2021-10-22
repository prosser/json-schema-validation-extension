// <copyright file="TextHelper.cs">Copyright (c) Peter Rosser.</copyright>

namespace Rosser.Extensions.JsonSchemaLanguageServer
{
    using System;
    using System.Diagnostics;
    using System.Text;

    [DebuggerDisplay("Line: {LineIndex}, bOffset: {ByteOffset}, bCount: {ByteCount}, cCount: {CharCount}")]
    public struct TextLineMetrics : IEquatable<TextLineMetrics>
    {
        public TextLineMetrics(string line, int lineIndex, int byteOffset, int byteCount)
        {
            this.Line = line;
            this.LineIndex = lineIndex;
            this.ByteOffset = byteOffset;
            this.ByteCount = byteCount;
        }

        public string Line { get; }
        public int LineIndex { get; }
        public int ByteOffset { get; }
        public int ByteCount { get; }
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

        public override bool Equals(object? obj)
        {
            if (obj is not TextLineMetrics other)
            {
                return false;
            }

            return this.Equals(other);
        }

        public override int GetHashCode()
            => HashCode.Combine(this.Line, this.LineIndex, this.ByteOffset, this.ByteCount);

        public int GetCharOffset(int byteOffsetFromStartOfText)
        {
            int localByteOffset = byteOffsetFromStartOfText - this.ByteOffset;
            byte[] utf8Bytes = Encoding.UTF8.GetBytes(this.Line);
            int charsBeforeOffset = Encoding.UTF8.GetCharCount(utf8Bytes, 0, localByteOffset - 1);
            return charsBeforeOffset + 1;
        }
    }
}
