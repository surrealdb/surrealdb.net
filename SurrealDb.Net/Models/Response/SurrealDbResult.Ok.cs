using System.Buffers;
using System.Text.Json;
using Dahomey.Cbor;
#if NET8_0_OR_GREATER
using System.Text.Json.Serialization.Metadata;
#endif

namespace SurrealDb.Net.Models.Response;

/// <summary>
/// A SurrealDB ok result that can be returned from a query request.
/// </summary>
public sealed class SurrealDbOkResult : ISurrealDbResult
{
    private readonly JsonSerializerOptions? _jsonSerializerOptions;
    private readonly ReadOnlyMemory<byte>? _binaryResult;
    private readonly CborOptions? _cborOptions;

    /// <summary>
    /// The result value of the query.
    /// </summary>
    public JsonElement? RawValue { get; }

    /// <summary>
    /// Time taken to execute the query.
    /// </summary>
    public TimeSpan Time { get; }

    /// <summary>
    /// Status of the query ("OK").
    /// </summary>
    public string Status { get; }

    public bool IsOk => true;

    internal SurrealDbOkResult(
        TimeSpan time,
        string status,
        JsonElement rawValue,
        JsonSerializerOptions jsonSerializerOptions
    )
    {
        Time = time;
        Status = status;
        RawValue = rawValue;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    internal SurrealDbOkResult(
        TimeSpan time,
        string status,
        ReadOnlyMemory<byte> binaryResult,
        CborOptions cborOptions
    )
    {
        Time = time;
        Status = status;
        _binaryResult = binaryResult;
        _cborOptions = cborOptions;
    }

    /// <summary>
    /// Gets the result value of the query.
    /// </summary>
    /// <typeparam name="T">The type of the query result value.</typeparam>
    /// <exception cref="JsonException">T is not compatible with the query result value.</exception>
    /// <exception cref="NotSupportedException">There is no compatible deserializer for T.</exception>
    public T? GetValue<T>()
    {
        if (RawValue.HasValue && _jsonSerializerOptions is not null)
        {
#if NET8_0_OR_GREATER
            if (JsonSerializer.IsReflectionEnabledByDefault)
            {
#pragma warning disable IL2026, IL3050
                return RawValue.Value.Deserialize<T>(_jsonSerializerOptions);
#pragma warning restore IL2026, IL3050
            }

            return RawValue.Value.Deserialize(
                (_jsonSerializerOptions.GetTypeInfo(typeof(T)) as JsonTypeInfo<T>)!
            );
#else
            return RawValue.Value.Deserialize<T>(_jsonSerializerOptions);
#endif
        }

        return CborSerializer.Deserialize<T>(_binaryResult!.Value.Span, _cborOptions!);
    }

    /// <summary>
    /// Enumerates the result value of the query.
    /// </summary>
    /// <typeparam name="T">The type of the query result value.</typeparam>
    /// <exception cref="JsonException">T is not compatible with the query result value.</exception>
    /// <exception cref="NotSupportedException">There is no compatible deserializer for T.</exception>
    public IEnumerable<T> GetValues<T>()
    {
        if (RawValue.HasValue && _jsonSerializerOptions is not null)
        {
            if (RawValue.Value.ValueKind is not JsonValueKind.Array)
            {
                throw new NotSupportedException(
                    "The query result value is not an array. "
                        + "This can happen if you have used the 'ONLY' keyword in your query."
                );
            }

            foreach (var element in RawValue.Value.EnumerateArray())
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
}
