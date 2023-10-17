using SurrealDb.Net.Internals.Auth;

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

    public void Reset()
    {
        Ns = null;
        Db = null;
        Auth = new NoAuth();
    }
}
