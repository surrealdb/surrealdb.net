using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Models.Auth;

/// <summary>
/// Authentication token pair
/// </summary>
public sealed class Tokens
{
    /// <summary>
    /// The access token
    /// </summary>
    [CborProperty("access")]
    public string Access { get; internal set; } = string.Empty;

    /// <summary>
    /// The refresh token, if requested
    /// </summary>
    [CborProperty("refresh")]
    public string? Refresh { get; internal set; }

    internal Tokens() { }

    internal Tokens(string jwt)
    {
        Access = jwt;
    }
}
