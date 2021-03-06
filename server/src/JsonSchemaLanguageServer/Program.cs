// <copyright file="Program.cs">Copyright (c) Peter Rosser.</copyright>
// #define WAIT_FOR_DEBUGGER
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM.

[assembly: Guid("7aef478a-6fb9-482e-92c7-fa7f8126416a")]

[assembly: InternalsVisibleTo("JsonSchemaLanguageServerUnitTests")]

namespace Rosser.Extensions.JsonSchemaLanguageServer
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    using Rosser.Extensions.JsonSchemaLanguageServer.Logging;
    using Rosser.Extensions.JsonSchemaLanguageServer.Services;

    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            bool debugMode = args.Any(x => x.Equals("--debug", StringComparison.OrdinalIgnoreCase));
            bool waitForAttach = args.Any(x => x.Equals("--waitForAttach", StringComparison.OrdinalIgnoreCase));
            bool launchDebugger = args.Any(x => x.Equals("--launchDebugger", StringComparison.OrdinalIgnoreCase));

            if (launchDebugger)
            {
                Debugger.Launch();
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
                logger.LogInformation($"arg{i}: '{args[i]}'");
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
            services.AddSingleton<ConfigurationProvider>();
            services.AddSingleton<LanguageServerHost>();
            services.AddSingleton<LanguageServer>();
            services.AddSingleton<SchemaAnalyzer>();
            services.AddSingleton<SchemaProvider>();
            services.AddSingleton<HttpMessageHandler, FileSystemHandler>();
            services.AddSingleton<FileSystemCache>();
            services.AddLogging(o =>
            {
                o.SetMinimumLevel(debugMode ? LogLevel.Debug : LogLevel.Information);
                o.AddProvider(new LogFileProvider(new LogFileProviderOptions { BaseName = "JsonSchemaLanguageServer", LogDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)! }));
                o.AddDebug();
            });
        }
    }
}
