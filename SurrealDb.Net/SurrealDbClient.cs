using System.Text.Json;
using System.Text.Json.Serialization;
using Dahomey.Cbor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SurrealDb.Net.Extensions.DependencyInjection;
using SurrealDb.Net.Internals;

namespace SurrealDb.Net;

/// <summary>
/// The entry point to communicate with a SurrealDB instance.
/// Authenticate, use namespace/database, execute queries, etc...
/// </summary>
public class SurrealDbClient : BaseSurrealDbClient, ISurrealDbClient
{
    /// <summary>
    /// Creates a new SurrealDbClient, with the defined endpoint.
    /// </summary>
    /// <param name="endpoint">The endpoint to access a SurrealDB instance.</param>
    /// <param name="namingPolicy">The naming policy to use for serialization.</param>
    /// <param name="httpClientFactory">An IHttpClientFactory instance, or none.</param>
    /// <param name="configureJsonSerializerOptions">An optional action to configure <see cref="JsonSerializerOptions"/>.</param>
    /// <param name="prependJsonSerializerContexts">
    /// An option function to retrieve the <see cref="JsonSerializerContext"/> to use and prepend to the current list of contexts,
    /// in AoT mode.
    /// </param>
    /// <param name="appendJsonSerializerContexts">
    /// An option function to retrieve the <see cref="JsonSerializerContext"/> to use and append to the current list of contexts,
    /// in AoT mode.
    /// </param>
    /// <param name="configureCborOptions">An optional action to configure <see cref="CborOptions"/>.</param>
    /// <param name="loggerFactory">
    /// An instance of <see cref="ILoggerFactory"/> used to log messages.
    /// </param>
    /// <exception cref="ArgumentException"></exception>
    public SurrealDbClient(
        string endpoint,
        string? namingPolicy = null,
        IHttpClientFactory? httpClientFactory = null,
        Action<JsonSerializerOptions>? configureJsonSerializerOptions = null,
        Func<JsonSerializerContext[]>? prependJsonSerializerContexts = null,
        Func<JsonSerializerContext[]>? appendJsonSerializerContexts = null,
        Action<CborOptions>? configureCborOptions = null,
        ILoggerFactory? loggerFactory = null
    )
        : this(
            new SurrealDbOptions(endpoint, namingPolicy),
            httpClientFactory,
            configureJsonSerializerOptions,
            prependJsonSerializerContexts,
            appendJsonSerializerContexts,
            configureCborOptions
        ) { }

    /// <summary>
    /// Creates a new SurrealDbClient using a specific configuration.
    /// </summary>
    /// <param name="configuration">The configuration options for the SurrealDbClient.</param>
    /// <param name="httpClientFactory">An IHttpClientFactory instance, or none.</param>
    /// <param name="configureJsonSerializerOptions">An optional action to configure <see cref="JsonSerializerOptions"/>.</param>
    /// <param name="prependJsonSerializerContexts">
    /// An option function to retrieve the <see cref="JsonSerializerContext"/> to use and prepend to the current list of contexts,
    /// in AoT mode.
    /// </param>
    /// <param name="appendJsonSerializerContexts">
    /// An option function to retrieve the <see cref="JsonSerializerContext"/> to use and append to the current list of contexts,
    /// in AoT mode.
    /// </param>
    /// <param name="configureCborOptions">An optional action to configure <see cref="CborOptions"/>.</param>
    /// <param name="loggerFactory">
    /// An instance of <see cref="ILoggerFactory"/> used to log messages.
    /// </param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public SurrealDbClient(
        SurrealDbOptions configuration,
        IHttpClientFactory? httpClientFactory = null,
        Action<JsonSerializerOptions>? configureJsonSerializerOptions = null,
        Func<JsonSerializerContext[]>? prependJsonSerializerContexts = null,
        Func<JsonSerializerContext[]>? appendJsonSerializerContexts = null,
        Action<CborOptions>? configureCborOptions = null,
        ILoggerFactory? loggerFactory = null
    )
        : this(
            configuration,
            null,
            httpClientFactory,
            configureJsonSerializerOptions,
            prependJsonSerializerContexts,
            appendJsonSerializerContexts,
            configureCborOptions,
            loggerFactory
        ) { }

    internal SurrealDbClient(
        SurrealDbOptions configuration,
        IServiceProvider? serviceProvider,
        IHttpClientFactory? httpClientFactory = null,
        Action<JsonSerializerOptions>? configureJsonSerializerOptions = null,
        Func<JsonSerializerContext[]>? prependJsonSerializerContexts = null,
        Func<JsonSerializerContext[]>? appendJsonSerializerContexts = null,
        Action<CborOptions>? configureCborOptions = null,
        ILoggerFactory? loggerFactory = null
    )
    {
        if (configuration.Endpoint is null)
            throw new ArgumentNullException(nameof(configuration), "The endpoint is required.");

        Uri = new Uri(configuration.Endpoint);
        NamingPolicy = configuration.NamingPolicy;

        var protocol = Uri.Scheme;

        _engine = protocol switch
        {
            "http"
            or "https"
                => new SurrealDbHttpEngine(
                    configuration,
                    httpClientFactory,
                    configureJsonSerializerOptions,
                    prependJsonSerializerContexts,
                    appendJsonSerializerContexts,
                    configureCborOptions,
                    loggerFactory is not null ? new SurrealDbLoggerFactory(loggerFactory) : null
                ),
            "ws"
            or "wss"
                => new SurrealDbWsEngine(
                    configuration,
                    configureJsonSerializerOptions,
                    prependJsonSerializerContexts,
                    appendJsonSerializerContexts,
                    configureCborOptions,
                    loggerFactory is not null ? new SurrealDbLoggerFactory(loggerFactory) : null
                ),
            "mem"
                => ResolveInMemoryProvider(
                    serviceProvider,
                    configuration,
                    configureCborOptions,
                    loggerFactory
                )
                    ?? throw new Exception(
                        "Impossible to create a new in-memory SurrealDB client. Make sure to use `AddInMemoryProvider`."
                    ),
            _ => throw new NotSupportedException($"The protocol '{protocol}' is not supported."),
        };

        if (configuration.Username is not null)
            Configure(
                configuration.Namespace,
                configuration.Database,
                configuration.Username,
                configuration.Password
            );
        else
            Configure(configuration.Namespace, configuration.Database, configuration.Token);
    }

    private ISurrealDbInMemoryEngine? ResolveInMemoryProvider(
        IServiceProvider? serviceProvider,
        SurrealDbOptions configuration,
        Action<CborOptions>? configureCborOptions,
        ILoggerFactory? loggerFactory
    )
    {
        var engine = serviceProvider?.GetService<ISurrealDbInMemoryEngine>();
        if (engine is not null)
        {
            InitializeProviderEngine(engine, configuration, configureCborOptions, loggerFactory);
        }

        return engine;
    }
}
