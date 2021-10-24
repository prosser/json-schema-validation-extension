// <copyright file="SchemaProvider.cs">Copyright (c) Peter Rosser.</copyright>

namespace Rosser.Extensions.JsonSchemaLanguageServer.Services
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Json.Schema;

    using Microsoft.VisualStudio.Threading;

    public class SchemaProvider : IDisposable
    {
        private readonly JoinableTaskContext syncTaskContext;
        private readonly HttpClient? httpClient;
        private bool isDisposed;
        private Dictionary<string, JsonSchema> schemaCache = new(StringComparer.OrdinalIgnoreCase);

        internal const string Schema2020Url = @"https://json-schema.org/draft/2020-12/schema";

        public SchemaProvider(HttpMessageHandler messageHandler)
        {
            this.syncTaskContext = new JoinableTaskContext();
            this.httpClient = new HttpClient(messageHandler);
        }

        private HttpClient HttpClient => this.httpClient ?? throw new InvalidOperationException("Not initialized");
        public Dictionary<string, JsonSchema> SchemaCache => this.schemaCache;

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async Task InitializeAsync(CancellationToken ct = default)
        {
            JsonSchema? schema2020 = await this.DownloadSchemaAsync(Schema2020Url, ct)
                ?? throw new InvalidOperationException($"Could not retrieve {Schema2020Url}");

            this.schemaCache.Add(Schema2020Url, schema2020);
        }

        public bool TryGetSchema(string url, [NotNullWhen(true)] out JsonSchema? schema)
        {
            if (this.schemaCache.TryGetValue(url, out schema))
            {
                return true;
            }

            JsonSchema? downloaded = null;
            using var downloadDone = new AutoResetEvent(false);
            ThreadPool.QueueUserWorkItem((_) =>
            {
                try
                {
                    var taskFactory = this.syncTaskContext.CreateFactory(this.syncTaskContext.CreateCollection());

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    downloaded = taskFactory.Run(async () => await this.DownloadSchemaAsync(url, cts.Token));
                }
                finally
                {
                    downloadDone.Set();
                }
            });

            downloadDone.WaitOne();
            schema = downloaded;

            if (schema is not null)
            {
                this.schemaCache.Add(url, schema);
                return true;
            }
            return false;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!this.isDisposed)
            {
                if (disposing)
                {
                    this.httpClient?.Dispose();
                }

                this.isDisposed = true;
            }
        }

        private async Task<JsonSchema?> DownloadSchemaAsync(string url, CancellationToken ct)
        {
            try
            {
                HttpResponseMessage response = await this.HttpClient.GetAsync(url, ct);

                JsonSchema? downloaded = null;
                if (response.IsSuccessStatusCode)
                {
                    downloaded = await JsonSchema.FromStream(await response.Content.ReadAsStreamAsync(ct));
                }
                return downloaded;
            }
            catch (Exception)
            {
                return null;
            }
        }

    }
}
