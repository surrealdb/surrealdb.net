using System.Text.Json.Serialization;
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
    [JsonPropertyName("ns")]
    [CborProperty("ns")]
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// The database the user has access to
    /// </summary>
    [JsonPropertyName("db")]
    [CborProperty("db")]
    public string Database { get; set; } = string.Empty;

    /// <summary>
    /// The scope to use for signin and signup
    /// </summary>
    [JsonPropertyName("sc")]
    [CborProperty("sc")]
    public string Scope { get; set; } = string.Empty;
}
