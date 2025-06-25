// <copyright file="ServerTarget.cs">Copyright (c) Peter Rosser.</copyright>

namespace Rosser.Extensions.JsonSchemaLanguageServer;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.VisualStudio.LanguageServer.Protocol;

using Newtonsoft.Json.Linq;

using Rosser.Extensions.JsonSchemaLanguageServer.Services;

using StreamJsonRpc;

public class ServerTarget(LanguageServer server)
{
    public event EventHandler? Initialized;

    [JsonRpcMethod(Methods.InitializeName)]
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
                TriggerCharacters = [",", "."]
            }
        };

        var result = new InitializeResult
        {
            Capabilities = capabilities
        };

        Initialized?.Invoke(this, new EventArgs());

        return result;
    }

    [JsonRpcMethod(Methods.TextDocumentDidOpenName)]
    public void OnTextDocumentOpened(JToken arg)
    {
        DidOpenTextDocumentParams? parameter = arg.ToObject<DidOpenTextDocumentParams>();
        if (parameter is null)
        {
            return;
        }

        _ = Task.Run(async () =>
        {
            await server.OnTextDocumentOpenedAsync(parameter);
        }).GetAwaiter();
    }

    [JsonRpcMethod(Methods.TextDocumentDidChangeName)]
    public void OnTextDocumentChanged(JToken arg)
    {
        DidChangeTextDocumentParams? parameter = arg.ToObject<DidChangeTextDocumentParams>();
        if (parameter is null)
        {
            return;
        }

        _ = server.SendDiagnosticsAsync(parameter.TextDocument.Uri.ToString(), parameter.ContentChanges[0].Text);
    }

    [JsonRpcMethod(Methods.TextDocumentCompletionName)]
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Public interface")]
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

        return [.. items];
    }

    [JsonRpcMethod(Methods.WorkspaceDidChangeConfigurationName)]
    public void OnDidChangeConfiguration(JToken arg)
    {
        DidChangeConfigurationParams? parameter = arg.ToObject<DidChangeConfigurationParams>();
        if (parameter is null)
        {
            return;
        }

        server.SendSettings(parameter);
    }

    [JsonRpcMethod(Methods.ShutdownName)]
    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Public interface")]
    public object? Shutdown() => null;

    [JsonRpcMethod(Methods.ExitName)]
    public void Exit() => server.Exit();

    public string GetText() => string.IsNullOrWhiteSpace(server.CustomText) ? "custom text from language server target" : server.CustomText;
}
