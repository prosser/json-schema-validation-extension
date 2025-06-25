// <copyright file="DiagnosticCodes.cs">Copyright (c) Peter Rosser.</copyright>

namespace Rosser.Extensions.JsonSchemaLanguageServer;

using Code = Microsoft.VisualStudio.LanguageServer.Protocol.SumType<int, string?>;
public static class DiagnosticCodes
{
    private const string CodePrefix = "JSLS-";
    public static Code UnknownError { get; } = new(CodePrefix + 100);
    public static Code CouldNotResolveSchema { get; } = new(CodePrefix + 101);
    public static Code ValidationResultProcessingError { get; } = new(CodePrefix + 102);

    public static Code ValidationError { get; } = new(CodePrefix + 1000);
}
