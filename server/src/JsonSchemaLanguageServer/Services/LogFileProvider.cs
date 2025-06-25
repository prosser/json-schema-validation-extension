// <copyright file="LogFileProvider.cs">Copyright (c) Peter Rosser.</copyright>

namespace Rosser.Extensions.JsonSchemaLanguageServer.Services;

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;

using Microsoft.Extensions.Logging;

using Rosser.Extensions.JsonSchemaLanguageServer.Logging;

public class LogFileProvider : ILoggerProvider
{
    private bool isDisposed;
    private readonly Stream stream;
    private readonly StreamWriter writer;
    private readonly ConcurrentQueue<(LogLevel logLevel, EventId eventId, string message, Exception? exception)> messageQueue = new();
    private readonly Thread thread;
    private readonly AutoResetEvent messageAvailable = new(false);
    private bool shutdownRequested = false;
    private readonly AutoResetEvent shutdownComplete = new(false);

    public LogFileProvider(LogFileProviderOptions options)
    {
        if (string.IsNullOrEmpty(options.LogDirectory))
        {
            options.LogDirectory = ".";
        }

        options.LogDirectory = Path.GetFullPath(options.LogDirectory);
        string logFile = Path.Combine(options.LogDirectory, $"{options.BaseName}.{DateTime.Now:yyyyMMddThhmmss}.log");

        if (!Directory.Exists(options.LogDirectory))
        {
            _ = Directory.CreateDirectory(options.LogDirectory);
        }

        this.stream = File.OpenWrite(logFile);
        this.writer = new StreamWriter(this.stream, Encoding.UTF8)
        {
            AutoFlush = true
        };
        this.thread = new Thread(this.ThreadProc);
        this.thread.Start();
    }

    public ILogger CreateLogger(string categoryName) => new LogFileLogger(this);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception, string> formatter)
    {
        string message = state?.ToString() ?? string.Empty;
        if (exception is not null)
        {
            message += formatter(state, exception);
        }

        this.messageQueue.Enqueue((logLevel, eventId, message, exception));
        _ = this.messageAvailable.Set();
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!this.isDisposed)
        {
            this.shutdownRequested = true;
            _ = this.messageAvailable.Set();
            _ = this.shutdownComplete.WaitOne(TimeSpan.FromSeconds(2.0));

            if (disposing)
            {
                this.writer.Flush();
                this.writer.Dispose();
                this.stream.Dispose();
            }

            this.isDisposed = true;
        }
    }

    private void ThreadProc()
    {
        while (!this.shutdownRequested)
        {
            _ = this.messageAvailable.WaitOne();

            while (this.messageQueue.TryDequeue(out (LogLevel logLevel, EventId eventId, string message, Exception? exception) item))
            {
                this.writer.WriteLine($"{DateTime.Now:O} {item.eventId.Id} {item.eventId.Name} {item.message}");
            }
        }

        _ = this.shutdownComplete.Set();
    }
}

public class NopDisposable : IDisposable
{
    public void Dispose() => GC.SuppressFinalize(this);
}
