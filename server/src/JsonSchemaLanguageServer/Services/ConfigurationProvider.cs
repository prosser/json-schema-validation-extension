// <copyright file="ConfigurationProvider.cs">Copyright (c) Peter Rosser.</copyright>

namespace Rosser.Extensions.JsonSchemaLanguageServer.Services
{
    using System;

    using Rosser.Extensions.JsonSchemaLanguageServer;

    public class ConfigurationProvider
    {
        private Configuration configuration = new();
        public Configuration Configuration => this.configuration;

        public void UpdateConfiguration(Configuration value)
        {
            if (this.configuration != value)
            {
                Configuration old = this.configuration;
                this.configuration = value;
                this.ConfigurationChanged?.Invoke(this, new ConfigurationChangedEventArgs(old, value));
            }
        }

        public event EventHandler<ConfigurationChangedEventArgs>? ConfigurationChanged;
    }

    public class ConfigurationChangedEventArgs : EventArgs
    {
        public ConfigurationChangedEventArgs(Configuration oldConfiguration, Configuration newConfiguration)
        {
            this.OldConfiguration = oldConfiguration;
            this.NewConfiguration = newConfiguration;
        }

        public Configuration OldConfiguration { get; }
        public Configuration NewConfiguration { get; }
    }
}
