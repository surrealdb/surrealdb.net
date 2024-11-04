using System.Collections;
using SurrealDb.Net.Exceptions;

namespace SurrealDb.Net.Models.Response;

/// <summary>
/// The response type of a query request.
/// </summary>
public sealed class SurrealDbResponse : IReadOnlyList<ISurrealDbResult>
{
    private readonly List<ISurrealDbResult> _results;

    public ISurrealDbResult this[int index] => _results[index];
    public int Count => _results.Count;

    /// <summary>
    /// Gets all OK results from the query response.
    /// </summary>
    public IEnumerable<SurrealDbOkResult> Oks => _results.OfType<SurrealDbOkResult>();

    /// <summary>
    /// Gets all errors from the query response.
    /// </summary>
    public IEnumerable<ISurrealDbErrorResult> Errors => _results.OfType<ISurrealDbErrorResult>();

    /// <summary>
    /// Gets the first result from the response, null otherwise.
    /// </summary>
    public ISurrealDbResult? FirstResult => _results.FirstOrDefault();

    /// <summary>
    /// Gets the first OK result from the response, null otherwise.
    /// </summary>
    public SurrealDbOkResult? FirstOk => Oks.FirstOrDefault();

    /// <summary>
    /// Gets the first error from the response, null otherwise.
    /// </summary>
    public ISurrealDbErrorResult? FirstError => Errors.FirstOrDefault();

    /// <summary>
    /// Checks if the response contains any errors.
    /// </summary>
    /// <returns></returns>
    public bool HasErrors => Errors.Any();

    /// <summary>
    /// Checks if the response contains any result.
    /// </summary>
    public bool IsEmpty => _results.Count == 0;

    /// <summary>
    /// Checks if the response contains a single result.
    /// </summary>
    public bool IsSingle => _results.Count == 1;

    public SurrealDbResponse(List<ISurrealDbResult> results)
    {
        _results = results;
    }

    public IEnumerator<ISurrealDbResult> GetEnumerator()
    {
        return _results.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _results.GetEnumerator();
    }

    /// <summary>
    /// Throws an exception if at least one error is found in the list of <see cref="ISurrealDbResult"/>.
    /// </summary>
    /// <exception cref="SurrealDbException">The SurrealDbResponse is unsuccessful.</exception>
    public SurrealDbResponse EnsureAllOks()
    {
        if (HasErrors)
        {
            throw new SurrealDbException($"The {nameof(SurrealDbResponse)} is unsuccessful.");
        }

        return this;
    }

    /// <summary>
    /// Gets the result value of the query based on its index.
    /// </summary>
    /// <typeparam name="T">The type of the query result value.</typeparam>
    /// <param name="index">The index of the result in the query response.</param>
    /// <exception cref="IndexOutOfRangeException">Index is out of range.</exception>
    /// <exception cref="NotSupportedException">The result is not an OK result.</exception>
    public T? GetValue<T>(int index)
    {
        if (index < 0 || index >= _results.Count)
            throw new IndexOutOfRangeException();

        if (_results[index] is SurrealDbOkResult okResult)
            return okResult.GetValue<T>();

        throw new NotSupportedException(
            $"Cannot get value from a result of type {_results[index].GetType()}"
        );
    }

    /// <summary>
    /// Gets the result value of the query based on its index.
    /// </summary>
    /// <typeparam name="T">The type of the query result value.</typeparam>
    /// <param name="index">The index of the result in the query response.</param>
    /// <exception cref="IndexOutOfRangeException">Index is out of range.</exception>
    /// <exception cref="NotSupportedException">The result is not an OK result.</exception>
    public IEnumerable<T> GetValues<T>(int index)
    {
        if (index < 0 || index >= _results.Count)
            throw new IndexOutOfRangeException();

        if (_results[index] is SurrealDbOkResult okResult)
            return okResult.GetValues<T>();

        throw new NotSupportedException(
            $"Cannot get values from a result of type {_results[index].GetType()}"
        );
    }
}
