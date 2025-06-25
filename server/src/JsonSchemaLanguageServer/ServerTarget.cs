// <copyright file="ServerTarget.cs">Copyright (c) Peter Rosser.</copyright>

namespace Rosser.Extensions.JsonSchemaLanguageServer
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Microsoft.VisualStudio.LanguageServer.Protocol;
    using Microsoft.VisualStudio.Threading;

    using Newtonsoft.Json.Linq;

    using Rosser.Extensions.JsonSchemaLanguageServer.Services;

    using StreamJsonRpc;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD110:Observe result of async calls", Justification = "Intentional")]
    public class ServerTarget
    {
        private readonly LanguageServer server;

        public ServerTarget(LanguageServer server)
        {
            this.server = server;
        }

        public event EventHandler? Initialized;

        [JsonRpcMethod(Methods.InitializeName)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Part of public contract")]
        public object Initialize(JToken arg)
        {
            var capabilities = new ServerCapabilities
            {
                TextDocumentSync = new TextDocumentSyncOptions
                {
                    OpenClose = true,
                    Change = TextDocumentSyncKind.Full
                },
                CompletionProvider = new CompletionOptions
                {
                    ResolveProvider = false,
                    TriggerCharacters = new string[] { ",", "." }
                }
            };

            var result = new InitializeResult
            {
                Capabilities = capabilities
            };

            Initialized?.Invoke(this, new EventArgs());

            return result;
        }

        [JsonRpcMethod(Methods.InitializedName)]
        public void OnInitialized(JToken arg)
        {
            // The InitializedParams object is empty in the LSP spec, so we don't need to convert the arg
            // This method signals that the client and server have finished their handshake
            // Trigger the Initialized event if not already triggered in Initialize
            Initialized?.Invoke(this, new EventArgs());
        }

        [JsonRpcMethod(Methods.TextDocumentDidOpenName)]
        public void OnTextDocumentOpened(JToken arg)
        {
            DidOpenTextDocumentParams? parameter = arg.ToObject<DidOpenTextDocumentParams>();
            // Access the JoinableTaskContext from the LanguageServer instance to properly handle the async call
            var jtf = new JoinableTaskFactory(this.server.syncTaskContext);
            jtf.Run(() => this.server.OnTextDocumentOpenedAsync(parameter));
        }

        [JsonRpcMethod(Methods.TextDocumentDidChangeName)]
        public void OnTextDocumentChanged(JToken arg)
        {
            DidChangeTextDocumentParams? parameter = arg.ToObject<DidChangeTextDocumentParams>();
            this.server.SendDiagnosticsAsync(parameter.TextDocument.Uri.ToString(), parameter.ContentChanges[0].Text);
        }

        [JsonRpcMethod(Methods.TextDocumentCompletionName)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Required for contract")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Shipped public API")]
        public CompletionItem[] OnTextDocumentCompletion(JToken arg)
        {
            var items = new List<CompletionItem>();

            for (int i = 0; i < 10; i++)
            {
                var item = new CompletionItem
                {
                    Label = "Item " + i,
                    InsertText = "Item" + i,
                    Kind = (CompletionItemKind)(i % Enum.GetNames(typeof(CompletionItemKind)).Length + 1)
                };
                items.Add(item);
            }

            return items.ToArray();
        }

        [JsonRpcMethod(Methods.WorkspaceDidChangeConfigurationName)]
        public void OnDidChangeConfiguration(JToken arg)
        {
            DidChangeConfigurationParams? parameter = arg.ToObject<DidChangeConfigurationParams>();
            this.server.SendSettings(parameter);
        }

        [JsonRpcMethod(Methods.ShutdownName)]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Required for contract")]
        public object? Shutdown() => null;

        [JsonRpcMethod(Methods.ExitName)]
        public void Exit() => this.server.Exit();

        public string GetText() => string.IsNullOrWhiteSpace(this.server.CustomText) ? "custom text from language server target" : this.server.CustomText;
    }
}
