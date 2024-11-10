namespace SurrealDb.Embedded.Internals;

internal sealed class SurrealDbEmbeddedEngineConfig
{
    public string? Ns { get; private set; }
    public string? Db { get; private set; }

    public void Use(string ns, string? db)
    {
        Ns = ns;
        Db = db;
    }

    public void Reset()
    {
        Db = null;
    }
}
