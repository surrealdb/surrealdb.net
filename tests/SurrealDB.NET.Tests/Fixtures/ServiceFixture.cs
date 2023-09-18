using Microsoft.Extensions.Configuration;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Xunit.Abstractions;
using System.Text;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SurrealDB.NET.Http;
using SurrealDB.NET.Rpc;

namespace SurrealDB.NET.Tests.Fixtures;

internal sealed class ServiceFixture : IDisposable
{
    private readonly XunitOutputWriter _writer;
    private readonly ServiceProvider _serviceRoot;
    private readonly IServiceScope _serviceScope;

    public ISurrealRpcClient TextRpc => _serviceScope.ServiceProvider.GetRequiredService<SurrealTextRpcClient>();

    //public ISurrealRpcClient BinaryRpc => _serviceScope.ServiceProvider.GetRequiredService<SurrealBinaryRpcClient>();

	public ISurrealHttpClient Http => _serviceScope.ServiceProvider.GetRequiredService<SurrealJsonHttpClient>();

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
            .AddSingleton<IConfiguration>(new ConfigurationBuilder().Build())
            .AddSurrealDB()
            .Configure<SurrealOptions>(opt =>
            {
				opt.DefaultNamespace = "test";
				opt.DefaultDatabase = "test";
				opt.Endpoint = new Uri($"http://localhost:{surrealPort}");
				opt.Secure = false;
				opt.JsonRequestOptions.WriteIndented = true;
				opt.JsonResponseOptions.WriteIndented = true;
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
#pragma warning disable CA1031 // Prevent lost logs from failing tests
			catch { }
#pragma warning restore CA1031
		}
    }

    private sealed class XunitLogger : ILogger
    {
        private readonly ITestOutputHelper _xunit;

		private sealed class EmptyDisposable : IDisposable
		{
			public void Dispose()
			{
			}
		}

		public XunitLogger(ITestOutputHelper xunit)
        {
            _xunit = xunit;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
			return new EmptyDisposable();
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
#pragma warning disable CA1031 // Prevent lost logs from failing tests
			catch { }
#pragma warning restore CA1031
		}
	}

#pragma warning disable CA1812 // DI
	private sealed class XunitLoggerProvider : ILoggerProvider
#pragma warning restore CA1812
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
