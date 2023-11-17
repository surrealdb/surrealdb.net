using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace SurrealDb.Net.Internals.Ws;

internal class SurrealDbWsOkResponse : ISurrealDbWsStandardResponse
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    [JsonPropertyName("id")]
    public string Id { get; }

    [JsonPropertyName("result")]
    public JsonElement Result { get; }

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

#if NET8_0_OR_GREATER
    public T? GetValue<T>()
    {
        if (JsonSerializer.IsReflectionEnabledByDefault)
        {
#pragma warning disable IL2026, IL3050
            return Result.Deserialize<T>(_jsonSerializerOptions);
#pragma warning restore IL2026, IL3050
        }

        return Result.Deserialize(
            (_jsonSerializerOptions.GetTypeInfo(typeof(T)) as JsonTypeInfo<T>)!
        );
    }
#else
    public T? GetValue<T>() => Result.Deserialize<T>(_jsonSerializerOptions);
#endif

    internal IEnumerable<T> DeserializeEnumerable<T>()
    {
        foreach (var element in Result.EnumerateArray())
        {
#if NET8_0_OR_GREATER
            var item = JsonSerializer.IsReflectionEnabledByDefault
                ?
#pragma warning disable IL2026, IL3050
                element.Deserialize<T>(_jsonSerializerOptions)
#pragma warning restore IL2026, IL3050
                : element.Deserialize(
                    (_jsonSerializerOptions.GetTypeInfo(typeof(T)) as JsonTypeInfo<T>)!
                );
#else
            var item = element.Deserialize<T>(_jsonSerializerOptions);
#endif
            yield return item!;
        }
    }
}
