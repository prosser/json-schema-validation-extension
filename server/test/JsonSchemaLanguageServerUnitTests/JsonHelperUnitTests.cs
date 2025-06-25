// <copyright file="JsonHelperUnitTests.cs">Copyright (c) Peter Rosser.</copyright>

namespace JsonSchemaLanguageServerUnitTests
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.Json;

    using Json.Pointer;

    using Rosser.Extensions.JsonSchemaLanguageServer;

    using Xunit;

    public class JsonHelperUnitTests
    {
        [Theory]
        [MemberData(nameof(GetElementBoundsData))]
        public void GetElementBoundsIsAccurate(string find, long expectedStart, long expectedEnd, string json)
        {
            (long start, long end) = JsonHelper.GetElementBounds(json, find);

            Assert.Equal((expectedStart, expectedEnd), (start, end));
        }

        [Fact]
        public void NotFoundJsonPointerReadsEntireJson()
        {
            string json = File.ReadAllText(@"Content\ReadToJsonPointer1.json");
            var buffer = Encoding.UTF8.GetBytes(json);
            var bytes = new ReadOnlySequence<byte>(buffer);
            var jsonPointer = JsonPointer.Parse("/foo");
            var reader = new Utf8JsonReader(bytes);

            try
            {
                JsonHelper.ReadToJsonPointer(ref reader, jsonPointer);
                Assert.True(false);
            }
            catch (InvalidOperationException)
            {
                // expected
                Assert.Equal(buffer.Length, reader.BytesConsumed);
            }
        }

        [Theory]
        [MemberData(nameof(GetReadToJsonPointerData))]
        public void ReadToJsonPointerIsAccurate(string find, long expectedStart, JsonTokenType expectedTokenType, string json)
        {
            var jsonPointer = JsonPointer.Parse(find);
            using var doc = JsonDocument.Parse(json);
            var bytes = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(json));
            var reader = new Utf8JsonReader(bytes);
            JsonHelper.ReadToJsonPointer(ref reader, jsonPointer);

            Assert.Equal(expectedStart, reader.TokenStartIndex);
            Assert.Equal(expectedTokenType, reader.TokenType);
        }

        [Theory]
        [MemberData(nameof(GetSkipObjectOrArrayData))]
        public void SkipObjectOrArraySkipsOnlyIt(string propertyName, JsonTokenType expectedPreconditionToken, int expectedPreconditionTokenOffset, JsonTokenType expectedPostconditionToken, int expectedPostconditionTokenOffset, string json)
        {
            var buffer = Encoding.UTF8.GetBytes(json);
            var bytes = new ReadOnlySequence<byte>(buffer);
            var reader = new Utf8JsonReader(bytes);

            ReadToProperty(ref reader, propertyName);

            Assert.Equal(expectedPreconditionToken, reader.TokenType);
            Assert.Equal(expectedPreconditionTokenOffset, reader.BytesConsumed);
            JsonHelper.SkipObjectOrArray(ref reader);

            Assert.Equal(expectedPostconditionToken, reader.TokenType);
            Assert.Equal(expectedPostconditionTokenOffset, reader.BytesConsumed);

            static void ReadToProperty(ref Utf8JsonReader reader, string propertyName)
            {
                bool keepGoing;
                do
                {
                    keepGoing = reader.Read();

                    if (keepGoing && reader.TokenType == JsonTokenType.PropertyName)
                    {
                        if (reader.GetString() == propertyName)
                        {
                            keepGoing = false;
                            Assert.True(reader.Read()); // leave it on the StartObject
                        }
                    }
                } while (keepGoing);
            }
        }

        public static IEnumerable<object[]> GetElementBoundsData()
        {
            var jsonData = GetJsonData();

            foreach (JsonTestData item in jsonData)
            {
                yield return new object[] { item.Path, item.Start, item.End, item.Json };
            }
        }

        public static IEnumerable<object[]> GetReadToJsonPointerData()
        {
            var jsonData = GetJsonData();

            foreach (JsonTestData item in jsonData)
            {
                yield return new object[] { item.Path, item.Start, item.ExpectedTokenType, item.Json };
            }
        }

        public class JsonTestData
        {
            public JsonTestData(string path, long start, long end, JsonTokenType expectedTokenType)
            {
                this.Path = path;
                this.Start = start;
                this.End = end;
                this.ExpectedTokenType = expectedTokenType;
            }

            public string Json { get; set; }
            public string Path { get; }
            public long Start { get; }
            public long End { get; }
            public JsonTokenType ExpectedTokenType { get; }
        }

        public static IEnumerable<JsonTestData> GetJsonData()
        {
            string json = File.ReadAllText(@"Content\ReadToJsonPointer1.json");

            var data = new JsonTestData[]
            {
                new("/stringProperty", 23, 31, JsonTokenType.String),
                new("/numberProperty", 54, 57, JsonTokenType.Number),
                new("/arrayProperty", 79, 90, JsonTokenType.StartArray),
                new("/arrayProperty/0", 81, 82, JsonTokenType.Number),
                new("/arrayProperty/1", 84, 85, JsonTokenType.Number),
                new("/arrayProperty/2", 87, 88, JsonTokenType.Number),
                new("/objectProperty", 113, 214, JsonTokenType.StartObject),
                new("/objectProperty/stringProperty", 138, 146, JsonTokenType.String),
                new("/objectProperty/numberProperty", 171, 174, JsonTokenType.Number),
                new("/objectProperty/arrayProperty", 198, 209, JsonTokenType.StartArray),
                new("/objectProperty/arrayProperty/0", 200, 201, JsonTokenType.Number),
                new("/objectProperty/arrayProperty/1", 203, 204, JsonTokenType.Number),
                new("/objectProperty/arrayProperty/2", 206, 207, JsonTokenType.Number),
                new("/nestedObjectProperty", 243, 382, JsonTokenType.StartObject),
                new("/nestedObjectProperty/objectProperty", 268, 377, JsonTokenType.StartObject),
                new("/nestedObjectProperty/objectProperty/stringProperty", 295, 303, JsonTokenType.String),
                new("/nestedObjectProperty/objectProperty/numberProperty", 330, 333, JsonTokenType.Number),
                new("/nestedObjectProperty/objectProperty/arrayProperty", 359, 370, JsonTokenType.StartArray),
                new("/nestedObjectProperty/objectProperty/arrayProperty/0", 361, 362, JsonTokenType.Number),
                new("/nestedObjectProperty/objectProperty/arrayProperty/1", 364, 365, JsonTokenType.Number),
                new("/nestedObjectProperty/objectProperty/arrayProperty/2", 367, 368, JsonTokenType.Number),
                new("/nestedArrayProperty", 410, 469, JsonTokenType.StartArray),
                new("/nestedArrayProperty/0", 417, 428, JsonTokenType.StartArray),
                new("/nestedArrayProperty/0/0", 419, 420, JsonTokenType.Number),
                new("/nestedArrayProperty/0/1", 422, 423, JsonTokenType.Number),
                new("/nestedArrayProperty/0/2", 425, 426, JsonTokenType.Number),
                new("/nestedArrayProperty/1", 435, 446, JsonTokenType.StartArray),
                new("/nestedArrayProperty/1/0", 437, 438, JsonTokenType.Number),
                new("/nestedArrayProperty/1/1", 440, 441, JsonTokenType.Number),
                new("/nestedArrayProperty/1/2", 443, 444, JsonTokenType.Number),
                new("/nestedArrayProperty/2", 453, 464, JsonTokenType.StartArray),
                new("/nestedArrayProperty/2/0", 455, 456, JsonTokenType.Number),
                new("/nestedArrayProperty/2/1", 458, 459, JsonTokenType.Number),
                new("/nestedArrayProperty/2/2", 461, 462, JsonTokenType.Number),
                new("/arrayInObjectProperty", 478, 536, JsonTokenType.StartObject),
                new("/arrayInObjectProperty/array1", 494, 505, JsonTokenType.StartArray),
                new("/arrayInObjectProperty/array1/0", 518, 519, JsonTokenType.Number),
                new("/arrayInObjectProperty/array1/1", 521, 522, JsonTokenType.Number),
                new("/arrayInObjectProperty/array1/2", 524, 525, JsonTokenType.Number),
                new("/arrayInObjectProperty/array2", 544, 555, JsonTokenType.StartArray),
                new("/arrayInObjectProperty/array2/0", 546, 547, JsonTokenType.Number),
                new("/arrayInObjectProperty/array2/1", 549, 550, JsonTokenType.Number),
                new("/arrayInObjectProperty/array2/2", 552, 553, JsonTokenType.Number),
                new("/arrayInObjectPropertyArray", 595, 696, JsonTokenType.StartObject),
                new("/arrayInObjectPropertyArray/outerArray", 616, 691, JsonTokenType.StartArray),
                new("/arrayInObjectPropertyArray/outerArray/0", 625, 650, JsonTokenType.StartObject),
                new("/arrayInObjectPropertyArray/outerArray/0/array1", 637, 648, JsonTokenType.StartArray),
                new("/arrayInObjectPropertyArray/outerArray/0/array1/0", 639, 640, JsonTokenType.Number),
                new("/arrayInObjectPropertyArray/outerArray/0/array1/1", 642, 643, JsonTokenType.Number),
                new("/arrayInObjectPropertyArray/outerArray/0/array1/2", 645, 646, JsonTokenType.Number),
                new("/arrayInObjectPropertyArray/outerArray/1", 659, 684, JsonTokenType.StartObject),
                new("/arrayInObjectPropertyArray/outerArray/1/array2", 671, 682, JsonTokenType.StartArray),
                new("/arrayInObjectPropertyArray/outerArray/1/array2/0", 673, 674, JsonTokenType.Number),
                new("/arrayInObjectPropertyArray/outerArray/1/array2/1", 676, 677, JsonTokenType.Number),
                new("/arrayInObjectPropertyArray/outerArray/1/array2/2", 679, 680, JsonTokenType.Number),
            };

            foreach (JsonTestData item in data)
            {
                item.Json = json;
                yield return item;
            }
        }

        public static IEnumerable<object[]> GetSkipObjectOrArrayData()
        {
            string json = File.ReadAllText(@"Content\ReadToJsonPointer1.json");

            var data = new object[][]
            {
                new object[] { "objectProperty", JsonTokenType.StartObject, 114, JsonTokenType.EndObject, 214 },
                new object[] { "nestedObjectProperty", JsonTokenType.StartObject, 244, JsonTokenType.EndObject, 382 },
                new object[] { "nestedArrayProperty", JsonTokenType.StartArray, 411, JsonTokenType.EndArray, 469 },
                new object[] { "arrayInObjectProperty", JsonTokenType.StartObject, 500, JsonTokenType.EndObject, 560 },
                new object[] { "arrayInObjectPropertyArray", JsonTokenType.StartObject, 596, JsonTokenType.EndObject, 696 },
            };

            return data.Select(item => new object[] { item[0], item[1], item[2], item[3], item[4], json });
        }

    }
}
