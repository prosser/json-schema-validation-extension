// <copyright file="LanguageServerHost.cs">Copyright (c) Peter Rosser.</copyright>

namespace Rosser.Extensions.JsonSchemaLanguageServer
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.LanguageServer.Protocol;

    internal class LanguageServerHost : IDisposable
    {
        private readonly ILogger<LanguageServerHost> logger;
        private readonly IServiceProvider serviceProvider;
        private Server? languageServer;
        private bool isDisposed;

        private readonly Stream cin;
        private readonly Stream cout;
        private readonly BufferedStream bcin;

        public LanguageServerHost(ILogger<LanguageServerHost> logger, IServiceProvider serviceProvider)
        {
            this.logger = logger;
            this.serviceProvider = serviceProvider;
            this.Disconnected = new(false);

            this.cin = Console.OpenStandardInput();
            this.cout = Console.OpenStandardOutput();
            this.bcin = new BufferedStream(this.cin);
        }

        public ManualResetEventSlim Disconnected { get; private set; }

        public MessageType MessageType { get; set; }

        private Server Server => this.languageServer ?? throw new InvalidOperationException("Not initialized");

        public async Task InitializeAsync(CancellationToken ct)
        {
            this.logger.LogInformation("Initializing JSON Schema Language Server");
            this.languageServer = this.serviceProvider.GetRequiredService<Server>();

            this.Server.Disconnected += this.OnDisconnected;

            await this.Server.InitializeAsync(this.cout, this.bcin, ct: ct);

            this.logger.LogInformation("JSON Schema Language Server initialized");
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
                if (disposing)
                {
                    this.OnDisconnected(null, EventArgs.Empty);
                    this.Disconnected.Dispose();

                    this.bcin.Dispose();
                    this.cin.Dispose();
                    this.cout.Dispose();
                }

                this.isDisposed = true;
            }
        }

        private void OnDisconnected(object? sender, EventArgs e) => this.Disconnected.Set();
    }
}
