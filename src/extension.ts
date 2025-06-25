// The module 'vscode' contains the VS Code extensibility API
// Import the module and reference it with the alias vscode in your code below
import * as fs from 'fs';
import * as OS from 'os';
import * as path from 'path';
import * as vscode from 'vscode';
import { workspace } from 'vscode';
import {
  DidChangeConfigurationParams,
  Executable,
  LanguageClient,
  LanguageClientOptions,
  ServerOptions
} from 'vscode-languageclient/node';


let client: LanguageClient;

// this method is called when your extension is activated
// your extension is activated the very first time the command is executed
export function activate(context: vscode.ExtensionContext) {
  const serverPath = OS.platform() === 'win32'
    ? path.join('win-x64', 'JsonSchemaLanguageServer.exe')
    : OS.platform() === 'darwin'
      ? path.join('osx-x64', 'JsonSchemaLanguageServer')
      : path.join('linux-x64', 'JsonSchemaLanguageServer');

  let serverModule = context.asAbsolutePath(path.join('server', 'src', 'JsonSchemaLanguageServer', 'bin', 'Debug', 'net9.0', 'JsonSchemaLanguageServer.exe'));

  if (!fs.existsSync(serverModule)) {
    serverModule = context.asAbsolutePath(path.join('dist', 'server', serverPath));
  }

  const workPath = path.dirname(serverModule);

  const run: Executable = {
    command: serverModule,
    options: { cwd: workPath }
  };
  const debug: Executable = {
    ...run,
    args: ['--debug', '--launchDebugger']
  };
  console.log(serverModule);

  // If the extension is launched in debug mode then the debug server options are used
  // Otherwise the run options are used
  const serverOptions: ServerOptions = { run, debug };

  // Options to control the language client
  const clientOptions: LanguageClientOptions = {
    // Register the server for JSON documents
    documentSelector: [
        { scheme: 'file', language: 'json' },
        { scheme: 'file', language: 'jsonc' }],
    synchronize: {
      // Notify the server about file changes to .json files contained in the workspace
      fileEvents: workspace.createFileSystemWatcher('**/.json*')
    }
  };

  // Create the language client and start the client.
  client = new LanguageClient(
    'jsonSchemaLanguageServer',
    'JSON Schema Language Server',
    serverOptions,
    clientOptions
  );

  // Start the client. This will also launch the server
  client.start();
  client.onReady().then(() => {
    sendConfig();
  });

  workspace.onDidChangeConfiguration(e => {
    if (e.affectsConfiguration('jsonSchemaDiagnostics')) {
      sendConfig();
    }
  });
}

function sendConfig() {
  const config = workspace.getConfiguration('jsonSchemaDiagnostics');
  const n: DidChangeConfigurationParams = {
    settings: config
  };
  client.sendNotification("workspace/didChangeConfiguration", n);
}

export function deactivate(): Thenable<void> | undefined {
  if (!client) {
    return undefined;
  }
  return client.stop();
}
