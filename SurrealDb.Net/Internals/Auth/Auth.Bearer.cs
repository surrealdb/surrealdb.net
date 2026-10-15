namespace SurrealDb.Net.Internals.Auth;

/// <param name="Token">The access token.</param>
/// <param name="Refresh">The refresh token used to renew the access token, if any.</param>
internal record BearerAuth(string Token, string? Refresh = null) : IAuth;
