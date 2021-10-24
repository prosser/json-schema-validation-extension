// <copyright file="LanguageServer.cs">Copyright (c) Peter Rosser.</copyright>

namespace Rosser.Extensions.JsonSchemaLanguageServer.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.LanguageServer.Protocol;
    using Microsoft.VisualStudio.Threading;

    using Rosser.Extensions.JsonSchemaLanguageServer;

    using StreamJsonRpc;

    public class LanguageServer
    {
        private readonly JoinableTaskContext syncTaskContext;
        private JsonRpc? rpc;
        private readonly ServerTarget target;
        private readonly ManualResetEvent disconnectEvent = new(false);
        private readonly SchemaAnalyzer schemaAnalyzer;
        private readonly ConfigurationProvider configurationProvider;
        private TextDocumentItem? textDocument = null;

        public LanguageServer(ILogger<LanguageServer> logger, SchemaAnalyzer schemaAnalyzer, ConfigurationProvider configurationProvider)
        {
            this.syncTaskContext = new JoinableTaskContext();
            this.target = new ServerTarget(this);
            this.schemaAnalyzer = schemaAnalyzer;
            this.configurationProvider = configurationProvider;
            this.configurationProvider.ConfigurationChanged += this.OnConfigurationChanged;
        }

        public string CustomText { get; set; } = string.Empty;

        public Configuration Configuration => this.configurationProvider.Configuration;

        public event EventHandler? Disconnected;

        public async Task InitializeAsync(Stream sender, Stream reader, CancellationToken ct = default)
        {
            this.rpc = JsonRpc.Attach(sender, reader, this.target);
            this.rpc.Disconnected += this.OnRpcDisconnected;

            this.target.Initialized += this.OnInitialized;

            await this.schemaAnalyzer.InitializeAsync(ct);

            this.OnInitialized(this, EventArgs.Empty);
        }

        private JsonRpc Rpc => this.rpc ?? throw new InvalidOperationException("Not initialized");

        private void OnInitialized(object? sender, EventArgs e)
        {
        }

        public Task OnTextDocumentOpenedAsync(DidOpenTextDocumentParams messageParams)
        {
            this.textDocument = messageParams.TextDocument;

            return this.SendDiagnosticsAsync();
        }

        public async Task SendDiagnosticsAsync()
        {
            if (this.textDocument is null)
            {
                return;
            }

            List<Diagnostic> diagnostics = this.schemaAnalyzer.Analyze(this.textDocument.Text);

            PublishDiagnosticParams parameter = new()
            {
                Uri = this.textDocument.Uri,
                Diagnostics = diagnostics.ToArray()
            };

            if (this.Configuration.MaxNumberOfProblems > -1)
            {
                parameter.Diagnostics = parameter.Diagnostics.Take(this.Configuration.MaxNumberOfProblems).ToArray();
            }

            await this.Rpc.NotifyWithParameterObjectAsync(Methods.TextDocumentPublishDiagnosticsName, parameter);
        }

        public Task SendDiagnosticsAsync(string uri, string text)
        {
            //string[] lines = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            List<Diagnostic> diagnostics = this.schemaAnalyzer.Analyze(text);

            PublishDiagnosticParams parameter = new()
            {
                Uri = new Uri(uri),
                Diagnostics = diagnostics.ToArray()
            };

            if (this.Configuration.MaxNumberOfProblems > -1)
            {
                parameter.Diagnostics = parameter.Diagnostics.Take(this.Configuration.MaxNumberOfProblems).ToArray();
            }

            return this.Rpc.NotifyWithParameterObjectAsync(Methods.TextDocumentPublishDiagnosticsName, parameter);
        }

        public void SendSettings(DidChangeConfigurationParams parameter)
        {
            try
            {
                string configurationJson = parameter.Settings.ToString() ?? "{}";
                var serializerOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                };
                Configuration? configuration = JsonSerializer.Deserialize<Configuration>(configurationJson, serializerOptions);
                this.configurationProvider.UpdateConfiguration(configuration ?? new());
            }
            catch { }
        }

        public void WaitForExit() => this.disconnectEvent.WaitOne();

        public void Exit()
        {
            this.disconnectEvent.Set();

            Disconnected?.Invoke(this, new EventArgs());
        }

        private void OnRpcDisconnected(object? sender, JsonRpcDisconnectedEventArgs e) => this.Exit();

        private void OnConfigurationChanged(object? sender, ConfigurationChangedEventArgs e)
        {
            using var done = new AutoResetEvent(false);
            ThreadPool.QueueUserWorkItem((_) =>
            {
                try
                {
                    var taskFactory = this.syncTaskContext.CreateFactory(this.syncTaskContext.CreateCollection());

                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    taskFactory.Run(async () => await this.SendDiagnosticsAsync());
                }
                finally
                {
                    done.Set();
                }
            });

            done.WaitOne();
        }
    }
}
