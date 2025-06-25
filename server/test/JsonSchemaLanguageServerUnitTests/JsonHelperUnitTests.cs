// <copyright file="JsonHelperUnitTests.cs">Copyright (c) Peter Rosser.</copyright>

namespace JsonSchemaLanguageServerUnitTests;

using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Text.Json;

using Json.Pointer;

using Rosser.Extensions.JsonSchemaLanguageServer;

using Xunit;

public class JsonHelperUnitTests
{
    public static TheoryData<JsonTestData> GetJsonData()
    {
        string json = File.ReadAllText(@"Content\ReadToJsonPointer1.json");

        return
        [
            new() { Path = "/stringProperty", Start = 23, End = 31, ExpectedTokenType = JsonTokenType.String, Json = json },
            new() { Path = "/numberProperty", Start = 54, End = 57, ExpectedTokenType = JsonTokenType.Number, Json = json },
            new() { Path = "/arrayProperty", Start = 79, End = 90, ExpectedTokenType = JsonTokenType.StartArray, Json = json },
            new() { Path = "/arrayProperty/0", Start = 81, End = 82, ExpectedTokenType = JsonTokenType.Number, Json = json },
            new() { Path = "/arrayProperty/1", Start = 84, End = 85, ExpectedTokenType = JsonTokenType.Number, Json = json },
            new() { Path = "/arrayProperty/2", Start = 87, End = 88, ExpectedTokenType = JsonTokenType.Number, Json = json },
            new() { Path = "/objectProperty", Start = 113, End = 214, ExpectedTokenType = JsonTokenType.StartObject, Json = json },
            new() { Path = "/objectProperty/stringProperty", Start = 138, End = 146, ExpectedTokenType = JsonTokenType.String, Json = json },
            new() { Path = "/objectProperty/numberProperty", Start = 171, End = 174, ExpectedTokenType = JsonTokenType.Number, Json = json },
            new() { Path = "/objectProperty/arrayProperty", Start = 198, End = 209, ExpectedTokenType = JsonTokenType.StartArray, Json = json },
            new() { Path = "/objectProperty/arrayProperty/0", Start = 200, End = 201, ExpectedTokenType = JsonTokenType.Number, Json = json },
            new() { Path = "/objectProperty/arrayProperty/1", Start = 203, End = 204, ExpectedTokenType = JsonTokenType.Number, Json = json },
            new() { Path = "/objectProperty/arrayProperty/2", Start = 206, End = 207, ExpectedTokenType = JsonTokenType.Number, Json = json },
            new() { Path = "/nestedObjectProperty", Start = 243, End = 382, ExpectedTokenType = JsonTokenType.StartObject, Json = json },
            new() { Path = "/nestedObjectProperty/objectProperty", Start = 268, End = 377, ExpectedTokenType = JsonTokenType.StartObject, Json = json },
            new() { Path = "/nestedObjectProperty/objectProperty/stringProperty", Start = 295, End = 303, ExpectedTokenType = JsonTokenType.String, Json = json },
            new() { Path = "/nestedObjectProperty/objectProperty/numberProperty", Start = 330, End = 333, ExpectedTokenType = JsonTokenType.Number, Json = json },
            new() { Path = "/nestedObjectProperty/objectProperty/arrayProperty", Start = 359, End = 370, ExpectedTokenType = JsonTokenType.StartArray, Json = json },
            new() { Path = "/nestedObjectProperty/objectProperty/arrayProperty/0", Start = 361, End = 362, ExpectedTokenType = JsonTokenType.Number, Json = json },
            new() { Path = "/nestedObjectProperty/objectProperty/arrayProperty/1", Start = 364, End = 365, ExpectedTokenType = JsonTokenType.Number, Json = json },
            new() { Path = "/nestedObjectProperty/objectProperty/arrayProperty/2", Start = 367, End = 368, ExpectedTokenType = JsonTokenType.Number, Json = json },
            new() { Path = "/nestedArrayProperty", Start = 410, End = 469, ExpectedTokenType = JsonTokenType.StartArray, Json = json },
            new() { Path = "/nestedArrayProperty/0", Start = 417, End = 428, ExpectedTokenType = JsonTokenType.StartArray, Json = json },
            new() { Path = "/nestedArrayProperty/0/0", Start = 419, End = 420, ExpectedTokenType = JsonTokenType.Number, Json = json },
            new() { Path = "/nestedArrayProperty/0/1", Start = 422, End = 423, ExpectedTokenType = JsonTokenType.Number, Json = json },
            new() { Path = "/nestedArrayProperty/0/2", Start = 425, End = 426, ExpectedTokenType = JsonTokenType.Number, Json = json },
            new() { Path = "/nestedArrayProperty/1", Start = 435, End = 446, ExpectedTokenType = JsonTokenType.StartArray, Json = json },
            new() { Path = "/nestedArrayProperty/1/0", Start = 437, End = 438, ExpectedTokenType = JsonTokenType.Number, Json = json },
            new() { Path = "/nestedArrayProperty/1/1", Start = 440, End = 441, ExpectedTokenType = JsonTokenType.Number, Json = json },
            new() { Path = "/nestedArrayProperty/1/2", Start = 443, End = 444, ExpectedTokenType = JsonTokenType.Number, Json = json },
            new() { Path = "/nestedArrayProperty/2", Start = 453, End = 464, ExpectedTokenType = JsonTokenType.StartArray, Json = json },
            new() { Path = "/nestedArrayProperty/2/0", Start = 455, End = 456, ExpectedTokenType = JsonTokenType.Number, Json = json },
            new() { Path = "/nestedArrayProperty/2/1", Start = 458, End = 459, ExpectedTokenType = JsonTokenType.Number, Json = json },
            new() { Path = "/nestedArrayProperty/2/2", Start = 461, End = 462, ExpectedTokenType = JsonTokenType.Number, Json = json },
            new() { Path = "/arrayInObjectProperty", Start = 499, End = 560, ExpectedTokenType = JsonTokenType.StartObject, Json = json },
            new() { Path = "/arrayInObjectProperty/array1", Start = 516, End = 527, ExpectedTokenType = JsonTokenType.StartArray, Json = json },
            new() { Path = "/arrayInObjectProperty/array1/0", Start = 518, End = 519, ExpectedTokenType = JsonTokenType.Number, Json = json },
            new() { Path = "/arrayInObjectProperty/array1/1", Start = 521, End = 522, ExpectedTokenType = JsonTokenType.Number, Json = json },
            new() { Path = "/arrayInObjectProperty/array1/2", Start = 524, End = 525, ExpectedTokenType = JsonTokenType.Number, Json = json },
            new() { Path = "/arrayInObjectProperty/array2", Start = 544, End = 555, ExpectedTokenType = JsonTokenType.StartArray, Json = json },
            new() { Path = "/arrayInObjectProperty/array2/0", Start = 546, End = 547, ExpectedTokenType = JsonTokenType.Number, Json = json },
            new() { Path = "/arrayInObjectProperty/array2/1", Start = 549, End = 550, ExpectedTokenType = JsonTokenType.Number, Json = json },
            new() { Path = "/arrayInObjectProperty/array2/2", Start = 552, End = 553, ExpectedTokenType = JsonTokenType.Number, Json = json },
            new() { Path = "/arrayInObjectPropertyArray", Start = 595, End = 696, ExpectedTokenType = JsonTokenType.StartObject, Json = json },
            new() { Path = "/arrayInObjectPropertyArray/outerArray", Start = 616, End = 691, ExpectedTokenType = JsonTokenType.StartArray, Json = json },
            new() { Path = "/arrayInObjectPropertyArray/outerArray/0", Start = 625, End = 650, ExpectedTokenType = JsonTokenType.StartObject, Json = json },
            new() { Path = "/arrayInObjectPropertyArray/outerArray/0/array1", Start = 637, End = 648, ExpectedTokenType = JsonTokenType.StartArray, Json = json },
            new() { Path = "/arrayInObjectPropertyArray/outerArray/0/array1/0", Start = 639, End = 640, ExpectedTokenType = JsonTokenType.Number, Json = json },
            new() { Path = "/arrayInObjectPropertyArray/outerArray/0/array1/1", Start = 642, End = 643, ExpectedTokenType = JsonTokenType.Number, Json = json },
            new() { Path = "/arrayInObjectPropertyArray/outerArray/0/array1/2", Start = 645, End = 646, ExpectedTokenType = JsonTokenType.Number, Json = json },
            new() { Path = "/arrayInObjectPropertyArray/outerArray/1", Start = 659, End = 684, ExpectedTokenType = JsonTokenType.StartObject, Json = json },
            new() { Path = "/arrayInObjectPropertyArray/outerArray/1/array2", Start = 671, End = 682, ExpectedTokenType = JsonTokenType.StartArray, Json = json },
            new() { Path = "/arrayInObjectPropertyArray/outerArray/1/array2/0", Start = 673, End = 674, ExpectedTokenType = JsonTokenType.Number, Json = json },
            new() { Path = "/arrayInObjectPropertyArray/outerArray/1/array2/1", Start = 676, End = 677, ExpectedTokenType = JsonTokenType.Number, Json = json },
            new() { Path = "/arrayInObjectPropertyArray/outerArray/1/array2/2", Start = 679, End = 680, ExpectedTokenType = JsonTokenType.Number, Json = json },
        ];
    }

