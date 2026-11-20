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

    /// <summary>
    /// Creates a token pair, e.g. to authenticate with tokens stored from a previous sign in.
    /// </summary>
    /// <param name="access">The access token.</param>
    /// <param name="refresh">The refresh token, if any.</param>
    public Tokens(string access, string? refresh = null)
    {
        Access = access;
        Refresh = refresh;
    }
}
