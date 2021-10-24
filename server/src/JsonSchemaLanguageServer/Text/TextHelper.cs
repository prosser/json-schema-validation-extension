// <copyright file="TextHelper.cs">Copyright (c) Peter Rosser.</copyright>

namespace Rosser.Extensions.JsonSchemaLanguageServer
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public static class TextHelper
    {
        // assumes metrics is sorted
        public static TextLineMetrics FindLineAtByteOffset(this IEnumerable<TextLineMetrics> metrics, int byteOffset)
        {
            return metrics.First(x => byteOffset >= x.ByteOffset && byteOffset <= x.ByteOffset + x.ByteCount);
        }

        public static TextLineMetrics[] CreateTextLineMetrics(string text)
        {
            string[] lines = text.Split("\r\n").ToArray();

            var metrics = new TextLineMetrics[lines.Length];

            int prevByteCount = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                int byteCount = Encoding.UTF8.GetByteCount(lines[i]);
                if (i == 0)
                {
                    metrics[i] = new TextLineMetrics(lines[i], i, 0, byteCount);
                }
                else
                {
                    metrics[i] = new TextLineMetrics(lines[i], i, prevByteCount, byteCount);
                }

                prevByteCount += byteCount + 2;
            }

            return metrics;
        }
    }
}
