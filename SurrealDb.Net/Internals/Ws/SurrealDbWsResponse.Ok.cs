using SurrealDb.Net.Internals.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SurrealDb.Net.Internals.Ws;

internal class SurrealDbWsOkResponse : ISurrealDbWsResponse
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public JsonElement Result { get; set; }

    public T? GetValue<T>() => Result.Deserialize<T>(SurrealDbSerializerOptions.Default);
}