    public static TheoryData<SkipObjectOrArrayData> GetSkipObjectOrArrayData()
    {
        string json = File.ReadAllText(@"Content\ReadToJsonPointer1.json");

        return [
            new("nestedObjectProperty", JsonTokenType.StartObject, 244, JsonTokenType.EndObject, 382, json),
            new("nestedArrayProperty", JsonTokenType.StartArray, 411, JsonTokenType.EndArray, 469, json),
            new("arrayInObjectProperty", JsonTokenType.StartObject, 500, JsonTokenType.EndObject, 560, json),
            new("arrayInObjectPropertyArray", JsonTokenType.StartObject, 596, JsonTokenType.EndObject, 696, json),
        ];
    }

    [Theory]
    [MemberData(nameof(GetJsonData))]
    public void GetElementBoundsIsAccurate(JsonTestData testData)
    {
        (long start, long end) = JsonHelper.GetElementBounds(testData.Json, testData.Path);

        Assert.Equal((testData.Start, testData.End), (start, end));
    }

    [Fact]
    public void NotFoundJsonPointerReadsEntireJson()
    {
        string json = File.ReadAllText(@"Content\ReadToJsonPointer1.json");
        byte[] buffer = Encoding.UTF8.GetBytes(json);
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
    [MemberData(nameof(GetJsonData))]
    public void ReadToJsonPointerIsAccurate(JsonTestData testData)
    {
        var jsonPointer = JsonPointer.Parse(testData.Path);
        using var doc = JsonDocument.Parse(testData.Json);
        var bytes = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(testData.Json));
        var reader = new Utf8JsonReader(bytes);
        JsonHelper.ReadToJsonPointer(ref reader, jsonPointer);

        Assert.Equal(testData.Start, reader.TokenStartIndex);
        Assert.Equal(testData.ExpectedTokenType, reader.TokenType);
    }

