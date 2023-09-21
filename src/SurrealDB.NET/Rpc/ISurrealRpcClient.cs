using SurrealDB.NET.Json;

namespace SurrealDB.NET.Rpc;

public interface ISurrealRpcClient : IDisposable
{
	Task UseAsync(string @namespace, string database, CancellationToken ct = default);

	Task<TScope?> InfoAsync<TScope>(CancellationToken ct = default);

	Task<string> SignupAsync<TScope>(string @namespace, string database, string scope, TScope identity, CancellationToken ct = default);

	Task<string> SigninRootAsync(string username, string password, CancellationToken ct = default);

	Task<string> SigninNamespaceAsync(string @namespace, string username, string password, CancellationToken ct = default);

	Task<string> SigninAsync<TScope>(string @namespace, string database, string scope, TScope identity, CancellationToken ct = default);

	Task AuthenticateAsync(string token, CancellationToken ct = default);

	Task InvalidateAsync(CancellationToken ct = default);

	Task LetAsync<T>(string name, T value, CancellationToken ct = default);

	Task UnsetAsync(string name, CancellationToken ct = default);

	Task<SurrealLiveQueryId> LiveAsync<T>(Table table, Func<T, SurrealEventType, Task> callback, bool diff = false, CancellationToken ct = default);

	Task<SurrealLiveQueryId> LiveAsync<T>(Table table, Action<T, SurrealEventType> callback, bool diff = false, CancellationToken ct = default);

	Task KillAsync(SurrealLiveQueryId queryId, CancellationToken ct = default);

	Task<SurrealQueryResult> QueryAsync(string sql, object? vars = null, CancellationToken ct = default);

	Task<T?> SelectAsync<T>(Thing thing, CancellationToken ct = default);

	Task<IEnumerable<T>> SelectManyAsync<T>(Table table, CancellationToken ct = default);

	Task<T> CreateAsync<T>(Thing thing, T data, CancellationToken ct = default);

	Task<T> InsertAsync<T>(Table table, T data, CancellationToken ct = default);

	Task<T> UpdateAsync<T>(Thing thing, T data, CancellationToken ct = default);

	Task<IEnumerable<T>> BulkUpdateAsync<T>(Table table, T data, CancellationToken ct);

	Task<T> MergeAsync<T>(Thing thing, object merger, CancellationToken ct = default);

	Task<IEnumerable<T>> BulkMergeAsync<T>(Table table, object merger, CancellationToken ct = default);

	Task<T> PatchAsync<T>(Thing thing, Action<SurrealJsonPatchBuilder<T>> patches, CancellationToken ct = default);
	
	Task<IEnumerable<T>> BulkPatchAsync<T>(Table thing, Action<SurrealJsonPatchBuilder<T>> patches, CancellationToken ct = default);

	Task<T?> DeleteAsync<T>(Thing thing, CancellationToken ct = default);
}
