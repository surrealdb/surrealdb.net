using System.Text.Json;
using System.Text.Json.Serialization;
using Dahomey.Cbor;
#if NET8_0_OR_GREATER
using System.Text.Json.Serialization.Metadata;
#endif

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
    private readonly JsonSerializerOptions? _jsonSerializerOptions;
    private readonly ReadOnlyMemory<byte>? _binaryResult;
    private readonly CborOptions? _cborOptions;

    [JsonPropertyName("id")]
    public Guid Id { get; }

    [JsonPropertyName("action")]
    public string Action { get; }

    [JsonPropertyName("result")]
    public JsonElement? Result { get; }

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

    internal SurrealDbWsLiveResponseContent(
        Guid id,
        string action,
        ReadOnlyMemory<byte> binaryResult,
        CborOptions cborOptions
    )
    {
        Id = id;
        Action = action;
        _binaryResult = binaryResult;
        _cborOptions = cborOptions;
    }

    public T? GetValue<T>()
    {
        if (Result.HasValue && _jsonSerializerOptions is not null)
        {
#if NET8_0_OR_GREATER
            if (JsonSerializer.IsReflectionEnabledByDefault)
            {
#pragma warning disable IL2026, IL3050
                return Result.Value.Deserialize<T>(_jsonSerializerOptions);
#pragma warning restore IL2026, IL3050
            }

            return Result.Value.Deserialize(
                (_jsonSerializerOptions.GetTypeInfo(typeof(T)) as JsonTypeInfo<T>)!
            );
#else
            return Result.Value.Deserialize<T>(_jsonSerializerOptions);
#endif
        }

        return CborSerializer.Deserialize<T>(_binaryResult!.Value.Span, _cborOptions!);
    }
}
