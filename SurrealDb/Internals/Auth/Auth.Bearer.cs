namespace SurrealDb.Internals.Auth;

internal record BearerAuth(string Token) : IAuth;
