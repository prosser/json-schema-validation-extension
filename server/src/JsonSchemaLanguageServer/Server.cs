// <copyright file="Server.cs">Copyright (c) Peter Rosser.</copyright>

namespace Rosser.Extensions.JsonSchemaLanguageServer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.Logging;
    using Microsoft.VisualStudio.LanguageServer.Protocol;

    using Newtonsoft.Json.Linq;

    using StreamJsonRpc;

    public class Server
    {
        private int maxProblems = -1;
        private JsonRpc? rpc;
        private readonly ServerTarget target;
        private readonly ManualResetEvent disconnectEvent = new(false);
        private readonly SchemaAnalyzer schemaAnalyzer;
        private Dictionary<string, DiagnosticSeverity>? diagnostics;
        private TextDocumentItem? textDocument = null;

        private int counter = 100;

        public Server(ILogger<Server> logger, SchemaAnalyzer schemaAnalyzer)
        {
            this.target = new ServerTarget(this);
            this.schemaAnalyzer = schemaAnalyzer;
        }

        public string CustomText { get; set; } = string.Empty;

        public string CurrentSettings { get; private set; } = string.Empty;

        public event EventHandler? Disconnected;

        public async Task InitializeAsync(Stream sender, Stream reader, Dictionary<string, DiagnosticSeverity>? initialDiagnostics = null, CancellationToken ct = default)
        {
            this.rpc = JsonRpc.Attach(sender, reader, this.target);
            this.rpc.Disconnected += this.OnRpcDisconnected;
            this.diagnostics = initialDiagnostics;

            this.target.Initialized += this.OnInitialized;

            await this.schemaAnalyzer.InitializeAsync(ct);

            this.OnInitialized(this, EventArgs.Empty);
        }

        private JsonRpc Rpc => this.rpc ?? throw new InvalidOperationException("Not inialized");

        private void OnInitialized(object? sender, EventArgs e)
        {
        }

        public Task OnTextDocumentOpenedAsync(DidOpenTextDocumentParams messageParams)
        {
            this.textDocument = messageParams.TextDocument;

            return this.SendDiagnosticsAsync();
        }

        public void SetDiagnostics(Dictionary<string, DiagnosticSeverity> diagnostics) => this.diagnostics = diagnostics;

        public async Task SendDiagnosticsAsync()
        {
            if (this.textDocument is null)
            {
                return;
            }

            List<Diagnostic> diagnostics = this.schemaAnalyzer.Analyze(this.textDocument.Text);
            this.SetDiagnostics(diagnostics.ToDictionary(x => x.Message, x => x.Severity ?? DiagnosticSeverity.Hint));

            PublishDiagnosticParams parameter = new()
            {
                Uri = this.textDocument.Uri,
                Diagnostics = diagnostics.ToArray()
            };

            if (this.maxProblems > -1)
            {
                parameter.Diagnostics = parameter.Diagnostics.Take(this.maxProblems).ToArray();
            }

            await this.Rpc.NotifyWithParameterObjectAsync(Methods.TextDocumentPublishDiagnosticsName, parameter);
        }

        public Task SendDiagnosticsAsync(string uri, string text)
        {
            //string[] lines = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            List<Diagnostic> diagnostics = this.schemaAnalyzer.Analyze(text);
            this.SetDiagnostics(diagnostics.ToDictionary(x => x.Message, x => x.Severity ?? DiagnosticSeverity.Hint));

            PublishDiagnosticParams parameter = new()
            {
                Uri = new Uri(uri),
                Diagnostics = diagnostics.ToArray()
            };

            if (this.maxProblems > -1)
            {
                parameter.Diagnostics = parameter.Diagnostics.Take(this.maxProblems).ToArray();
            }

            return this.Rpc.NotifyWithParameterObjectAsync(Methods.TextDocumentPublishDiagnosticsName, parameter);
        }

        public Task LogMessageAsync(MessageType messageType) => this.LogMessageAsync("testing " + this.counter++, messageType);

        public Task LogMessageAsync(string message, MessageType messageType)
        {
            LogMessageParams parameter = new()
            {
                Message = message,
                MessageType = messageType
            };
            return this.Rpc.NotifyWithParameterObjectAsync(Methods.WindowLogMessageName, parameter);
        }

        public Task ShowMessageAsync(string message, MessageType messageType)
        {
            ShowMessageParams parameter = new()
            {
                Message = message,
                MessageType = messageType
            };
            return this.Rpc.NotifyWithParameterObjectAsync(Methods.WindowShowMessageName, parameter);
        }

        public async Task<MessageActionItem> ShowMessageRequestAsync(string message, MessageType messageType, string[] actionItems)
        {
            ShowMessageRequestParams parameter = new()
            {
                Message = message,
                MessageType = messageType,
                Actions = actionItems.Select(a => new MessageActionItem { Title = a }).ToArray()
            };

            JToken? response = await this.Rpc.InvokeWithParameterObjectAsync<JToken>(Methods.WindowShowMessageRequestName, parameter);
            return response.ToObject<MessageActionItem>();
        }

        public async Task SendSettingsAsync(DidChangeConfigurationParams parameter)
        {
            this.CurrentSettings = parameter.Settings.ToString() ?? string.Empty;

            var parsedSettings = JToken.Parse(this.CurrentSettings);
            int newMaxProblems = parsedSettings.Children().First().Values<int>("maxNumberOfProblems").First();
            if (this.maxProblems != newMaxProblems)
            {
                this.maxProblems = newMaxProblems;
                await this.SendDiagnosticsAsync();
            }
        }

        public void WaitForExit() => this.disconnectEvent.WaitOne();

        public void Exit()
        {
            this.disconnectEvent.Set();

            Disconnected?.Invoke(this, new EventArgs());
        }

        private static Diagnostic? GetDiagnostic(string line, int lineOffset, ref int characterOffset, string wordToMatch, DiagnosticSeverity severity)
        {
            if ((characterOffset + wordToMatch.Length) <= line.Length)
            {
                string? subString = line.Substring(characterOffset, wordToMatch.Length);
                if (subString.Equals(wordToMatch, StringComparison.OrdinalIgnoreCase))
                {
                    var diagnostic = new Diagnostic
                    {
                        Message = "This is an " + Enum.GetName(typeof(DiagnosticSeverity), severity),
                        Severity = severity,
                        Range = new()
                        {
                            Start = new Position(lineOffset, characterOffset),
                            End = new Position(lineOffset, characterOffset + wordToMatch.Length)
                        },
                        Code = "Test" + Enum.GetName(typeof(DiagnosticSeverity), severity)
                    };
                    characterOffset += wordToMatch.Length;

                    return diagnostic;
                }
            }

            return null;
        }

        private void OnRpcDisconnected(object? sender, JsonRpcDisconnectedEventArgs e) => this.Exit();
    }
}
