using System.Text.Json;
using System.Text.Json.Serialization;
using SurrealDb.Net.Internals.Json;

namespace SurrealDb.Net.Internals.Ws;

internal class SurrealDbWsLiveResponse : ISurrealDbWsLiveResponse
{
    [JsonPropertyName("result")]
    public SurrealDbWsLiveResponseContent Result { get; set; } = new();
}

internal class SurrealDbWsLiveResponseContent
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; }

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public JsonElement Result { get; set; }

    public T? GetValue<T>() => Result.Deserialize<T>(SurrealDbSerializerOptions.Default);
}
