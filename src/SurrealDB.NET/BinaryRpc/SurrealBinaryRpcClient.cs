namespace SurrealDB.NET.BinaryRpc;

internal class SurrealBinaryRpcClient : ISurrealRpcClient
{
	public Task AuthenticateAsync(string token, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task<IEnumerable<T>> BulkUpdateAsync<T>(string table, T data, CancellationToken ct)
	{
		throw new NotImplementedException();
	}

	public Task<T> CreateAsync<T>(Thing thing, T data, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task<T?> DeleteAsync<T>(Thing thing, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public void Dispose()
	{
		throw new NotImplementedException();
	}

	public Task<T?> InfoAsync<T>(CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task<T> InsertAsync<T>(string table, T data, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task InvalidateAsync(CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task KillAsync(SurrealLiveQueryId queryId, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task LetAsync<T>(string name, T value, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task<SurrealLiveQueryId> LiveAsync<T>(string table, Func<T, SurrealEventType, Task> callback, bool diff = false, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task<SurrealLiveQueryId> LiveAsync<T>(string table, Action<T, SurrealEventType> callback, bool diff = false, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task<T> MergeAsync<T>(Thing thing, object merger, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task<IEnumerable<T>> MergeAsync<T>(string table, object merger, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task PatchAsync<T>(Thing thing, Action<SurrealJsonPatchBuilder<T>> patches, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task<SurrealQueryResult> QueryAsync(string sql, object? vars = null, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task<T?> SelectAsync<T>(Thing recordId, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task<IEnumerable<T>> SelectManyAsync<T>(Thing table, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task<string> SigninRootAsync(string username, string password, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task<string> SigninScopeAsync<T>(string @namespace, string database, string scope, T identity, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task<string> SignupAsync<T>(string @namespace, string database, string scope, T identity, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task UnsetAsync(string name, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task<T> UpdateAsync<T>(Thing thing, T data, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}

	public Task UseAsync(string @namespace, string database, CancellationToken ct = default)
	{
		throw new NotImplementedException();
	}
}
