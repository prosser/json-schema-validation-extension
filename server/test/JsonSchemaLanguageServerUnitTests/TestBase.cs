// <copyright file="TestBase.cs">Copyright (c) Peter Rosser.</copyright>

namespace JsonSchemaLanguageServerUnitTests
{
    using System;
    using System.Net.Http;

    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    using Moq;

    using Rosser.Extensions.JsonSchemaLanguageServer.Services;

    using Xunit.Abstractions;

    public abstract class TestBase
    {
        protected readonly ITestOutputHelper output;
        protected readonly ServiceProvider serviceProvider;

        public TestBase(ITestOutputHelper output)
        {
            this.output = output;
            var loggerMock = new Mock<ILogger<LanguageServer>>();
            loggerMock.Setup(x => x.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()))
                .Callback(new InvocationAction(action =>
                {
                    this.output.WriteLine($"{action.Arguments[0]} {action.Arguments[2]?.ToString()}");
                }));

            var services = new ServiceCollection();
            services.AddScoped<ConfigurationProvider>();
            services.AddScoped<FileSystemCache>();
            services.AddScoped<HttpMessageHandler, FileSystemHandler>();
            services.AddScoped<SchemaProvider>();
            services.AddScoped<SchemaAnalyzer>();
            services.AddSingleton(loggerMock.Object);

            this.serviceProvider = services.BuildServiceProvider();
        }
    }
}
