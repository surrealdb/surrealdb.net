using Dahomey.Cbor;

namespace SurrealDb.Net.Models.Response;

/// <summary>
/// A SurrealDB ok result that can be returned from a query request.
/// </summary>
public sealed class SurrealDbOkResult : ISurrealDbResult
{
    private readonly ReadOnlyMemory<byte>? _binaryResult;
    private readonly CborOptions? _cborOptions;

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
    /// <exception cref="CborException">T is not compatible with the query result value.</exception>
    /// <exception cref="NotSupportedException">There is no compatible deserializer for T.</exception>
    public T? GetValue<T>()
    {
        return CborSerializer.Deserialize<T>(_binaryResult!.Value.Span, _cborOptions!);
    }

    /// <summary>
    /// Enumerates the result value of the query.
    /// </summary>
    /// <typeparam name="T">The type of the query result value.</typeparam>
    /// <exception cref="CborException">T is not compatible with the query result value.</exception>
    /// <exception cref="NotSupportedException">There is no compatible deserializer for T.</exception>
    public IEnumerable<T> GetValues<T>()
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
