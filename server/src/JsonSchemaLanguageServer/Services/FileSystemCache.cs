// <copyright file="FileSystemCache.cs">Copyright (c) Peter Rosser.</copyright>

namespace Rosser.Extensions.JsonSchemaLanguageServer.Services;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

public class FileSystemCache : IDisposable
{
    private readonly Dictionary<string, (string path, DateTime lastWriteTime, byte[] content)> cache = new(StringComparer.OrdinalIgnoreCase);
    private bool isDisposed;
    private readonly ConfigurationProvider configurationProvider;

    public FileSystemCache(ConfigurationProvider configurationProvider)
    {
        this.configurationProvider = configurationProvider;
        configurationProvider.ConfigurationChanged += this.OnConfigurationChanged;
    }

    public int Count => this.cache.Count;

    public void Clear() => this.cache.Clear();

    public IEnumerable<string> GetCachedUrls() => this.cache.Keys;

    public void Add(string url, string path, byte[] content)
    {
        var fileInfo = new FileInfo(path);
        this.cache.Add(url, (fileInfo.FullName, fileInfo.LastWriteTimeUtc, content));
    }

    public void Dispose()
    {
        // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
        this.Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public bool TryGetValue(string url, [NotNullWhen(true)] out byte[]? content)
    {
        if (!this.cache.TryGetValue(url, out (string path, DateTime lastWriteTime, byte[] content) tup))
        {
            content = null;
            return false;
        }

        if (!File.Exists(tup.path) ||
            new FileInfo(tup.path).LastWriteTimeUtc > tup.lastWriteTime)
        {
            _ = this.cache.Remove(url);
            content = null;
            return false;
        }

        content = tup.content;
        return true;
    }

    private void OnConfigurationChanged(object? sender, ConfigurationChangedEventArgs e)
    {
        if (!e.OldConfiguration.Equals(e.NewConfiguration))
        {
            this.cache.Clear();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!this.isDisposed)
        {
            if (disposing)
            {
                this.configurationProvider.ConfigurationChanged -= this.OnConfigurationChanged;
            }

            this.isDisposed = true;
        }
    }
}
