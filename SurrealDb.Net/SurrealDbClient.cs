using Dahomey.Cbor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using Serilog;
using Serilog.Events;
using SurrealDb.Net.Extensions.DependencyInjection;
using SurrealDb.Net.Internals;

namespace SurrealDb.Net;

/// <summary>
/// The entry point to communicate with a SurrealDB instance.
/// Authenticate, use namespace/database, execute queries, etc...
/// </summary>
public class SurrealDbClient : BaseSurrealDbClient, ISurrealDbClient
{
    internal ISurrealDbEngine Engine => _engine;

    /// <summary>
    /// Creates a new SurrealDbClient, with the defined endpoint.
    /// </summary>
    /// <param name="endpoint">The endpoint to access a SurrealDB instance.</param>
    /// <param name="namingPolicy">The naming policy to use for serialization.</param>
    /// <param name="httpClientFactory">An IHttpClientFactory instance, or none.</param>
    /// <param name="configureCborOptions">An optional action to configure <see cref="CborOptions"/>.</param>
    /// <param name="loggerFactory">
    /// An instance of <see cref="Microsoft.Extensions.Logging.ILoggerFactory"/> used to log messages.
    /// </param>
    /// <exception cref="ArgumentException"></exception>
    public SurrealDbClient(
        string endpoint,
        string? namingPolicy = null,
        IHttpClientFactory? httpClientFactory = null,
        Action<CborOptions>? configureCborOptions = null,
        ILoggerFactory? loggerFactory = null
    )
        : this(
            new SurrealDbOptions(endpoint, namingPolicy),
            httpClientFactory,
            configureCborOptions,
            loggerFactory
        ) { }

    /// <summary>
    /// Creates a new SurrealDbClient using a specific configuration.
    /// </summary>
    /// <param name="configuration">The configuration options for the SurrealDbClient.</param>
    /// <param name="httpClientFactory">An IHttpClientFactory instance, or none.</param>
    /// <param name="configureCborOptions">An optional action to configure <see cref="CborOptions"/>.</param>
    /// <param name="loggerFactory">
    /// An instance of <see cref="ILoggerFactory"/> used to log messages.
    /// </param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public SurrealDbClient(
        SurrealDbOptions configuration,
        IHttpClientFactory? httpClientFactory = null,
        Action<CborOptions>? configureCborOptions = null,
        ILoggerFactory? loggerFactory = null
    )
        : this(configuration, null, httpClientFactory, configureCborOptions, null, loggerFactory)
    { }

    internal SurrealDbClient(
        SurrealDbOptions configuration,
        IServiceProvider? serviceProvider,
        IHttpClientFactory? httpClientFactory = null,
        Action<CborOptions>? configureCborOptions = null,
        Func<Task>? poolTask = null,
        ILoggerFactory? loggerFactory = null
    )
    {
        if (configuration.Endpoint is null)
            throw new ArgumentNullException(nameof(configuration), "The endpoint is required.");

        _poolTask = poolTask;

        Uri = new Uri(configuration.Endpoint);
        if (Uri.Scheme is "ws" or "wss" && !Uri.AbsolutePath.EndsWith("/rpc"))
        {
            string absoluteNakedPath = Uri.AbsolutePath.EndsWith("/")
                ? Uri.AbsolutePath[..^1]
                : Uri.AbsolutePath;
            Uri = new Uri(Uri, $"{absoluteNakedPath}/rpc");
        }

        NamingPolicy = configuration.NamingPolicy;

        var protocol = Uri.Scheme;

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console(restrictedToMinimumLevel: LogEventLevel.Debug) // restricted... is Optional
            .CreateLogger();

        _engine = protocol switch
        {
            "http"
            or "https"
                => new SurrealDbHttpEngine(
                    configuration,
                    httpClientFactory,
                    configureCborOptions,
                    loggerFactory is not null ? new SurrealDbLoggerFactory(loggerFactory) : null
                ),
            "ws"
            or "wss"
                => new SurrealDbWsEngine(
                    configuration,
                    configureCborOptions,
                    loggerFactory is not null ? new SurrealDbLoggerFactory(loggerFactory) : null
                ),
            "mem"
                => ResolveEmbeddedProvider<ISurrealDbInMemoryEngine>(
                    serviceProvider,
                    configuration,
                    configureCborOptions,
                    loggerFactory
                )
                    ?? throw new Exception(
                        "Impossible to create a new in-memory SurrealDB client. Make sure to use `AddInMemoryProvider`."
                    ),
            "rocksdb"
                => ResolveEmbeddedProvider<ISurrealDbRocksDbEngine>(
                    serviceProvider,
                    configuration,
                    configureCborOptions,
                    loggerFactory
                )
                    ?? throw new Exception(
                        "Impossible to create a new file SurrealDB client, backed by RocksDB. Make sure to use `AddRocksDbProvider`."
                    ),
            "surrealkv"
                => ResolveEmbeddedProvider<ISurrealDbKvEngine>(
                    serviceProvider,
                    configuration,
                    configureCborOptions,
                    loggerFactory
                )
                    ?? throw new Exception(
                        "Impossible to create a new file SurrealDB client, backed by SurrealKV. Make sure to use `AddSurrealKvProvider`."
                    ),
            _ => throw new NotSupportedException($"The protocol '{protocol}' is not supported."),
        };
    }

    internal SurrealDbClient(
        SurrealDbOptions configuration,
        ISurrealDbEngine engine,
        Func<Task>? poolTask = null
    )
    {
        Uri = new Uri(configuration.Endpoint!);
        NamingPolicy = configuration.NamingPolicy;

        _engine = engine;
        _poolTask = poolTask;
    }

    private T? ResolveEmbeddedProvider<T>(
        IServiceProvider? serviceProvider,
        SurrealDbOptions configuration,
        Action<CborOptions>? configureCborOptions,
        ILoggerFactory? loggerFactory
    )
        where T : class, ISurrealDbProviderEngine
    {
        var engine = serviceProvider?.GetService<T>();
        if (engine is not null)
        {
            InitializeProviderEngine(engine, configuration, configureCborOptions, loggerFactory);
        }

        return engine;
    }
}
