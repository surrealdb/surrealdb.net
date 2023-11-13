using System.Text.Json;

namespace SurrealDb.Net.Models.Response;

/// <summary>
/// A SurrealDB ok result that can be returned from a query request.
/// </summary>
public sealed class SurrealDbOkResult : ISurrealDbResult
{
    private readonly JsonSerializerOptions _jsonSerializerOptions;
    private readonly JsonElement _value;

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
        _value = value;
        _jsonSerializerOptions = jsonSerializerOptions;
    }

    /// <summary>
    /// Gets the result value of the query.
    /// </summary>
    /// <typeparam name="T">The type of the query result value.</typeparam>
    public T? GetValue<T>() => JsonSerializer.Deserialize<T>(_value, _jsonSerializerOptions);

    internal IEnumerable<T> DeserializeEnumerable<T>()
    {
        foreach (var element in _value.EnumerateArray())
        {
            var item = JsonSerializer.Deserialize<T>(element, _jsonSerializerOptions);
            yield return item!;
        }
    }
}
