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
    /// <param name="filePath">
    /// The path to the database file, e.g. <c>data.db</c>.
    /// A full endpoint such as <c>surrealkv://data.db</c> is also accepted.
    /// </param>
    /// <param name="options">The configuration of the embedded engine.</param>
    /// <exception cref="ArgumentException"></exception>
    public SurrealDbKvClient(string filePath, SurrealDbEmbeddedOptions? options = null)
        : this(new SurrealDbOptions(ToEndpoint(filePath)), options) { }

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

        var engine = new SurrealDbEmbeddedEngine(options);
        InitializeAndSetProviderEngine(
            engine,
            parameters,
            configureCborOptions,
            loggerFactory,
            null
        );
    }

    /// <summary>
    /// Do not prefix the scheme twice: "surrealkv://surrealkv://data.db" is an invalid path on Windows
    /// (os error 123) and the wrong folder ("surrealkv:/data.db") on Linux and macOS.
    /// </summary>
    private static string ToEndpoint(string filePath) =>
        filePath?.StartsWith(BASE_ENDPOINT, StringComparison.OrdinalIgnoreCase) == true
            ? BASE_ENDPOINT + filePath[BASE_ENDPOINT.Length..]
            : BASE_ENDPOINT + filePath;
}
