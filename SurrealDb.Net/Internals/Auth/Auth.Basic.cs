namespace SurrealDb.Net.Internals.Auth;

internal record BasicAuth(string Username, string? Password) : IAuth;
