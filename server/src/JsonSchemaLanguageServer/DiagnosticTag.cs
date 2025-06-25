// <copyright file="DiagnosticTag.cs">Copyright (c) Peter Rosser.</copyright>

namespace Rosser.Extensions.JsonSchemaLanguageServer;

using Microsoft.VisualStudio.LanguageServer.Protocol;

public class DiagnosticTag
{
    public string Text { get; set; } = string.Empty;

    public DiagnosticSeverity Severity { get; set; }
}
