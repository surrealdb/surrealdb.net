using Dahomey.Cbor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SurrealDb.Embedded.Internals;
using SurrealDb.Embedded.Options;
using SurrealDb.Net;

namespace SurrealDb.Embedded.SurrealKv;

public class SurrealDbKvClient : BaseSurrealDbClient
{
    internal const string BASE_ENDPOINT = "surrealkv://";

    /// <summary>
    /// Creates a new <see cref="SurrealDbKvClient"/>.
    /// </summary>
    /// <param name="filePath">The path to the database file.</param>
    /// <param name="options">The configuration of the embedded engine.</param>
    /// <param name="namingPolicy">The naming policy to use for serialization.</param>
    /// <exception cref="ArgumentException"></exception>
    public SurrealDbKvClient(
        string filePath,
        SurrealDbEmbeddedOptions? options = null,
        string? namingPolicy = null
    )
        : this(new SurrealDbOptions($"{BASE_ENDPOINT}{filePath}", namingPolicy), options) { }

    /// <summary>
    /// Creates a new <see cref="SurrealDbKvClient"/> using a specific configuration.
    /// </summary>
    /// <param name="configuration">The configuration options for the SurrealDbClient.</param>
    /// <param name="options">The configuration of the embedded engine.</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public SurrealDbKvClient(
        SurrealDbOptions configuration,
        SurrealDbEmbeddedOptions? options = null
    )
        : this(configuration, options, null) { }

    internal SurrealDbKvClient(
        SurrealDbOptions parameters,
        SurrealDbEmbeddedOptions? options = null,
        Action<CborOptions>? configureCborOptions = null,
        ILoggerFactory? loggerFactory = null
    )
    {
        if (string.IsNullOrWhiteSpace(parameters.Endpoint))
        {
            throw new ArgumentException("Endpoint is required", nameof(parameters));
        }

        Uri = new Uri(parameters.Endpoint);
        NamingPolicy = parameters.NamingPolicy;

        var engine = new SurrealDbEmbeddedEngine(options);
        InitializeProviderEngine(engine, parameters, configureCborOptions, loggerFactory);

        _engine = engine;
    }
}
