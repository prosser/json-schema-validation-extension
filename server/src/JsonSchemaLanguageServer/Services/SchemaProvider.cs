// <copyright file="SchemaProvider.cs">Copyright (c) Peter Rosser.</copyright>

namespace Rosser.Extensions.JsonSchemaLanguageServer.Services;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Json.Schema;

using Microsoft.VisualStudio.Threading;

public class SchemaProvider(HttpMessageHandler messageHandler) : IDisposable
{
    private readonly JoinableTaskContext syncTaskContext = new();
    private readonly HttpClient? httpClient = new(messageHandler);
    private bool isDisposed;
    internal const string Schema2020Url = @"https://json-schema.org/draft/2020-12/schema";

    private HttpClient HttpClient => this.httpClient ?? throw new InvalidOperationException("Not initialized");
    public Dictionary<string, JsonSchema> SchemaCache { get; } = new(StringComparer.OrdinalIgnoreCase);

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

        this.SchemaCache.Add(Schema2020Url, schema2020);
    }

    public bool TryGetSchema(string url, [NotNullWhen(true)] out JsonSchema? schema)
    {
        if (this.SchemaCache.TryGetValue(url, out schema))
        {
            return true;
        }

        JsonSchema? downloaded = null;
        using var downloadDone = new AutoResetEvent(false);
        _ = ThreadPool.QueueUserWorkItem((_) =>
        {
            try
            {
                JoinableTaskFactory taskFactory = this.syncTaskContext.CreateFactory(this.syncTaskContext.CreateCollection());

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                downloaded = taskFactory.Run(async () => await this.DownloadSchemaAsync(url, cts.Token));
            }
            finally
            {
                _ = downloadDone.Set();
            }
        });

        _ = downloadDone.WaitOne();
        schema = downloaded;

        if (schema is not null)
        {
            this.SchemaCache.Add(url, schema);
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
