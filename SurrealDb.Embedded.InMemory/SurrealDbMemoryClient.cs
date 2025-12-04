using Dahomey.Cbor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SurrealDb.Embedded.Internals;
using SurrealDb.Embedded.Options;
using SurrealDb.Net;

namespace SurrealDb.Embedded.InMemory;

public class SurrealDbMemoryClient : BaseSurrealDbClient
{
    private const string ENDPOINT = "mem://";

    /// <summary>
    /// Creates a new <see cref="SurrealDbMemoryClient"/>.
    /// </summary>
    /// <param name="options">The configuration of the embedded engine.</param>
    /// <exception cref="ArgumentException"></exception>
    public SurrealDbMemoryClient(SurrealDbEmbeddedOptions? options = null)
        : this(new SurrealDbOptions(ENDPOINT), options) { }

    /// <summary>
    /// Creates a new <see cref="SurrealDbMemoryClient"/> using a specific configuration.
    /// </summary>
    /// <param name="configuration">The configuration options for the SurrealDbClient.</param>
    /// <param name="options">The configuration of the embedded engine.</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public SurrealDbMemoryClient(
        SurrealDbOptions configuration,
        SurrealDbEmbeddedOptions? options = null
    )
        : this(configuration, options, null) { }

    internal SurrealDbMemoryClient(
        SurrealDbOptions parameters,
        SurrealDbEmbeddedOptions? options = null,
        Action<CborOptions>? configureCborOptions = null,
        ILoggerFactory? loggerFactory = null
    )
    {
        Uri = new Uri(ENDPOINT);

        var engine = new SurrealDbEmbeddedEngine(options);
        InitializeProviderEngine(engine, parameters, configureCborOptions, loggerFactory);

        _engine = engine;
    }
}
