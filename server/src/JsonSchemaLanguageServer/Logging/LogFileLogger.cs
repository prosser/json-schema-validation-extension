// <copyright file="LogFileLogger.cs">Copyright (c) Peter Rosser.</copyright>

namespace Rosser.Extensions.JsonSchemaLanguageServer.Logging;

using System;

using Microsoft.Extensions.Logging;

using Rosser.Extensions.JsonSchemaLanguageServer.Services;

internal class LogFileLogger(LogFileProvider provider) : ILogger
{
    public IDisposable? BeginScope<TState>(TState _)
        where TState : notnull => new NopDisposable();

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception, string> formatter)
        => provider.Log(logLevel, eventId, state, exception, FormatLogMessage);

    private static string FormatLogMessage<TState>(TState state, Exception ex) => ex.Message + ex.StackTrace;
}
