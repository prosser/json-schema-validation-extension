// <copyright file="JsonHelper.cs">Copyright (c) Peter Rosser.</copyright>

#define EXTRA_DEBUG_OUTPUT

namespace Rosser.Extensions.JsonSchemaLanguageServer;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;

using Json.Pointer;

public static class JsonHelper
{
    public static (long start, long end) GetElementBounds(string text, string jsonPointer)
        => GetElementBounds(text, JsonPointer.Parse(jsonPointer));

    public static (long start, long end) GetElementBounds(string text, JsonPointer jsonPointer)
    {
        Span<byte> bytes = new(Encoding.UTF8.GetBytes(text));
        var reader = new Utf8JsonReader(bytes);
        return GetElementBounds(ref reader, jsonPointer);
    }

    public static (long start, long end) GetElementBounds(ref Utf8JsonReader reader, JsonPointer jsonPointer)
    {
        ReadToJsonPointer(ref reader, jsonPointer);

        long start = reader.TokenStartIndex;
        long chars = Encoding.UTF8.GetCharCount(reader.ValueSpan);

        if (reader.TokenType is JsonTokenType.String or JsonTokenType.PropertyName)
        {
            chars += 2;
        }

        long end = start + chars;

        if (!reader.TokenType.IsScalar())
        {
            SkipObjectOrArray(ref reader);
            chars += reader.TokenStartIndex - start + Encoding.UTF8.GetCharCount(reader.ValueSpan) - 1;
            end = start + chars;
        }

        return (start, end);
    }

    public static void SkipObjectOrArray(ref Utf8JsonReader reader)
    {
        JsonTokenType skipTo = reader.TokenType switch
        {
            JsonTokenType.StartObject => JsonTokenType.EndObject,
            JsonTokenType.StartArray => JsonTokenType.EndArray,
            _ => throw new InvalidOperationException()
        };

        Debug.Assert(reader.TokenType is JsonTokenType.StartArray or JsonTokenType.StartObject);
        int startingDepth = reader.CurrentDepth;
        while (reader.Read())
        {
            Debug.WriteLine(reader.TokenType);
            if (reader.CurrentDepth == startingDepth && reader.TokenType == skipTo)
            {
                break;
            }
        }

        Debug.Assert(reader.TokenType == skipTo);
    }

    public static bool ReadToNonComment(ref Utf8JsonReader reader)
    {
        while (true)
        {
            if (!reader.Read())
            {
                return false;
            }

            if (reader.TokenType != JsonTokenType.Comment)
            {
                return true;
            }
        }
    }

    public static bool IsScalar(this JsonTokenType tokenType)
        => tokenType is not JsonTokenType.StartObject and not JsonTokenType.StartArray and not JsonTokenType.EndObject and not JsonTokenType.EndArray;

