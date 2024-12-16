using SurrealDb.Net;
using SurrealDb.Net.Extensions.DependencyInjection;
using SurrealDb.Net.Internals.Constants;

namespace Microsoft.Extensions.DependencyInjection;

public sealed class SurrealDbOptions
{
    /// <summary>
    /// Endpoint of the SurrealDB instance.<br /><br />
    /// Examples:<br />
    /// - http://127.0.0.1:8000<br />
    /// - wss://cloud.surrealdb.com
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// Default namespace to use when new <see cref="ISurrealDbClient"/> is generated.
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// Default database to use when new <see cref="ISurrealDbClient"/> is generated.
    /// </summary>
    public string? Database { get; set; }

    /// <summary>
    /// Default username (Root auth) to use when new <see cref="ISurrealDbClient"/> is generated.
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Default password (Root auth) to use when new <see cref="ISurrealDbClient"/> is generated.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>
    /// Default token (User auth) to use when new <see cref="ISurrealDbClient"/> is generated.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Naming policy used to interact with the database.
    /// It will change the default NamingPolicy of the <see cref="ISurrealDbClient"/> used.
    /// Valid options are "CamelCase", "SnakeCaseLower", "SnakeCaseUpper", "KebabCaseLower" and "KebabCaseUpper".
    /// </summary>
    public string? NamingPolicy { get; set; }

    /// <summary>
    /// Indicates if the options are made to use a SurrealDB instance in embedded mode.
    /// Supported embedded modes are <c>mem://</c>, <c>rocksdb://</c> and <c>surrealkv://</c>.
    /// </summary>
    public bool IsEmbedded =>
        Endpoint!.StartsWith(EndpointConstants.Client.MEMORY)
        || Endpoint.StartsWith(EndpointConstants.Client.ROCKSDB)
        || Endpoint.StartsWith(EndpointConstants.Client.SURREALKV);

    /// <summary>
    /// Logging options used for the SurrealDB client.
    /// </summary>
    public SurrealDbLoggingOptions Logging { get; set; } = new();

    public SurrealDbOptions() { }

    public SurrealDbOptions(SurrealDbOptions clone)
    {
        Endpoint = clone.Endpoint;
        Namespace = clone.Namespace;
        Database = clone.Database;
        Username = clone.Username;
        Password = clone.Password;
        Token = clone.Token;
        NamingPolicy = clone.NamingPolicy;
        Logging = clone.Logging;
    }

    public SurrealDbOptions(string endpoint, string? namingPolicy = null)
    {
        Endpoint = endpoint;
        NamingPolicy = namingPolicy;
    }

    public static SurrealDbOptionsBuilder Create()
    {
        return new SurrealDbOptionsBuilder();
    }
}
