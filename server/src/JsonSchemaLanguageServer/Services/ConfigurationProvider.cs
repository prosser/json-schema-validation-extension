// <copyright file="ConfigurationProvider.cs">Copyright (c) Peter Rosser.</copyright>

namespace Rosser.Extensions.JsonSchemaLanguageServer.Services;

using System;

using Rosser.Extensions.JsonSchemaLanguageServer;

public class ConfigurationProvider
{
    public Configuration Configuration { get; private set; } = new();

    public void UpdateConfiguration(Configuration value)
    {
        if (this.Configuration != value)
        {
            Configuration old = this.Configuration;
            this.Configuration = value;
            this.ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs(old, value));
        }
    }

    public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;
}

public class ConfigurationChangedEventArgs(Configuration oldConfiguration, Configuration newConfiguration) : EventArgs
{
    public Configuration OldConfiguration { get; } = oldConfiguration;
    public Configuration NewConfiguration { get; } = newConfiguration;
}
