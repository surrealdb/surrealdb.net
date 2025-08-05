using Dahomey.Cbor;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SurrealDb.Embedded.Internals;
using SurrealDb.Embedded.Options;
using SurrealDb.Net;

namespace SurrealDb.Embedded.RocksDb;

public class SurrealDbRocksDbClient : BaseSurrealDbClient
{
    internal const string BASE_ENDPOINT = "rocksdb://";

    /// <summary>
    /// Creates a new <see cref="SurrealDbRocksDbClient"/>.
    /// </summary>
    /// <param name="filePath">The path to the database file.</param>
    /// <param name="options">The configuration of the embedded engine.</param>
    /// <exception cref="ArgumentException"></exception>
    public SurrealDbRocksDbClient(string filePath, SurrealDbEmbeddedOptions? options = null)
        : this(new SurrealDbOptions($"{BASE_ENDPOINT}{filePath}"), options) { }

    /// <summary>
    /// Creates a new <see cref="SurrealDbRocksDbClient"/> using a specific configuration.
    /// </summary>
    /// <param name="configuration">The configuration options for the SurrealDbClient.</param>
    /// <param name="options">The configuration of the embedded engine.</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public SurrealDbRocksDbClient(
        SurrealDbOptions configuration,
        SurrealDbEmbeddedOptions? options = null
    )
        : this(configuration, options, null) { }

    internal SurrealDbRocksDbClient(
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

        var engine = new SurrealDbEmbeddedEngine(options);
        InitializeProviderEngine(engine, parameters, configureCborOptions, loggerFactory);

        _engine = engine;
    }
}
