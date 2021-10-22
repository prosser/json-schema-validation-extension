// <copyright file="SchemaAnalyzer.cs">Copyright (c) Peter Rosser.</copyright>

namespace Rosser.Extensions.JsonSchemaLanguageServer
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    using Json.Schema;

    using Microsoft.VisualStudio.LanguageServer.Protocol;
    using Microsoft.VisualStudio.Threading;

    public class SchemaAnalyzer : IDisposable
    {
        private const string Schema2020Url = @"https://json-schema.org/draft/2020-12/schema";
        private HttpClient? httpClient;
        private readonly JoinableTaskContext syncTaskContext;
        private bool isDisposed;

        public SchemaAnalyzer()
        {
            this.httpClient = new HttpClient();
            this.syncTaskContext = new JoinableTaskContext();
        }

        private HttpClient HttpClient => this.httpClient ?? throw new InvalidOperationException("Not initialized");

        private Dictionary<string, JsonSchema> schemaCache = new(StringComparer.OrdinalIgnoreCase);

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

        public List<Diagnostic> Analyze(string text)
        {
            List<Diagnostic> diagnostics = new();

            try
            {
                using var doc = JsonDocument.Parse(text);
                JsonElement root = doc.RootElement;
                if (root.TryGetPropertyValue("$schema", out string? schemaUrl))
                {
                    if (schemaUrl is null ||
                        !this.TryGetSchema(schemaUrl, out JsonSchema? schema))
                    {
                        return diagnostics;
                    }

                    ValidationResults validationResults = schema.Validate(root, new ValidationOptions { OutputFormat = OutputFormat.Detailed });
                    if (!validationResults.IsValid)
                    {
                        TextLineMetrics[] metrics = TextHelper.CreateTextLineMetrics(text);

                        foreach (ValidationResults leaf in GetLeafNodeResults(validationResults))
                        {
                            long start = 0;
                            long end = 0;
                            try
                            {
                                (start, end) = JsonHelper.GetElementBounds(text, leaf.InstanceLocation);
                            }
                            catch (InvalidOperationException ex)
                            {
                                diagnostics.Add(new Diagnostic
                                {
                                    Code = new SumType<int, string?>("JSLS-1002"),
                                    Severity = DiagnosticSeverity.Error,
                                    Message = ex.Message + ex.StackTrace,
                                });
                            }
                            TextLineMetrics startMetrics = metrics.FindLineAtByteOffset((int)start);
                            TextLineMetrics endMetrics = metrics.FindLineAtByteOffset((int)end);

                            int startCharOffset = startMetrics.GetCharOffset((int)start);
                            int endCharOffset = endMetrics.GetCharOffset((int)end);

                            var d = new Diagnostic
                            {
                                Code = new SumType<int, string?>("JSLS-1000"),
                                Severity = DiagnosticSeverity.Error,
                                Message = leaf.Message ?? "Schema validation failure",
                                Range = new()
                                {
                                    Start = new Position(startMetrics.LineIndex, startCharOffset),
                                    End = new Position(endMetrics.LineIndex, endCharOffset),
                                },
                                Source = "JsonSchemaLanguageServer",
                            };

                            diagnostics.Add(d);
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                diagnostics.Add(new Diagnostic
                {
                    Code = new SumType<int, string?>("JSLS-1001"),
                    Severity = DiagnosticSeverity.Error,
                    Message = ex.Message + ex.StackTrace,
                });
            }

            return diagnostics;
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
            HttpResponseMessage response = await this.HttpClient.GetAsync(url, ct);

            JsonSchema? downloaded = null;
            if (response.IsSuccessStatusCode)
            {
                downloaded = await JsonSchema.FromStream(await response.Content.ReadAsStreamAsync(ct));
            }
            return downloaded;
        }

        private bool TryGetSchema(string url, [NotNullWhen(true)] out JsonSchema? schema)
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

        private static IEnumerable<ValidationResults> GetLeafNodeResults(ValidationResults root)
        {
            if (root.NestedResults.Count == 0)
            {
                yield return root;
                yield break;
            }

            foreach (ValidationResults nestedResult in root.NestedResults.SelectMany(x => GetLeafNodeResults(x)))
            {
                yield return nestedResult;
            }
        }
    }
}
