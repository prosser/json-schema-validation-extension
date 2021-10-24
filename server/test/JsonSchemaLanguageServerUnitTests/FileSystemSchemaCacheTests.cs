// <copyright file="FileSystemSchemaCacheTests.cs">Copyright (c) Peter Rosser.</copyright>

namespace JsonSchemaLanguageServerUnitTests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    using Microsoft.Extensions.DependencyInjection;

    using Rosser.Extensions.JsonSchemaLanguageServer.Services;

    using Xunit;
    using Xunit.Abstractions;

    public class FileSystemSchemaCacheTests : TestBase
    {
        public FileSystemSchemaCacheTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact]
        public void ConfigurationChangesClearCache()
        {
            using IServiceScope scope = this.serviceProvider.CreateScope();
            FileSystemCache cache = scope.ServiceProvider.GetRequiredService<FileSystemCache>();

            ConfigurationProvider configurationProvider = scope.ServiceProvider.GetRequiredService<ConfigurationProvider>();

            cache.Add("urn://test", "/foo", Encoding.UTF8.GetBytes("test content"));

            Assert.Equal(1, cache.Count);

            configurationProvider.UpdateConfiguration(
                configurationProvider.Configuration.WithSchemaSearchPaths(@"Content\Schemas")
                .WithMaxNumberOfProblems(1)
                .WithSchemaSearchGlobs(@"**\$1.json")
                .WithSchemaSearchUrlPattern(@""));

            Assert.Equal(0, cache.Count);
        }

        [Fact]
        public void CanAddAndRetrieve()
        {
            using IServiceScope scope = this.serviceProvider.CreateScope();
            FileSystemCache cache = scope.ServiceProvider.GetRequiredService<FileSystemCache>();

            string path = @"Content\LoremIpsum.txt";
            byte[] content = File.ReadAllBytes(path);
            Assert.Equal(0, cache.Count);
            cache.Add("urn://test", path, content);

            Assert.Equal(1, cache.Count);

            Assert.True(cache.TryGetValue("urn://test", out byte[] cached));
            Assert.NotEmpty(cached);
            Assert.Equal(content, cached);
        }

        [Fact]
        public void GetCachedUrlsReturnsAllUrls()
        {
            using IServiceScope scope = this.serviceProvider.CreateScope();
            FileSystemCache cache = scope.ServiceProvider.GetRequiredService<FileSystemCache>();
            cache.Add("urn://test", "/foo", Encoding.UTF8.GetBytes("test content"));
            cache.Add("urn://test2", "/foo2", Encoding.UTF8.GetBytes("test content2"));

            var urls = cache.GetCachedUrls().ToArray();
            Assert.Equal(new[] { "urn://test", "urn://test2" }, urls);
        }

        [Fact]
        public void ClearWorks()
        {
            using IServiceScope scope = this.serviceProvider.CreateScope();
            FileSystemCache cache = scope.ServiceProvider.GetRequiredService<FileSystemCache>();
            cache.Add("urn://test", "/foo", Encoding.UTF8.GetBytes("test content"));

            Assert.Equal(1, cache.Count);
            cache.Clear();
            Assert.Equal(0, cache.Count);
        }

        [Fact]
        public void TouchingFileRemovesCacheEntry()
        {
            using IServiceScope scope = this.serviceProvider.CreateScope();
            FileSystemCache cache = scope.ServiceProvider.GetRequiredService<FileSystemCache>();

            string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("n"));
            File.WriteAllText(path, nameof(TouchingFileRemovesCacheEntry));
            try
            {
                Assert.Equal(0, cache.Count);
                cache.Add("urn://test", path, File.ReadAllBytes(path));
                Assert.Equal(1, cache.Count);

                File.AppendAllText(path, "-updated");

                Assert.False(cache.TryGetValue("urn://test", out byte[] cached));
                Assert.Equal(0, cache.Count);
            }
            finally
            {
                File.Delete(path);
            }
        }
    }
}
