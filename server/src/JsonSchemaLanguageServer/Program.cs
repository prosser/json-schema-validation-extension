// <copyright file="Program.cs">Copyright (c) Peter Rosser.</copyright>

#define WAIT_FOR_DEBUGGER

namespace Rosser.Extensions.JsonSchemaLanguageServer
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            bool debugMode = args.Any(x => x.Equals("--debug", StringComparison.OrdinalIgnoreCase));
            bool waitForAttach = args.Any(x => x.Equals("--wait-for-attach", StringComparison.OrdinalIgnoreCase));

            if (waitForAttach)
            {
                while (!System.Diagnostics.Debugger.IsAttached)
                {
                    Thread.Sleep(1000);
                }
            }

            var services = new ServiceCollection();
            services.AddTransient<LanguageServerHost>();
            services.AddTransient<Server>();
            services.AddTransient<SchemaAnalyzer>();
            services.AddLogging(o =>
            {
                o.SetMinimumLevel(debugMode ? LogLevel.Debug : LogLevel.Information);
                o.AddProvider(new LogFileProvider(new LogFileProviderOptions { BaseName = "JsonSchemaLanguageServer", LogDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)! }));
                o.AddDebug();
            });

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
    }
}
