using System.Text.Json;

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
    public JsonElement Value { get; } 

    /// <summary>
    /// Time taken to execute the query.
    /// </summary>
    public TimeSpan Time { get; set; }

    /// <summary>
    /// Status of the query ("OK").
    /// </summary>
    public string Status { get; set; }

    public bool IsOk => true;

    internal SurrealDbOkResult(
        TimeSpan time,
        string status,
        JsonElement value,
        JsonSerializerOptions jsonSerializerOptions
    )
    {
        Time = time;
        Status = status;
        Value = value;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    /// <summary>
    /// Gets the result value of the query.
    /// </summary>
    /// <typeparam name="T">The type of the query result value.</typeparam>
    /// <exception cref="JsonException">T is not compatible with the query result value.</exception>
    /// <exception cref="NotSupportedException">There is no compatible deserializer for T.</exception>
    public T? GetValue<T>() => Value.Deserialize<T>(_jsonSerializerOptions);

    /// <summary>
    /// Enumerates the result value of the query.
    /// </summary>
    /// <typeparam name="T">The type of the query result value.</typeparam>
    /// <exception cref="JsonException">T is not compatible with the query result value.</exception>
    /// <exception cref="NotSupportedException">There is no compatible deserializer for T.</exception>
    public IEnumerable<T> GetValues<T>() => DeserializeEnumerable<T>();

    internal IEnumerable<T> DeserializeEnumerable<T>()
    {
        if (Value.ValueKind is not JsonValueKind.Array)
            throw new NotSupportedException(
                "The query result value is not an array. " +
                "This can happen if you have used the 'ONLY' keyword in your query.");

        foreach (var element in Value.EnumerateArray())
        {
            var item = element.Deserialize<T>(_jsonSerializerOptions);
            yield return item!;
        }
    }
}
