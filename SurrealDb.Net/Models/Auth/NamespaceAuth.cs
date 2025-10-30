using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Models.Auth;

/// <summary>
/// Credentials for the namespace user
/// </summary>
public sealed class NamespaceAuth : SystemAuth
{
    /// <summary>
    /// The namespace the user has access to
    /// </summary>
    [CborProperty("ns")]
    public string Namespace { get; set; } = string.Empty;
}
