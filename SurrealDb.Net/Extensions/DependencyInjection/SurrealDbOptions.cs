using System.Text.Json;
using SurrealDb.Net;

namespace Microsoft.Extensions.DependencyInjection;

public class SurrealDbOptions
{
    /// <summary>
    /// Endpoint of the SurrealDB instance.<br /><br />
    /// Examples:<br />
    /// - http://127.0.0.1:8000<br />
    /// - wss://cloud.surrealdb.com
    /// </summary>
    public string? Endpoint { get; internal set; }

    /// <summary>
    /// Default namespace to use when new <see cref="ISurrealDbClient"/> is generated.
    /// </summary>
    public string? Namespace { get; internal set; }

    /// <summary>
    /// Default database to use when new <see cref="ISurrealDbClient"/> is generated.
    /// </summary>
    public string? Database { get; internal set; }

    /// <summary>
    /// Default username (Root auth) to use when new <see cref="ISurrealDbClient"/> is generated.
    /// </summary>
    public string? Username { get; internal set; }

    /// <summary>
    /// Default password (Root auth) to use when new <see cref="ISurrealDbClient"/> is generated.
    /// </summary>
    public string? Password { get; internal set; }

    /// <summary>
    /// Default token (User auth) to use when new <see cref="ISurrealDbClient"/> is generated.
    /// </summary>
    public string? Token { get; internal set; }

    /// <summary>
    /// Naming policy used to interact with the database.
    /// It will change the default <see cref="JsonNamingPolicy"/> of the <see cref="ISurrealDbClient"/> used.
    /// Valid options are "CamelCase", "SnakeCaseLower", "SnakeCaseUpper", "KebabCaseLower" and "KebabCaseUpper".
    /// </summary>
    public string? NamingPolicy { get; internal set; }

    /// <summary>
    /// Default serialization method to use when new <see cref="ISurrealDbClient"/> is generated.
    /// Either JSON or CBOR. Defaults to JSON.
    /// </summary>
    public string? Serialization { get; internal set; }

    public static SurrealDbOptionsBuilder Create()
    {
        return new SurrealDbOptionsBuilder();
    }
}
