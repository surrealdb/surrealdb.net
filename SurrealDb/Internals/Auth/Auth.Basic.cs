namespace SurrealDb.Internals.Auth;

internal record BasicAuth(string Username, string? Password) : IAuth;
