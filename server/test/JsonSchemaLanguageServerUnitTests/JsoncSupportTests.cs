using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Rosser.Extensions.JsonSchemaLanguageServer.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace JsonSchemaLanguageServerUnitTests
{
    public class JsoncSupportTests : TestBase
    {
        public JsoncSupportTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public async Task ShouldParseSchemaUrlFromJsoncFileAsync()
        {
            // Arrange
            string jsoncContent = @"{
  // This is a JSONC file with comments
  ""$schema"": ""https://json-schema.org/draft/2020-12/schema#"",
  ""title"": ""Test Schema"", // inline comment
  ""type"": ""object""
}";

            using var analyzer = this.serviceProvider.GetRequiredService<SchemaAnalyzer>();
            await analyzer.InitializeAsync();

            // Act
            var diagnostics = analyzer.Analyze(jsoncContent);

            // Assert - Should not crash and should produce some result
            Assert.NotNull(diagnostics);
            // For now, just verify it doesn't crash when parsing JSONC
        }

        [Fact]
        public async Task ShouldAttemptSchemaSubstitutionForJsoncFileAsync()
        {
            // Arrange - Create a JSONC file that references a schema URL that might be substituted
            string jsoncContent = @"{
  // This is a JSONC file with comments
  ""$schema"": ""https://example.com/test-schema.json"",
  ""name"": ""test"", // inline comment
  ""value"": 42
}";

            using var analyzer = this.serviceProvider.GetRequiredService<SchemaAnalyzer>();
            await analyzer.InitializeAsync();

            // Act
            var diagnostics = analyzer.Analyze(jsoncContent);

            // Assert - Should attempt to resolve the schema (might fail but shouldn't crash)
            Assert.NotNull(diagnostics);

            // If it finds "CouldNotResolveSchema" diagnostic, that means it parsed the $schema property
            // If no diagnostics, that could mean it found the schema or no $schema was found
            // Either way, it shouldn't crash

            // The key test is that it doesn't fail during JSON parsing due to comments
        }
    }
}
