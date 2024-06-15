using SurrealDb.Net.Internals.Auth;
using SurrealDb.Net.Internals.Models;

namespace SurrealDb.Net.Internals.Ws;

internal class SurrealDbWsEngineConfig
{
    public IAuth Auth { get; private set; } = new NoAuth();
    public string? Ns { get; private set; }
    public string? Db { get; private set; }

    public void Use(string ns, string? db)
    {
        Ns = ns;
        Db = db;
    }

    public void SetBasicAuth(string username, string? password)
    {
        Auth = new BasicAuth(username, password);
    }

    public void SetBearerAuth(string token)
    {
        Auth = new BearerAuth(token);
    }

    public void ResetAuth()
    {
        Auth = new NoAuth();
    }

    public void Reset(SurrealDbClientParams @params)
    {
        Ns = @params.Ns;
        Db = @params.Db;

        if (@params.Username is not null)
        {
            SetBasicAuth(@params.Username, @params.Password);
        }
        else if (@params.Token is not null)
        {
            SetBearerAuth(@params.Token);
        }
        else
        {
            ResetAuth();
        }
    }
}
