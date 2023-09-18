namespace SurrealDB.NET;

public interface ISurrealClient
{
	Task<T> GetAsync<T>(Thing thing, CancellationToken ct = default);

	Task<IEnumerable<T>> GetAsync<T>(Table table, CancellationToken ct = default);

	Task<T> CreateAsync<T>(Thing thing, T data, CancellationToken ct = default);

	Task<T> InsertAsync<T>(Table table, T data, CancellationToken ct = default);

	Task<IEnumerable<T>> BulkInsertAsync<T>(Table table, IEnumerable<T> data, CancellationToken ct = default);

	Task<T> UpdateAsync<T>(Thing thing, T data, CancellationToken ct = default);

	Task<IEnumerable<T>> BulkUpdateAsync<T>(Table table, IEnumerable<T> data, CancellationToken ct = default);
	
	Task<T> ModifyAsync<T>(Thing thing, SurrealJsonPatchBuilder<T> patcher, CancellationToken ct = default);

	Task<IEnumerable<T>> BulkModifyAsync<T>(Table table, SurrealJsonPatchBuilder<T> patcher, CancellationToken ct = default);

	Task<SurrealLiveQueryId> SubscribeToLiveQueryAsync<T>(Table table, Func<T, SurrealEventType, Task> callback, bool diff = false, CancellationToken ct = default);

	Task<SurrealLiveQueryId> SubscribeToLiveQueryAsync<T>(Table table, Action<T, SurrealEventType> callback, bool diff = false, CancellationToken ct = default);

	Task LetAsync<T>(string name, T value, CancellationToken ct = default);

	Task UnsetAsync<T>(string name, CancellationToken ct = default);

	Task<SurrealQueryResult> QueryAsync(string sql, object? vars = null, CancellationToken ct = default);
}
