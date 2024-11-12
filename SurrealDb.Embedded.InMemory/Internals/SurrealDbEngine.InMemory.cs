using SurrealDb.Net.Internals;

namespace SurrealDb.Embedded.Internals;

internal sealed partial class SurrealDbEmbeddedEngine : ISurrealDbInMemoryEngine
{
    partial void PreConnect() { }
}
