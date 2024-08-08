using Microsoft.IO;

namespace SurrealDb.Net.Internals.Stream;

internal static class MemoryStreamProvider
{
    public static readonly RecyclableMemoryStreamManager MemoryStreamManager = new();
}
