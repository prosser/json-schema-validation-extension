// <copyright file="TextHelperUnitTests.cs">Copyright (c) Peter Rosser.</copyright>

namespace JsonSchemaLanguageServerUnitTests
{
    using System.IO;
    using System.Linq;
    using System.Text;

    using Rosser.Extensions.JsonSchemaLanguageServer;

    using Xunit;
    using Xunit.Abstractions;

    public class TextHelperUnitTests
    {
        private readonly ITestOutputHelper output;

        public TextHelperUnitTests(ITestOutputHelper output)
        {
            this.output = output;
        }

        [Fact]
        public void LineOffsets()
        {
            string text = File.ReadAllText(@"Content\LoremIpsum.txt");
            string[] lines = text.Split("\r\n");

            var uut = TextHelper.CreateTextLineMetrics(text);

            var expected = new TextLineMetrics[]
            {
                new(lines[0], 0, 0, Encoding.UTF8.GetByteCount(lines[0])),
                new(lines[1], 1, 180, Encoding.UTF8.GetByteCount(lines[1])),
                new(lines[2], 2, 271, Encoding.UTF8.GetByteCount(lines[2])),
                new(lines[3], 3, 372, Encoding.UTF8.GetByteCount(lines[3])),
                new(lines[4], 4, 445, Encoding.UTF8.GetByteCount(lines[4])),
                new(lines[5], 5, 512, Encoding.UTF8.GetByteCount(lines[5])),
                new(lines[6], 6, 565, Encoding.UTF8.GetByteCount(lines[6])),
                new(lines[7], 7, 611, Encoding.UTF8.GetByteCount(lines[7])),
                new(lines[8], 8, 683, Encoding.UTF8.GetByteCount(lines[8])),
                new(lines[9], 9, 743, Encoding.UTF8.GetByteCount(lines[9])),
                new(lines[10], 10, 792, Encoding.UTF8.GetByteCount(lines[10])),
                new(lines[11], 11, 871, Encoding.UTF8.GetByteCount(lines[11])),
                new(lines[12], 12, 963, Encoding.UTF8.GetByteCount(lines[12]))
            };

            this.output.WriteLine("Expected   : Actual");
            this.output.WriteLine("---------- : ---------------");
            this.output.WriteLine(string.Join('\n',
                expected.Select((x, i) =>
                    $"({x.ByteOffset,3}, {x.ByteCount,3}) : ({uut[i].ByteOffset,3}, {uut[i].ByteCount,3})")));

            Assert.Equal(expected, uut);
        }
    }
}