    [Theory]
    [MemberData(nameof(GetSkipObjectOrArrayData))]
    public void SkipObjectOrArraySkipsOnlyIt(SkipObjectOrArrayData data) //string propertyName, JsonTokenType expectedPreconditionToken, int expectedPreconditionTokenOffset, JsonTokenType expectedPostconditionToken, int expectedPostconditionTokenOffset, string json)
    {
        byte[] buffer = Encoding.UTF8.GetBytes(data.Json);
        var bytes = new ReadOnlySequence<byte>(buffer);
        var reader = new Utf8JsonReader(bytes);

        ReadToProperty(ref reader, data.PropertyName);

        Assert.Equal(data.ExpectedPreconditionToken, reader.TokenType);
        Assert.Equal(data.ExpectedPreconditionTokenOffset, reader.BytesConsumed);
        JsonHelper.SkipObjectOrArray(ref reader);

        Assert.Equal(data.ExpectedPostconditionToken, reader.TokenType);
        Assert.Equal(data.ExpectedPostconditionTokenOffset, reader.BytesConsumed);

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

    public sealed class JsonTestData : Xunit.Abstractions.IXunitSerializable
    {
        public JsonTestData()
        { }

        public JsonTestData(string path, long start, long end, JsonTokenType expectedTokenType, string json)
        {
            this.Path = path;
            this.Start = start;
            this.End = end;
            this.ExpectedTokenType = expectedTokenType;
            this.Json = json;
        }

        public long End { get; set; }
        public JsonTokenType ExpectedTokenType { get; set; }
        public string Json { get; set; }
        public string Path { get; set; }
        public long Start { get; set; }

        public void Deserialize(Xunit.Abstractions.IXunitSerializationInfo info)
        {
            this.Path = info.GetValue<string>(nameof(this.Path));
            this.Start = info.GetValue<long>(nameof(this.Start));
            this.End = info.GetValue<long>(nameof(this.End));
            this.ExpectedTokenType = info.GetValue<JsonTokenType>(nameof(this.ExpectedTokenType));
            this.Json = info.GetValue<string>(nameof(this.Json));
        }

        public void Serialize(Xunit.Abstractions.IXunitSerializationInfo info)
        {
            info.AddValue(nameof(this.Path), this.Path);
            info.AddValue(nameof(this.Start), this.Start);
            info.AddValue(nameof(this.End), this.End);
            info.AddValue(nameof(this.ExpectedTokenType), this.ExpectedTokenType);
            info.AddValue(nameof(this.Json), this.Json);
        }
    }

    public sealed class SkipObjectOrArrayData : Xunit.Abstractions.IXunitSerializable
    {
        public SkipObjectOrArrayData()
        { }

        public SkipObjectOrArrayData(string propertyName, JsonTokenType expectedPreconditionToken, int expectedPreconditionTokenOffset, JsonTokenType expectedPostconditionToken, int expectedPostconditionTokenOffset, string json)
        {
            this.PropertyName = propertyName;
            this.ExpectedPreconditionToken = expectedPreconditionToken;
            this.ExpectedPreconditionTokenOffset = expectedPreconditionTokenOffset;
            this.ExpectedPostconditionToken = expectedPostconditionToken;
            this.ExpectedPostconditionTokenOffset = expectedPostconditionTokenOffset;
            this.Json = json;
        }

        public JsonTokenType ExpectedPostconditionToken { get; set; }
        public int ExpectedPostconditionTokenOffset { get; set; }
        public JsonTokenType ExpectedPreconditionToken { get; set; }
        public int ExpectedPreconditionTokenOffset { get; set; }
        public string Json { get; set; }
        public string PropertyName { get; set; }

        public void Deserialize(Xunit.Abstractions.IXunitSerializationInfo info)
        {
            this.PropertyName = info.GetValue<string>(nameof(this.PropertyName));
            this.ExpectedPreconditionToken = info.GetValue<JsonTokenType>(nameof(this.ExpectedPreconditionToken));
            this.ExpectedPreconditionTokenOffset = info.GetValue<int>(nameof(this.ExpectedPreconditionTokenOffset));
            this.ExpectedPostconditionToken = info.GetValue<JsonTokenType>(nameof(this.ExpectedPostconditionToken));
            this.ExpectedPostconditionTokenOffset = info.GetValue<int>(nameof(this.ExpectedPostconditionTokenOffset));
            this.Json = info.GetValue<string>(nameof(this.Json));
        }

        public void Serialize(Xunit.Abstractions.IXunitSerializationInfo info)
        {
            info.AddValue(nameof(this.PropertyName), this.PropertyName);
            info.AddValue(nameof(this.ExpectedPreconditionToken), this.ExpectedPreconditionToken);
            info.AddValue(nameof(this.ExpectedPreconditionTokenOffset), this.ExpectedPreconditionTokenOffset);
            info.AddValue(nameof(this.ExpectedPostconditionToken), this.ExpectedPostconditionToken);
            info.AddValue(nameof(this.ExpectedPostconditionTokenOffset), this.ExpectedPostconditionTokenOffset);
            info.AddValue(nameof(this.Json), this.Json);
        }
    }
}