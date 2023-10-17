using SurrealDb.Net.Internals.Json;
using System.Text.Json;

namespace SurrealDb.Net.Models.Response;

/// <summary>
/// A SurrealDB ok result that can be returned from a query request.
/// </summary>
public sealed class SurrealDbOkResult : ISurrealDbResult
{
    private JsonElement _value;

    /// <summary>
    /// Time taken to execute the query.
    /// </summary>
    public TimeSpan Time { get; set; }

    /// <summary>
    /// Status of the query ("OK").
    /// </summary>
    public string Status { get; set; }

    /// <summary>
    /// Gets the result value of the query.
    /// </summary>
    /// <typeparam name="T">The type of the query result value.</typeparam>
    public T? GetValue<T>() =>
        JsonSerializer.Deserialize<T>(_value, SurrealDbSerializerOptions.Default);

    public bool IsOk => true;

    internal SurrealDbOkResult(TimeSpan time, string status, JsonElement value)
    {
        Time = time;
        Status = status;
        _value = value;
    }
}
