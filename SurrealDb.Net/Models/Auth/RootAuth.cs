using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Models.Auth;

/// <summary>
/// Credentials for the root user
/// </summary>
public sealed class RootAuth
{
    /// <summary>
    /// The username of the root user
    /// </summary>
    [CborProperty("user")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The password of the root user
    /// </summary>
    [CborProperty("pass")]
    public string Password { get; set; } = string.Empty;
}
