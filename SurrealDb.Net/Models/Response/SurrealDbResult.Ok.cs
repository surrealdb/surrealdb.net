using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace SurrealDb.Net.Models.Response;

/// <summary>
/// A SurrealDB ok result that can be returned from a query request.
/// </summary>
public sealed class SurrealDbOkResult : ISurrealDbResult
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    /// <summary>
    /// The result value of the query.
    /// </summary>
    public JsonElement RawValue { get; }

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

    /// <summary>
    /// Gets the result value of the query.
    /// </summary>
    /// <typeparam name="T">The type of the query result value.</typeparam>
    /// <exception cref="JsonException">T is not compatible with the query result value.</exception>
    /// <exception cref="NotSupportedException">There is no compatible deserializer for T.</exception>
#if NET8_0_OR_GREATER
    public T? GetValue<T>()
    {
        if (JsonSerializer.IsReflectionEnabledByDefault)
        {
#pragma warning disable IL2026, IL3050
            return RawValue.Deserialize<T>(_jsonSerializerOptions);
#pragma warning restore IL2026, IL3050
        }

        return RawValue.Deserialize(
            (_jsonSerializerOptions.GetTypeInfo(typeof(T)) as JsonTypeInfo<T>)!
        );
    }
#else
    public T? GetValue<T>() => RawValue.Deserialize<T>(_jsonSerializerOptions);
#endif

    /// <summary>
    /// Enumerates the result value of the query.
    /// </summary>
    /// <typeparam name="T">The type of the query result value.</typeparam>
    /// <exception cref="JsonException">T is not compatible with the query result value.</exception>
    /// <exception cref="NotSupportedException">There is no compatible deserializer for T.</exception>
    public IEnumerable<T> GetValues<T>()
    {
        if (RawValue.ValueKind is not JsonValueKind.Array)
        {
            throw new NotSupportedException(
                "The query result value is not an array. "
                    + "This can happen if you have used the 'ONLY' keyword in your query."
            );
        }

        foreach (var element in RawValue.EnumerateArray())
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
