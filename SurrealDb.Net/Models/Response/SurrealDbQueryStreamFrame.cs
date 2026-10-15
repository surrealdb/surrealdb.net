using Dahomey.Cbor;
using SurrealDb.Net.Exceptions;

namespace SurrealDb.Net.Models.Response;

/// <summary>
/// A part of the answer to a streaming query, see <see cref="ISurrealDbSharedMethods.RawQueryStream"/>.
/// </summary>
/// <remarks>
/// Frames arrive in this order: one <see cref="SurrealDbQueryStreamBeginFrame"/>,
/// then for each statement either <see cref="SurrealDbQueryStreamRowsFrame"/>s or one <see cref="SurrealDbQueryStreamValueFrame"/>,
/// followed by its <see cref="SurrealDbQueryStreamFinishedFrame"/>, and finally one <see cref="SurrealDbQueryStreamEndFrame"/>.
/// Frames of different statements can interleave.
/// Rows are provisional until their statement is finished: a statement that fails retracts the rows it already sent.
/// </remarks>
public abstract class SurrealDbQueryStreamFrame
{
    private protected SurrealDbQueryStreamFrame() { }
}

/// <summary>
/// The stream is open.
/// </summary>
public sealed class SurrealDbQueryStreamBeginFrame : SurrealDbQueryStreamFrame
{
    /// <summary>
    /// The most statements that can finish. Control flow can skip some of them,
    /// so count <see cref="SurrealDbQueryStreamFinishedFrame"/>s rather than relying on this.
    /// </summary>
    public int Statements { get; }

    internal SurrealDbQueryStreamBeginFrame(int statements)
    {
        Statements = statements;
    }
}

/// <summary>
/// Rows produced by a statement. More can follow for the same statement.
/// </summary>
public sealed class SurrealDbQueryStreamRowsFrame : SurrealDbQueryStreamFrame
{
    private readonly ReadOnlyMemory<byte> _values;
    private readonly CborOptions _cborOptions;

    /// <summary>
    /// The index of the statement, starting at 0.
    /// </summary>
    public int Index { get; }

    internal SurrealDbQueryStreamRowsFrame(
        int index,
        ReadOnlyMemory<byte> values,
        CborOptions cborOptions
    )
    {
        Index = index;
        _values = values;
        _cborOptions = cborOptions;
    }

    /// <summary>
    /// Deserializes the rows of this frame.
    /// </summary>
    /// <typeparam name="T">The type of a row.</typeparam>
    public IReadOnlyList<T> GetValues<T>()
    {
        return CborSerializer.Deserialize<List<T>>(_values.Span, _cborOptions) ?? [];
    }
}

/// <summary>
/// The single value produced by a statement that does not return a list of rows,
/// e.g. <c>RETURN 1 + 2</c> or <c>SELECT * FROM ONLY person:john</c>.
/// </summary>
public sealed class SurrealDbQueryStreamValueFrame : SurrealDbQueryStreamFrame
{
    private readonly ReadOnlyMemory<byte> _value;
    private readonly CborOptions _cborOptions;

    /// <summary>
    /// The index of the statement, starting at 0.
    /// </summary>
    public int Index { get; }

    internal SurrealDbQueryStreamValueFrame(
        int index,
        ReadOnlyMemory<byte> value,
        CborOptions cborOptions
    )
    {
        Index = index;
        _value = value;
        _cborOptions = cborOptions;
    }

    /// <summary>
    /// Deserializes the value of this frame.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    public T? GetValue<T>()
    {
        return CborSerializer.Deserialize<T>(_value.Span, _cborOptions);
    }
}

/// <summary>
/// A statement is finished. No other frame follows for this statement.
/// </summary>
public sealed class SurrealDbQueryStreamFinishedFrame : SurrealDbQueryStreamFrame
{
    /// <summary>
    /// The index of the statement, starting at 0.
    /// </summary>
    public int Index { get; }

    /// <summary>
    /// Time taken to execute the statement.
    /// </summary>
    public TimeSpan Time { get; }

    /// <summary>
    /// Type of the statement.
    /// </summary>
    public SurrealDbResponseType Type { get; }

    /// <summary>
    /// Whether the statement produced one value rather than a list of rows.
    /// </summary>
    /// <remarks>
    /// On connections that cannot stream, a statement whose single value is an array is reported as rows.
    /// </remarks>
    public bool Single { get; }

    /// <summary>
    /// The error of the statement if it failed, in which case the rows it sent must be discarded.
    /// </summary>
    public SurrealDbException? Error { get; }

    /// <summary>
    /// Whether the statement succeeded.
    /// </summary>
    public bool IsOk => Error is null;

    internal SurrealDbQueryStreamFinishedFrame(
        int index,
        TimeSpan time,
        SurrealDbResponseType type,
        bool single,
        SurrealDbException? error
    )
    {
        Index = index;
        Time = time;
        Type = type;
        Single = single;
        Error = error;
    }
}

/// <summary>
/// The stream is complete. Nothing follows.
/// </summary>
public sealed class SurrealDbQueryStreamEndFrame : SurrealDbQueryStreamFrame
{
    /// <summary>
    /// The number of statements that finished.
    /// </summary>
    public int Results { get; }

    /// <summary>
    /// Time taken to execute the whole query.
    /// </summary>
    public TimeSpan Time { get; }

    /// <summary>
    /// An error that belongs to no single statement, e.g. a timeout or a cancellation.
    /// The statements that did not finish are retracted.
    /// </summary>
    public SurrealDbException? Error { get; }

    /// <summary>
    /// Whether the query ran to completion.
    /// </summary>
    public bool IsOk => Error is null;

    internal SurrealDbQueryStreamEndFrame(int results, TimeSpan time, SurrealDbException? error)
    {
        Results = results;
        Time = time;
        Error = error;
    }
}
