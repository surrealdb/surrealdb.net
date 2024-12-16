using Dahomey.Cbor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SurrealDb.Net.Extensions.DependencyInjection;
using SurrealDb.Net.Internals;

namespace SurrealDb.Net;

public abstract partial class BaseSurrealDbClient : ISurrealDbClient
{
    protected ISurrealDbEngine _engine { get; set; } = null!;

    protected Func<Task>? _poolTask { get; set; } = null;

    public Uri Uri { get; protected set; } = null!;
    public string? NamingPolicy { get; protected set; }

    protected void InitializeProviderEngine(
        ISurrealDbProviderEngine engine,
        SurrealDbOptions configuration,
        Action<CborOptions>? configureCborOptions,
        ILoggerFactory? loggerFactory
    )
    {
        engine.Initialize(
            configuration,
            configureCborOptions,
            loggerFactory is not null ? new SurrealDbLoggerFactory(loggerFactory) : null
        );
    }
}
