// <copyright file="SchemaAnalyzerTests.cs">Copyright (c) Peter Rosser.</copyright>

namespace JsonSchemaLanguageServerUnitTests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Text.Json.Serialization;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.LanguageServer.Protocol;

    using Moq;

    using Rosser.Extensions.JsonSchemaLanguageServer;

    using Xunit;
    using Xunit.Abstractions;

    public class SchemaAnalyzerTests
    {
        private readonly ITestOutputHelper output;
        private readonly ILogger<Server> logger;

        public SchemaAnalyzerTests(ITestOutputHelper output)
        {
            this.output = output;
            var loggerMock = new Mock<ILogger<Server>>();
            loggerMock.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()))
                .Callback(new InvocationAction(action =>
                {
                    this.output.WriteLine($"{action.Arguments[0]} {action.Arguments[2]?.ToString()}");
                }));

            this.logger = loggerMock.Object;
        }

        [Theory]
        [MemberData(nameof(GetSchemaValidationData))]
        public async Task TestValidationAsync(TestData data)
        {
            Assert.NotNull(data);
            SchemaAnalyzer analyzer = new();
            await analyzer.InitializeAsync();
            List<Diagnostic> diagnostics = analyzer.Analyze(data.SchemaJson);

            Assert.Equal(data.ExpectedDiagnostics.Length, diagnostics.Count);

            foreach ((Diagnostic expected, Diagnostic actual) in data.ExpectedDiagnostics.Zip(diagnostics))
            {
                Assert.Equal(expected.Severity, actual.Severity);
                Assert.Equal(expected.Range, actual.Range);
            }
        }

        public static IEnumerable<object[]> GetSchemaValidationData()
        {
            var testContainer = TestDataContainer.Load(@"Content\schema-validation.json");

            foreach (TestData testData in testContainer.Tests)
            {
                yield return new object[] { testData };
            }
        }

        public class TestData
        {
            public string Name { get; set; }
            public Diagnostic[] ExpectedDiagnostics { get; set; }
            public string SchemaJson { get; set; }
        }

        public class TestDataContainer
        {
            public TestData[] Tests { get; set; }

            public static TestDataContainer Load(string path)
            {
                var jsonSerializerOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true,
                };
                jsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

                string json = File.ReadAllText(path);
                TestDataContainer container = JsonSerializer.Deserialize<TestDataContainer>(json, jsonSerializerOptions);

                foreach (TestData test in container.Tests)
                {
                    string schemaPath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileNameWithoutExtension(path) + "-test-" + test.Name + ".json");
                    test.SchemaJson = File.ReadAllText(schemaPath);
                }
                return container;
            }
        }
    }

}
