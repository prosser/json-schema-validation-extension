// <copyright file="SchemaAnalyzerTests.cs">Copyright (c) Peter Rosser.</copyright>

namespace JsonSchemaLanguageServerUnitTests;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.LanguageServer.Protocol;

using Rosser.Extensions.JsonSchemaLanguageServer.Services;

using Xunit;
using Xunit.Abstractions;

public class SchemaAnalyzerTests(ITestOutputHelper output) : TestBase(output)
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public static TheoryData<TestData> GetSchemaValidationData()
    {
        var testContainer = TestDataContainer.Load(@"Content\schema-validation.json");
        return [.. testContainer.Tests];
    }

    [Theory]
    [MemberData(nameof(GetSchemaValidationData))]
    public async Task TestValidationAsync(TestData data)
    {
        Assert.NotNull(data);
        using SchemaAnalyzer analyzer = this.serviceProvider.GetRequiredService<SchemaAnalyzer>();
        await analyzer.InitializeAsync();
        List<Diagnostic> diagnostics = analyzer.Analyze(data.SchemaJson);

        Assert.Equal(data.ExpectedDiagnostics.Length, diagnostics.Count);

        foreach ((Diagnostic expected, Diagnostic actual) in data.ExpectedDiagnostics.Zip(diagnostics))
        {
            Assert.Equal(expected.Severity, actual.Severity);
            Assert.Equal(expected.Range, actual.Range);
        }
    }

    public class TestDataContainer
    {
        public TestData[] Tests { get; set; }

        public static TestDataContainer Load(string path)
        {
            SerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

            string json = File.ReadAllText(path);
            TestDataContainer container = JsonSerializer.Deserialize<TestDataContainer>(json, SerializerOptions);

            foreach (TestData test in container.Tests)
            {
                string schemaPath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "-test-" + test.Name + ".json");
                test.SchemaJson = File.ReadAllText(schemaPath);
            }

            return container;
        }
    }

    public sealed record TestData : IXunitSerializable
    {
        public Diagnostic[] ExpectedDiagnostics { get; set; }
        public string Name { get; set; }
        public string SchemaJson { get; set; }

        public void Serialize(IXunitSerializationInfo info)
        {
            info.AddValue(nameof(this.ExpectedDiagnostics), this.ExpectedDiagnostics);
            info.AddValue(nameof(this.Name), this.Name);
            info.AddValue(nameof(this.SchemaJson), this.SchemaJson);
        }

        public void Deserialize(IXunitSerializationInfo info)
        {
            this.ExpectedDiagnostics = info.GetValue<Diagnostic[]>(nameof(this.ExpectedDiagnostics));
            this.Name = info.GetValue<string>(nameof(this.Name));
            this.SchemaJson = info.GetValue<string>(nameof(this.SchemaJson));
        }
    }
}