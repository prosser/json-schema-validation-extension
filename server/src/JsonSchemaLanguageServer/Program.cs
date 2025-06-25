// <copyright file="Program.cs">Copyright (c) Peter Rosser.</copyright>

using System.Runtime.InteropServices;

[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM.

[assembly: Guid("7aef478a-6fb9-482e-92c7-fa7f8126416a")]

namespace Rosser.Extensions.JsonSchemaLanguageServer;

using System.Net.Http;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal class Program
{
    static async Task<int> Main(string[] args)
    {
        bool debugMode = args.Any(x => x.Equals("--debug", StringComparison.OrdinalIgnoreCase));
        bool waitForAttach = args.Any(x => x.Equals("--waitForAttach", StringComparison.OrdinalIgnoreCase));
        bool launchDebugger = args.Any(x => x.Equals("--launchDebugger", StringComparison.OrdinalIgnoreCase));

        if (launchDebugger)
        {
            _ = Debugger.Launch();
        }

        while (waitForAttach && !Debugger.IsAttached)
        {
            Thread.Sleep(1000);
        }

        var services = new ServiceCollection();
        RegisterServices(debugMode, services);

        ServiceProvider container = services.BuildServiceProvider();

        ILogger<Program> logger = container.GetRequiredService<ILogger<Program>>();

        for (int i = 0; i < args.Length; i++)
        {
            logger.LogInformation("arg{i}: '{arg}'", i, args[i]);
        }

        try
        {
            using CancellationTokenSource cts = new();
            CancellationToken ct = cts.Token;

            using LanguageServerHost host = container.GetRequiredService<LanguageServerHost>();

            Console.CancelKeyPress += (sender, e) =>
            {
                cts.Cancel();
                host.Dispose();
                e.Cancel = true;
            };

            await host.InitializeAsync(ct);
            host.Disconnected.Wait();

            return 0;
        }
        catch (OperationCanceledException)
        {
            return 0;
        }
    }

    private static void RegisterServices(bool debugMode, ServiceCollection services)
    {
        _ = services.AddSingleton<ConfigurationProvider>();
        _ = services.AddSingleton<LanguageServerHost>();
        _ = services.AddSingleton<LanguageServer>();
        _ = services.AddSingleton<SchemaAnalyzer>();
        _ = services.AddSingleton<SchemaProvider>();
        _ = services.AddSingleton<HttpMessageHandler, FileSystemHandler>();
        _ = services.AddSingleton<FileSystemCache>();
        _ = services.AddLogging(o =>
        {
            _ = o.SetMinimumLevel(debugMode ? LogLevel.Debug : LogLevel.Information);
            _ = o.AddProvider(new LogFileProvider(new LogFileProviderOptions { BaseName = "JsonSchemaLanguageServer", LogDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)! }));
            _ = o.AddDebug();
        });
    }
}
