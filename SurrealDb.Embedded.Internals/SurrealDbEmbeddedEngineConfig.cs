using Microsoft.Extensions.DependencyInjection;

namespace SurrealDb.Embedded.Internals;

internal sealed class SurrealDbEmbeddedEngineConfig
{
    public string? Ns { get; private set; }
    public string? Db { get; private set; }

    public SurrealDbEmbeddedEngineConfig() { }

    public SurrealDbEmbeddedEngineConfig(SurrealDbOptions options)
    {
        Reset(options);
    }

    private void Reset(SurrealDbOptions options)
    {
        Ns = options.Namespace;
        Db = options.Database;
    }
}
