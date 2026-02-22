using Dahomey.Cbor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SurrealDb.Net.Extensions.DependencyInjection;
using SurrealDb.Net.Internals;
using SurrealDb.Net.Internals.DependencyInjection;

namespace SurrealDb.Net;

/// <summary>
/// The entry point to communicate with a SurrealDB instance.
/// Authenticate, use namespace/database, execute queries, etc...
/// </summary>
public class SurrealDbClient : BaseSurrealDbClient
{
    /// <summary>
    /// Creates a new SurrealDbClient, with the defined endpoint.
    /// </summary>
    /// <param name="endpoint">The endpoint to access a SurrealDB instance.</param>
    /// <param name="httpClientFactory">An IHttpClientFactory instance, or none.</param>
    /// <param name="configureCborOptions">An optional action to configure <see cref="CborOptions"/>.</param>
    /// <param name="loggerFactory">
    /// An instance of <see cref="ILoggerFactory"/> used to log messages.
    /// </param>
    /// <exception cref="ArgumentException"></exception>
    public SurrealDbClient(
        string endpoint,
        IHttpClientFactory? httpClientFactory = null,
        Action<CborOptions>? configureCborOptions = null,
        ILoggerFactory? loggerFactory = null
    )
        : this(
            new SurrealDbOptions(endpoint),
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
        : this(configuration, null, httpClientFactory, configureCborOptions, loggerFactory) { }

    internal SurrealDbClient(
        SurrealDbOptions configuration,
        IServiceProvider? serviceProvider,
        IHttpClientFactory? httpClientFactory = null,
        Action<CborOptions>? configureCborOptions = null,
        ILoggerFactory? loggerFactory = null,
        ISessionizer? sessionizer = null
    )
    {
        Uri = ParseEndpoint(configuration);
        Engine = CreateEngine(
            configuration,
            serviceProvider,
            httpClientFactory,
            configureCborOptions,
            loggerFactory,
            sessionizer,
            Uri
        );
    }

    private static Uri ParseEndpoint(SurrealDbOptions configuration)
    {
        if (configuration.Endpoint is null)
            throw new ArgumentNullException(nameof(configuration), "The endpoint is required.");

        var endpointUri = new Uri(configuration.Endpoint);
        if (endpointUri.Scheme is "ws" or "wss" && !endpointUri.AbsolutePath.EndsWith("/rpc"))
        {
            string absoluteNakedPath = endpointUri.AbsolutePath.EndsWith('/')
                ? endpointUri.AbsolutePath[..^1]
                : endpointUri.AbsolutePath;
            return new Uri(endpointUri, $"{absoluteNakedPath}/rpc");
        }

        return endpointUri;
    }

    private static ISurrealDbEngine CreateEngine(
        SurrealDbOptions configuration,
        IServiceProvider? serviceProvider,
        IHttpClientFactory? httpClientFactory,
        Action<CborOptions>? configureCborOptions,
        ILoggerFactory? loggerFactory,
        ISessionizer? sessionizer,
        Uri uri
    )
    {
        string protocol = uri.Scheme;
        return protocol switch
        {
            "http" or "https" => new SurrealDbHttpEngine(
                configuration,
                httpClientFactory,
                configureCborOptions,
                loggerFactory is not null ? new SurrealDbLoggerFactory(loggerFactory) : null,
                sessionizer
            ),
            "ws" or "wss" => new SurrealDbWsEngine(
                configuration,
                configureCborOptions,
                loggerFactory is not null ? new SurrealDbLoggerFactory(loggerFactory) : null,
                sessionizer
            ),
            "mem" => ResolveEmbeddedProvider<ISurrealDbInMemoryEngine>(
                serviceProvider,
                configuration,
                configureCborOptions,
                loggerFactory,
                sessionizer
            )
                ?? throw new Exception(
                    "Impossible to create a new in-memory SurrealDB client. Make sure to use `AddInMemoryProvider`."
                ),
            "rocksdb" => ResolveEmbeddedProvider<ISurrealDbRocksDbEngine>(
                serviceProvider,
                configuration,
                configureCborOptions,
                loggerFactory,
                sessionizer
            )
                ?? throw new Exception(
                    "Impossible to create a new file SurrealDB client, backed by RocksDB. Make sure to use `AddRocksDbProvider`."
                ),
            "surrealkv" => ResolveEmbeddedProvider<ISurrealDbKvEngine>(
                serviceProvider,
                configuration,
                configureCborOptions,
                loggerFactory,
                sessionizer
            )
                ?? throw new Exception(
                    "Impossible to create a new file SurrealDB client, backed by SurrealKV. Make sure to use `AddSurrealKvProvider`."
                ),
            _ => throw new NotSupportedException($"The protocol '{protocol}' is not supported."),
        };
    }

    private static T? ResolveEmbeddedProvider<T>(
        IServiceProvider? serviceProvider,
        SurrealDbOptions configuration,
        Action<CborOptions>? configureCborOptions,
        ILoggerFactory? loggerFactory,
        ISessionizer? sessionizer
    )
        where T : class, ISurrealDbProviderEngine
    {
        var engine = serviceProvider?.GetService<T>();
        if (engine is not null)
        {
            InitializeProviderEngine(
                engine,
                configuration,
                configureCborOptions,
                loggerFactory,
                sessionizer
            );
        }

        return engine;
    }
}
