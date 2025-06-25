// <copyright file="JsonExtensions.cs">Copyright (c) Peter Rosser.</copyright>

namespace Rosser.Extensions.JsonSchemaLanguageServer.Extensions;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

using Json.Schema;

public static class JsonExtensions
{
    public static async Task<JsonSchema> ToJsonSchemaAsync(this JsonElement element)
    {
        using var stream = new MemoryStream();
        var writer = new Utf8JsonWriter(stream);
        element.WriteTo(writer);
        writer.Flush();

        stream.Position = 0;

        return await JsonSchema.FromStream(stream);
    }

    public static JsonElement ToJsonElement(this JsonSchema schema)
    {
        using var stream = new MemoryStream();
        using var writer = new Utf8JsonWriter(stream);
        JsonSerializer.Serialize(writer, schema);

        stream.Position = 0;

        return JsonDocument.Parse(stream).RootElement.Clone();
    }

    public static string? GetSchemaId(this JsonElement element)
    {
        _ = element.TryGetPropertyValue("$schema", out string? value);
        return value;
    }

    public static string? GetDocumentId(this JsonElement element)
    {
        _ = element.TryGetPropertyValue("$id", out string? value);
        return value;
    }

    public static bool TryGetPropertyValue(this JsonElement element, string propertyName, [NotNullWhen(true)] out string? value)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("Invalid element, must be Object.", nameof(element));
        }

        if (!element.TryGetProperty(propertyName, out JsonElement idValue) ||
            idValue.ValueKind != JsonValueKind.String)
        {
            value = null;
            return false;
        }

        value = idValue.GetString()!;
        return true;
    }

    public static string ReadId(this JsonElement element)
    {
        return !element.TryGetProperty("id", out JsonElement idValue)
            ? throw new KeyNotFoundException()
            : idValue.GetString() ?? string.Empty;
    }
}