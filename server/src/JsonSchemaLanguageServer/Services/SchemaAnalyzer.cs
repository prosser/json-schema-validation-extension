// <copyright file="SchemaAnalyzer.cs">Copyright (c) Peter Rosser.</copyright>

namespace Rosser.Extensions.JsonSchemaLanguageServer.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Json.Schema;

using Microsoft.VisualStudio.LanguageServer.Protocol;

public class SchemaAnalyzer : IDisposable
{
    private readonly SchemaProvider schemaProvider;
    private bool isDisposed;

    public SchemaAnalyzer(SchemaProvider schemaProvider)
    {
        this.schemaProvider = schemaProvider;
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public async Task InitializeAsync(CancellationToken ct = default) => await this.schemaProvider.InitializeAsync(ct);

    public List<Diagnostic> Analyze(string text)
    {
        List<Diagnostic> diagnostics = [];

        JsonDocument? doc;

        try
        {
            doc = JsonDocument.Parse(text);
            JsonElement root = doc.RootElement;
            if (root.TryGetPropertyValue("$schema", out string? schemaUrl))
            {
                if (!this.schemaProvider.TryGetSchema(schemaUrl, out JsonSchema? schema))
                {
                    diagnostics.Add(new Diagnostic
                    {
                        Code = DiagnosticCodes.CouldNotResolveSchema,
                    });
                }
                else
                {
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

                                TextLineMetrics startMetrics = metrics.FindLineAtByteOffset((int)start);
                                TextLineMetrics endMetrics = metrics.FindLineAtByteOffset((int)end);

                                int startCharOffset = startMetrics.GetCharOffset((int)start);
                                int endCharOffset = endMetrics.GetCharOffset((int)end);

                                var d = new Diagnostic
                                {
                                    Code = DiagnosticCodes.ValidationError,
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
                            catch (InvalidOperationException ex)
                            {
                                diagnostics.Add(new Diagnostic
                                {
                                    Code = DiagnosticCodes.ValidationResultProcessingError,
                                    Severity = DiagnosticSeverity.Error,
                                    Message = ex.Message + ex.StackTrace,
                                });
                            }
                        }
                    }
                }
            }
        }
        catch (JsonException)
        {
            // invalid json -- cannot validate
        }
        catch (Exception ex)
        {
            diagnostics.Add(new Diagnostic
            {
                Code = DiagnosticCodes.UnknownError,
                Severity = DiagnosticSeverity.Error,
                Message = "Please report this! Error details: " + ex.Message + ex.StackTrace,
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
                this.schemaProvider.Dispose();
            }

            this.isDisposed = true;
        }
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
