using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Models.Auth;

/// <summary>
/// Credentials for the scope user
/// </summary>
public class ScopeAuth
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

    /// <summary>
    /// The scope to use for signin and signup
    /// </summary>
    [CborProperty("sc")]
    [Obsolete(
        $"Only needed for V1 backward compatiblity. Use the '{nameof(Access)}' property for V2."
    )]
    public string Scope { get; set; } = string.Empty;

    /// <summary>
    /// The access method to use for signin and signup
    /// </summary>
    [CborProperty("ac")]
    public string Access { get; set; } = string.Empty;
}
