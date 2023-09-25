using SurrealDb.Net.Internals.Models;
using System.Collections;

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

	internal SurrealDbResponse(ISurrealDbResult result)
	{
		_results = new() { result };
	}
	internal SurrealDbResponse(List<ISurrealDbResult> results)
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
	/// Gets the result value of the query based on its index.
	/// </summary>
	/// <typeparam name="T">The type of the query result value.</typeparam>
	/// <param name="index">The index of the result in the query response.</param>
	public T? GetValue<T>(int index)
	{
		if (_results[index] is SurrealDbOkResult okResult)
			return okResult.GetValue<T>();

		return default;
	}
}
