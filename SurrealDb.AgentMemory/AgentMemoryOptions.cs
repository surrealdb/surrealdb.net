using System.Text.Json;

namespace SurrealDb.AgentMemory;

/// <summary>
/// Options for the <c>AddAgentMemory</c> / <c>AddKeyedAgentMemory</c> registrations.
/// </summary>
public sealed class AgentMemoryOptions
{
    /// <summary>
    /// The Agent Memory endpoint origin, without trailing slash (e.g. <c>https://memory.surrealdb.com</c>).
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    /// <summary>
    /// The API key sent as an <c>Authorization: Bearer</c> token on every request.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Optional request timeout for the underlying <see cref="System.Net.Http.HttpClient"/>.
    /// </summary>
    public TimeSpan? RequestTimeout { get; set; }

    /// <summary>
    /// Optional JSON serializer options. When unset, the generated model converters are
    /// registered automatically (see <see cref="AgentMemoryJson.DefaultOptions"/>); supply
    /// your own only if you need additional converters.
    /// </summary>
    public JsonSerializerOptions? JsonSerializerOptions { get; set; }
}
