// <copyright file="LogFileProviderOptions.cs">Copyright (c) Peter Rosser.</copyright>

namespace Rosser.Extensions.JsonSchemaLanguageServer.Logging;

public class LogFileProviderOptions
{
    public string LogDirectory { get; set; } = string.Empty;
    public string BaseName { get; set; } = "log";
}
