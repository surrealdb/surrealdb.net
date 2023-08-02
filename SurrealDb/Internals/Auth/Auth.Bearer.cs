namespace SurrealDb.Internals.Auth;

internal class BearerAuth : IAuth
{
    public string Token { get; set; } = string.Empty;
}
