// <copyright file="SchemaProviderTests.cs">Copyright (c) Peter Rosser.</copyright>

namespace JsonSchemaLanguageServerUnitTests;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;

using Rosser.Extensions.JsonSchemaLanguageServer.Services;

using Xunit;
using Xunit.Abstractions;

public class SchemaProviderTests(ITestOutputHelper output) : TestBase(output)
{
    [Fact]
    public async Task InitializingPreloadsMetaSchemasAsync()
    {
        SchemaProvider uut = this.serviceProvider.GetRequiredService<SchemaProvider>();

        Assert.Empty(uut.SchemaCache);

        await uut.InitializeAsync();

        _ = Assert.Single(uut.SchemaCache);

        Assert.Equal(SchemaProvider.Schema2020Url, uut.SchemaCache.Single().Key);
    }
}
