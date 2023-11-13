using System.Text.Json;
using System.Text.Json.Serialization;

namespace SurrealDb.Net.Internals.Ws;

internal class SurrealDbWsOkResponse : ISurrealDbWsStandardResponse
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public JsonElement Result { get; set; }

    internal SurrealDbWsOkResponse(
        string id,
        JsonElement result,
        JsonSerializerOptions jsonSerializerOptions
    )
    {
        Id = id;
        Result = result;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    public T? GetValue<T>() => Result.Deserialize<T>(_jsonSerializerOptions);

    internal IEnumerable<T> DeserializeEnumerable<T>()
    {
        foreach (var element in Result.EnumerateArray())
        {
            var item = JsonSerializer.Deserialize<T>(element, _jsonSerializerOptions);
            yield return item!;
        }
    }
}
