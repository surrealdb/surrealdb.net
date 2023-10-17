using Microsoft.Extensions.DependencyInjection;

namespace SurrealDb.Net.Internals.Models;

internal class SurrealDbClientParams
{
    public string? Endpoint { get; }
    public string? Ns { get; }
    public string? Db { get; }
    public string? Username { get; }
    public string? Password { get; }
    public string? Token { get; }

    public SurrealDbClientParams(string endpoint)
    {
        Endpoint = endpoint;
    }

    public SurrealDbClientParams(SurrealDbOptions configuration)
    {
        Endpoint = configuration.Endpoint;
        Ns = configuration.Namespace;
        Db = configuration.Database;
        Username = configuration.Username;
        Password = configuration.Password;
        Token = configuration.Token;
    }
}
