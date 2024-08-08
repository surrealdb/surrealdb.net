namespace SurrealDb.Embedded.InMemory.Internals;

internal class SurrealDbEmbeddedEngineConfig
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
        Ns = null;
        Db = null;
    }
}
