using Dahomey.Cbor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SurrealDb.Embedded.Internals;
using SurrealDb.Net;

namespace SurrealDb.Embedded.InMemory;

public class SurrealDbMemoryClient : BaseSurrealDbClient
{
    private const string ENDPOINT = "mem://";

    /// <summary>
    /// Creates a new <see cref="SurrealDbMemoryClient"/>.
    /// </summary>
    /// <param name="namingPolicy">The naming policy to use for serialization.</param>
    /// <exception cref="ArgumentException"></exception>
    public SurrealDbMemoryClient(string? namingPolicy = null)
        : this(new SurrealDbOptions(ENDPOINT, namingPolicy)) { }

    /// <summary>
    /// Creates a new <see cref="SurrealDbMemoryClient"/> using a specific configuration.
    /// </summary>
    /// <param name="configuration">The configuration options for the SurrealDbClient.</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public SurrealDbMemoryClient(SurrealDbOptions configuration)
        : this(configuration, null) { }

    internal SurrealDbMemoryClient(
        SurrealDbOptions parameters,
        Action<CborOptions>? configureCborOptions = null,
        ILoggerFactory? loggerFactory = null
    )
    {
        Uri = new Uri(ENDPOINT);
        NamingPolicy = parameters.NamingPolicy;

        var engine = new SurrealDbEmbeddedEngine();
        InitializeProviderEngine(engine, parameters, configureCborOptions, loggerFactory);

        _engine = engine;
    }
}
