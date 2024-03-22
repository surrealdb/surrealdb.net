using System.Buffers;
using System.Text.Json;
using Dahomey.Cbor;
using Dahomey.Cbor.Util;
#if NET8_0_OR_GREATER
using System.Text.Json.Serialization.Metadata;
#endif

namespace SurrealDb.Net.Internals.Ws;

internal class SurrealDbWsOkResponse : ISurrealDbWsStandardResponse
{
    private readonly JsonSerializerOptions? _jsonSerializerOptions;
    private readonly ReadOnlyMemory<byte>? _binaryResult;
    private readonly CborOptions? _cborOptions;

    public string Id { get; }
    public JsonElement? Result { get; }

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

    internal SurrealDbWsOkResponse(
        string id,
        ReadOnlyMemory<byte> binaryResult,
        CborOptions cborOptions
    )
    {
        Id = id;
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

    internal IEnumerable<T> DeserializeEnumerable<T>()
    {
        if (Result.HasValue && _jsonSerializerOptions is not null)
        {
            foreach (var element in Result.Value.EnumerateArray())
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
        else
        {
            var items = CborSerializer.Deserialize<IEnumerable<T>>(
                _binaryResult!.Value.Span,
                _cborOptions!
            );
            foreach (var item in items)
            {
                yield return item;
            }
        }
    }

    private static readonly byte[] _cborNone = [0xc6, 0xf6];
    private static readonly byte[] _cborEmptyArray = [0x80];

    internal bool ExpectNone()
    {
        return _binaryResult.HasValue && _binaryResult.Value.Span.SequenceEqual(_cborNone);
    }

    internal bool ExpectEmptyArray()
    {
        return _binaryResult.HasValue && _binaryResult.Value.Span.SequenceEqual(_cborEmptyArray);
    }
}
