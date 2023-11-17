using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace SurrealDb.Net.Internals.Ws;

internal class SurrealDbWsLiveResponse : ISurrealDbWsLiveResponse
{
    [JsonPropertyName("result")]
    public SurrealDbWsLiveResponseContent Result { get; }

    internal SurrealDbWsLiveResponse(SurrealDbWsLiveResponseContent result)
    {
        Result = result;
    }
}

internal class SurrealDbWsLiveResponseContent
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    [JsonPropertyName("id")]
    public Guid Id { get; }

    [JsonPropertyName("action")]
    public string Action { get; }

    [JsonPropertyName("result")]
    public JsonElement Result { get; }

    internal SurrealDbWsLiveResponseContent(
        Guid id,
        string action,
        JsonElement result,
        JsonSerializerOptions jsonSerializerOptions
    )
    {
        Id = id;
        Action = action;
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
}
