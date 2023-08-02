namespace SurrealDb.Internals.Auth;

internal class BasicAuth : IAuth
{
    public string Username { get; set; } = string.Empty;
    public string? Password { get; set; }
}
