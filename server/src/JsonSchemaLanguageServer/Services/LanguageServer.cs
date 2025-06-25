// <copyright file="LanguageServer.cs">Copyright (c) Peter Rosser.</copyright>

namespace Rosser.Extensions.JsonSchemaLanguageServer.Services;

using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.LanguageServer.Protocol;
using Microsoft.VisualStudio.Threading;

using StreamJsonRpc;

public class LanguageServer
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly ConfigurationProvider configurationProvider;
    private readonly ManualResetEvent disconnectEvent = new(false);
    private readonly SchemaAnalyzer schemaAnalyzer;
    private readonly JoinableTaskContext syncTaskContext;
    private readonly ServerTarget target;
    private JsonRpc? rpc;
    private TextDocumentItem? textDocument = null;

    public LanguageServer(ILogger<LanguageServer> logger, SchemaAnalyzer schemaAnalyzer, ConfigurationProvider configurationProvider)
    {
        this.syncTaskContext = new JoinableTaskContext();
        this.target = new ServerTarget(this);
        this.schemaAnalyzer = schemaAnalyzer;
        this.configurationProvider = configurationProvider;
        this.configurationProvider.ConfigurationChanged += this.OnConfigurationChanged;
    }

    public event EventHandler? Disconnected;

    public Configuration Configuration => this.configurationProvider.Configuration;
    public string CustomText { get; set; } = string.Empty;
    private JsonRpc Rpc => this.rpc ?? throw new InvalidOperationException("Not initialized");

    public void Exit()
    {
        _ = this.disconnectEvent.Set();

        Disconnected?.Invoke(this, new EventArgs());
    }

    public async Task InitializeAsync(Stream sender, Stream reader, CancellationToken ct = default)
    {
        this.rpc = JsonRpc.Attach(sender, reader, this.target);
        this.rpc.Disconnected += this.OnRpcDisconnected;

        this.target.Initialized += this.OnInitialized;

        await this.schemaAnalyzer.InitializeAsync(ct);

        this.OnInitialized(this, EventArgs.Empty);
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
            Diagnostics = [.. diagnostics],
        };

        if (this.Configuration.MaxNumberOfProblems > -1)
        {
            parameter.Diagnostics = [.. parameter.Diagnostics.Take(this.Configuration.MaxNumberOfProblems)];
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
            Diagnostics = [.. diagnostics],
        };

        if (this.Configuration.MaxNumberOfProblems > -1)
        {
            parameter.Diagnostics = [.. parameter.Diagnostics.Take(this.Configuration.MaxNumberOfProblems)];
        }

        return this.Rpc.NotifyWithParameterObjectAsync(Methods.TextDocumentPublishDiagnosticsName, parameter);
    }

    public void SendSettings(DidChangeConfigurationParams parameter)
    {
        try
        {
            string configurationJson = parameter.Settings.ToString() ?? "{}";
            Configuration? configuration = JsonSerializer.Deserialize<Configuration>(configurationJson, SerializerOptions);
            this.configurationProvider.UpdateConfiguration(configuration ?? new());
        }
        catch { }
    }

    public void WaitForExit() => this.disconnectEvent.WaitOne();

    private void OnConfigurationChanged(object? sender, ConfigurationChangedEventArgs e)
    {
        using var done = new AutoResetEvent(false);
        _ = ThreadPool.QueueUserWorkItem((_) =>
        {
            try
            {
                JoinableTaskFactory taskFactory = this.syncTaskContext.CreateFactory(this.syncTaskContext.CreateCollection());

                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                taskFactory.Run(async () => await this.SendDiagnosticsAsync());
            }
            finally
            {
                _ = done.Set();
            }
        });

        _ = done.WaitOne();
    }

    private void OnInitialized(object? sender, EventArgs e)
    {
    }

    private void OnRpcDisconnected(object? sender, JsonRpcDisconnectedEventArgs e) => this.Exit();
}