    public static void ReadToJsonPointer(ref Utf8JsonReader reader, JsonPointer jsonPointer)
    {
        PathStack path = new();
        bool found = false;
        PathStack find = new(jsonPointer);
        while (!found && ReadToNonComment(ref reader))
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.StartArray:
                    found = FindInArray(find, ref reader, path);
                    break;

                case JsonTokenType.StartObject:
                    found = FindInObject(find, ref reader, path);
                    break;

                default:
                    continue;
            }
        }

        if (!found)
        {
            throw new InvalidOperationException($"JSON Pointer '{jsonPointer}' was not found.");
        }
    }

    private static bool FindInArray(PathStack find, ref Utf8JsonReader reader, PathStack path)
    {
        path.Push(-1);
        bool found = false;
        bool any = false;
        while (!found && ReadToNonComment(ref reader) && reader.TokenType != JsonTokenType.EndArray)
        {
            any = true;
            path.Increment();
            int matchingSegments = path.GetMatchingPrefixLength(find, 0);

            found = matchingSegments == find.Count;
            if (!found)
            {
                if (matchingSegments == path.Count)
                {
                    found = reader.TokenType switch
                    {
                        JsonTokenType.StartArray => FindInArray(find, ref reader, path),
                        JsonTokenType.StartObject => FindInObject(find, ref reader, path),
                        _ => path.Equals(find),
                    };
                }
                else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                {
                    SkipObjectOrArray(ref reader);
                }
            }
        }

        if (!found)
        {
            if (any)
            {
                _ = path.Pop(); // pop the index
            }
        }

        return found;
    }

    private static bool FindInObject(PathStack find, ref Utf8JsonReader reader, PathStack path)
    {
        bool found = false;
        while (!found && ReadToNonComment(ref reader) && reader.TokenType != JsonTokenType.EndObject)
        {
            string propertyName = reader.GetString() ?? throw new InvalidOperationException();
            path.Push(propertyName);
            int matchingSegments = path.GetMatchingPrefixLength(find, 0);
            found = matchingSegments == find.Count;

            // move the reader to the value
            _ = ReadToNonComment(ref reader);
            if (!found)
            {
                if (matchingSegments == path.Count)
                {
                    switch (reader.TokenType)
                    {
                        case JsonTokenType.StartArray:
                            found = FindInArray(find, ref reader, path);
                            break;

                        case JsonTokenType.StartObject:
                            found = FindInObject(find, ref reader, path);
                            break;
                    }
                }
                else if (reader.TokenType is JsonTokenType.StartObject or JsonTokenType.StartArray)
                {
                    SkipObjectOrArray(ref reader);
                }

                if (!found)
                {
                    _ = path.Pop();
                }
            }
        }

        return found;
    }

    [DebuggerDisplay("{ToString()}")]
    private class PathStack : Stack<PathSegment>
    {
        public PathStack()
        {
        }

        public PathStack(JsonPointer pointer)
            : this(pointer.Select(x => x))
        {
        }

        public PathStack(string str)
            : this(str.Split('/').Skip(1))
        {
        }

        public PathStack(IEnumerable<string> segments)
        {
            foreach (string segStr in segments)
            {
                if (string.IsNullOrWhiteSpace(segStr))
                {
                    throw new ArgumentException("Invalid JSON Pointer format", nameof(segments));
                }

                if (int.TryParse(segStr, out int index))
                {
                    this.Push(index);
                }
                else
                {
                    this.Push(segStr);
                }
            }
        }

        public void Increment()
        {
            PathSegment segment = this.Pop();
            if (segment.IsIndex)
            {
                segment++;
            }
            else
            {
                this.Push(segment);
                segment = 0;
            }

            this.Push(segment);
        }

        public int GetMatchingPrefixLength(PathStack other, int assumeMatchCount)
        {
            if (ReferenceEquals(this, other))
            {
                return this.Count;
            }

            int take = Math.Min(this.Count, other.Count);
            int len = 0;
            foreach ((PathSegment left, PathSegment right) in this
                .Reverse()
                .Skip(assumeMatchCount)
                .Take(take)
                .Zip(other
                    .Reverse()
                    .Skip(assumeMatchCount)
                    .Take(take)))
            {
                if (left.Equals(right))
                {
                    ++len;
                }
                else
                {
                    break;
                }
            }

            return len + assumeMatchCount;
        }

        public bool Equals(PathStack other)
        {
            if (ReferenceEquals(this, other))
            {
                return true;
            }

            if (this.Count != other.Count)
            {
                return false;
            }

            foreach ((PathSegment left, PathSegment right) in this.Zip(other))
            {
                if (!left.Equals(right))
                {
                    return false;
                }
            }

            return true;
        }

        public bool EndsWith(PathSegment segment) => this.Peek().Equals(segment);

        public bool StartsWith(PathStack other)
        {
            if (this.Count < other.Count)
            {
                return false;
            }

            foreach ((PathSegment left, PathSegment right) in this.Reverse().Take(other.Count).Zip(other.Reverse()))
            {
                if (!left.Equals(right))
                {
                    return false;
                }
            }

            return true;
        }

        public override string ToString() => "/" + string.Join('/', this.Reverse());
    }

    //private class PathItem
    //{
    //    public JsonTokenType TokenType { get; set; }
    //    public StringOrInt Value { get; set; }
    //}

    private struct PathSegment
    {
        public PathSegment(string str)
        {
            this.S = str;
            this.N = null;
        }

        public PathSegment(int n)
        {
            this.S = null;
            this.N = n;
        }

        public string? S;
        public int? N;

        public readonly bool IsString => this.S is not null;
        public readonly bool IsIndex => this.N is not null;
        public readonly bool IsEmpty => this.S is null && this.N is null;

        public static PathSegment Empty => new();

        public static implicit operator PathSegment(string str) => new(str);
        public static implicit operator PathSegment(int n) => new(n);

        public readonly bool Equals(PathSegment other)
        {
            return this.N == other.N &&
                this.S == other.S;
        }

        public static PathSegment operator ++(PathSegment si) => !si.IsIndex ? throw new InvalidOperationException("Not an integer") : new PathSegment(si.N!.Value + 1);

        public static PathSegment operator --(PathSegment si) => !si.IsIndex ? throw new InvalidOperationException("Not an integer") : new PathSegment(si.N!.Value - 1);

        public override readonly string? ToString() => this.S ?? this.N?.ToString();
    }
}
