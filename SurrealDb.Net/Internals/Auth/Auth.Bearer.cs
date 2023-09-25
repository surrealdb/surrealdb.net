namespace SurrealDb.Net.Internals.Auth;

internal record BearerAuth(string Token) : IAuth;
