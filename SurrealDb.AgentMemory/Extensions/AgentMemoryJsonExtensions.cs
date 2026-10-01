using System.Text.Json;
using SurrealDb.AgentMemory.Client;

namespace SurrealDb.AgentMemory;

/// <summary>
/// Default <see cref="JsonSerializerOptions"/> backed by the source-generated <see cref="AgentMemoryJsonContext"/>:
/// every converter the OpenAPI generator emitted (<c>XxxJsonConverter</c> in the <c>Model</c> and <c>Client</c>
/// namespaces) is registered explicitly, and every model resolves through source-generated metadata instead of
/// reflection — trim- and NativeAOT-safe. The generator does not attach these converters via attributes, so
/// without them several models (e.g. <c>CitationJson</c>) cannot be deserialized by System.Text.Json.
/// </summary>
public static class AgentMemoryJsonExtensions
{
    /// <summary>
    /// Shared default options.
    /// </summary>
    public static JsonSerializerOptions DefaultOptions => AgentMemoryJsonContext.Default.Options;
}
