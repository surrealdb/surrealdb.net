using Microsoft.Extensions.Configuration;
using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using SurrealDB.NET.Json;

using Xunit.Abstractions;
using System.Text;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace SurrealDB.NET.Tests.Fixtures;

internal sealed class ServiceFixture : IDisposable
{
    private readonly XunitOutputWriter _writer;
    private readonly ServiceProvider _serviceRoot;
    private readonly IServiceScope _serviceScope;

    public ISurrealClient Client => _serviceScope.ServiceProvider.GetRequiredService<ISurrealClient>();

    public void Dispose()
    {
        _writer.Dispose();
        _serviceScope.Dispose();
        _serviceRoot.Dispose();
    }

    public ServiceFixture(ITestOutputHelper xunit, ushort surrealPort)
    {
        var xunitWriter = new XunitOutputWriter(xunit);
        _writer = xunitWriter;
        Console.SetOut(xunitWriter);
        Console.SetError(xunitWriter);

        _serviceRoot = new ServiceCollection()
            .AddSingleton(xunit)
            .AddLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Trace);
                logging.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, XunitLoggerProvider>());
            })
            .AddSingleton<IConfiguration>(new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{SurrealOptions.Section}:Endpoint"] = $"ws://localhost:{surrealPort}/rpc",
                [$"{SurrealOptions.Section}:DefaultNamespace"] = "test",
                [$"{SurrealOptions.Section}:DefaultDatabase"] = "test",
            }).Build())
            .AddSurrealDB()
            .Configure<SurrealOptions>(opt =>
            {
				opt.JsonRequestOptions.WriteIndented = true;
            })
            .BuildServiceProvider(new ServiceProviderOptions
            {
                ValidateOnBuild = true,
                ValidateScopes = true,
            });

        _serviceScope = _serviceRoot.CreateScope();
    }

    private sealed class XunitOutputWriter : TextWriter
    {
        private readonly ITestOutputHelper _output;
        public XunitOutputWriter(ITestOutputHelper output)
        {
            _output = output;
        }
        public override Encoding Encoding => Console.OutputEncoding;
        public override void WriteLine(string? value)
        {
            try
            {
                _output.WriteLine(value);
            }
            catch { }
        }
    }

    private sealed class XunitLogger : ILogger
    {
        private readonly ITestOutputHelper _xunit;

        public XunitLogger(ITestOutputHelper xunit)
        {
            _xunit = xunit;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            throw new NotImplementedException();
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            try
            {
                var message = formatter(state, exception);
                _xunit.WriteLine(message);
            }
            catch { }
        }
    }

    private sealed class XunitLoggerProvider : ILoggerProvider
    {
        private readonly ITestOutputHelper _xunit;

        public XunitLoggerProvider(ITestOutputHelper xunit)
        {
            _xunit = xunit;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new XunitLogger(_xunit);
        }

        public void Dispose()
        {
        }
    }
}
