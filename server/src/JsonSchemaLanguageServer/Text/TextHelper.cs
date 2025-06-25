// <copyright file="TextHelper.cs">Copyright (c) Peter Rosser.</copyright>

namespace Rosser.Extensions.JsonSchemaLanguageServer.Text;

using System.Collections.Generic;
using System.Linq;
using System.Text;

public static class TextHelper
{
    // assumes metrics is sorted
    public static TextLineMetrics FindLineAtByteOffset(this IEnumerable<TextLineMetrics> metrics, int byteOffset) => metrics.First(x => byteOffset >= x.ByteOffset && byteOffset <= x.ByteOffset + x.ByteCount);

    public static TextLineMetrics[] CreateTextLineMetrics(string text)
    {
        string[] lines = [.. text.Split("\r\n")];

        var metrics = new TextLineMetrics[lines.Length];

        int prevByteCount = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            int byteCount = Encoding.UTF8.GetByteCount(lines[i]);
            metrics[i] = i == 0 ? new TextLineMetrics(lines[i], i, 0, byteCount) : new TextLineMetrics(lines[i], i, prevByteCount, byteCount);

            prevByteCount += byteCount + 2;
        }

        return metrics;
    }
}
