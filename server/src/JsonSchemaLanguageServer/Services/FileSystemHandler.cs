// <copyright file="FileSystemHandler.cs">Copyright (c) Peter Rosser.</copyright>

namespace Rosser.Extensions.JsonSchemaLanguageServer.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.FileSystemGlobbing;
    using Microsoft.Extensions.FileSystemGlobbing.Abstractions;

    public class FileSystemHandler : DelegatingHandler
    {
        private readonly ConfigurationProvider configurationProvider;
        private readonly FileSystemCache cache;
        private readonly HttpMessageHandler innerHandler;

        public FileSystemHandler(ConfigurationProvider configurationProvider, FileSystemCache cache)
        {
            this.innerHandler = new HttpClientHandler();
            this.InnerHandler = this.innerHandler;
            this.configurationProvider = configurationProvider;
            this.cache = cache;
            this.configurationProvider.ConfigurationChanged += this.OnConfigurationChanged;
        }

        private Configuration Configuration => this.configurationProvider.Configuration;

        private void OnConfigurationChanged(object? sender, ConfigurationChangedEventArgs e)
        {
            if (!e.OldConfiguration.Equals(e.NewConfiguration))
            {
                this.cache.Clear();
            }
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            string url = request.RequestUri?.ToString() ?? "";

            if (this.cache.TryGetValue(url, out byte[]? content))
            {
                return CreateResponseAsync(content);
            }

            if (this.TryFindLocalSchema(url, out string? path))
            {
                try
                {
                    content = File.ReadAllBytes(path);
                    this.cache.Add(url, path, content);
                    return CreateResponseAsync(content);
                }
                catch (ArgumentException) { }
                catch (NotSupportedException) { }
                catch (UnauthorizedAccessException) { }
                catch (System.Security.SecurityException) { }
                catch (IOException) { }
            }
            return base.SendAsync(request, ct);
        }

        private static Task<HttpResponseMessage> CreateResponseAsync(byte[] buffer)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(Encoding.UTF8.GetString(buffer), Encoding.UTF8, "application/json"),
            };
            
            return Task.FromResult(response);
        }

        private bool TryFindLocalSchema(string url, [NotNullWhen(true)] out string? path)
        {
            if (!this.Configuration.TryGetSearchOptions(
                out IReadOnlyList<string>? paths,
                out IReadOnlyList<string>? globs,
                out Regex? regex))
            {
                path = null;
                return false;
            }

            MatchCollection matches = regex.Matches(url);
            if (matches.Count > 0)
            {
                var matcher = new Matcher(StringComparison.OrdinalIgnoreCase);
                matcher.AddIncludePatterns(globs.Select(x => regex.Replace(url, x)));
                foreach (string searchPath in paths)
                {
                    PatternMatchingResult results = matcher.Execute(new DirectoryInfoWrapper(new DirectoryInfo(searchPath)));
                    if (results.HasMatches)
                    {
                        path = Path.Combine(searchPath, results.Files.First().Path);
                        return true;
                    }
                }
            }

            path = null;
            return false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.innerHandler.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
