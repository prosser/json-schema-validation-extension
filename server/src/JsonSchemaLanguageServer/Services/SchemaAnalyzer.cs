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
using Rosser.Extensions.JsonSchemaLanguageServer.Extensions;
using Rosser.Extensions.JsonSchemaLanguageServer.Text;

public class SchemaAnalyzer(SchemaProvider schemaProvider) : IDisposable
{
    private bool isDisposed;

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public async Task InitializeAsync(CancellationToken ct = default) => await schemaProvider.InitializeAsync(ct);

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
                if (!schemaProvider.TryGetSchema(schemaUrl, out JsonSchema? schema))
                {
                    diagnostics.Add(new Diagnostic
                    {
                        Code = (int)DiagnosticCodes.CouldNotResolveSchema,
                    });
                }
                else
                {
                    EvaluationResults evaluation = schema.Evaluate(root, new() { OutputFormat = OutputFormat.Hierarchical });
                    if (!evaluation.IsValid)
                    {
                        TextLineMetrics[] metrics = TextHelper.CreateTextLineMetrics(text);

                        foreach (EvaluationResults leaf in GetLeafNodeResults(evaluation))
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
                                    Code = (int)DiagnosticCodes.ValidationError,
                                    Severity = DiagnosticSeverity.Error,
                                    Message = string.Join(",", leaf.Errors?.Select(x => $"{x.Key}: {x.Value}") ?? ["Schema validation failure"]),
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
                                    Code = (int)DiagnosticCodes.ValidationResultProcessingError,
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
                Code = (int)DiagnosticCodes.UnknownError,
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
                schemaProvider.Dispose();
            }

            this.isDisposed = true;
        }
    }

    private static IEnumerable<EvaluationResults> GetLeafNodeResults(EvaluationResults root)
    {
        if (root.Details.Count == 0)
        {
            yield return root;
            yield break;
        }

        foreach (EvaluationResults nestedResult in root.Details.SelectMany(x => GetLeafNodeResults(x)))
        {
            yield return nestedResult;
        }
    }
}
