using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Models.Auth;

/// <summary>
/// The base class for System authentication, either Root, Namespace or Database level.
/// </summary>
public abstract class SystemAuth
{
    /// <summary>
    /// The username of the user
    /// </summary>
    [CborProperty("user")]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// The password of the user
    /// </summary>
    [CborProperty("pass")]
    public string Password { get; set; } = string.Empty;
}
