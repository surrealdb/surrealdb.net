using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Models.Auth;

/// <summary>
/// Credentials for the database user
/// </summary>
public sealed class DatabaseAuth : SystemAuth
{
    /// <summary>
    /// The namespace the user has access to
    /// </summary>
    [CborProperty("ns")]
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// The database the user has access to
    /// </summary>
    [CborProperty("db")]
    public string Database { get; set; } = string.Empty;
}
