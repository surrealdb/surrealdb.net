namespace SurrealDB.NET;

public interface ISurrealRpcClient
{
    Task UseAsync(string @namespace, string database, CancellationToken ct = default);

    Task<T?> InfoAsync<T>(CancellationToken ct = default);

    Task<string> SignupAsync<T>(string @namespace, string database, string scope, T identity, CancellationToken ct = default);

    Task<string> SigninRootAsync(string username, string password, CancellationToken ct = default);

    Task<string> SigninScopeAsync<T>(string @namespace, string database, string scope, T identity, CancellationToken ct = default);

    Task AuthenticateAsync(string token, CancellationToken ct = default);

    Task InvalidateAsync(CancellationToken ct = default);

    Task LetAsync<T>(string name, T value, CancellationToken ct = default);

    Task UnsetAsync(string name, CancellationToken ct = default);

    Task<SurrealLiveQueryId> LiveAsync<T>(Table table, Func<T, SurrealEventType, Task> callback, bool diff = false, CancellationToken ct = default);

    Task<SurrealLiveQueryId> LiveAsync<T>(Table table, Action<T, SurrealEventType> callback, bool diff = false, CancellationToken ct = default);

    Task KillAsync(SurrealLiveQueryId queryId, CancellationToken ct = default);

    Task<SurrealQueryResult> QueryAsync(string sql, object? vars = null, CancellationToken ct = default);

    Task<T?> SelectAsync<T>(Thing recordId, CancellationToken ct = default);

    Task<IEnumerable<T>> SelectManyAsync<T>(Thing table, CancellationToken ct = default);

    Task<T> CreateAsync<T>(Thing thing, T data, CancellationToken ct = default);

    Task<T> InsertAsync<T>(Table table, T data, CancellationToken ct = default);

    Task<T> UpdateAsync<T>(Thing thing, T data, CancellationToken ct = default);

    Task<IEnumerable<T>> BulkUpdateAsync<T>(string table, T data, CancellationToken ct);

    Task<T> MergeAsync<T>(Thing thing, object merger, CancellationToken ct = default);
    
    Task<IEnumerable<T>> MergeAsync<T>(string table, object merger, CancellationToken ct = default);

    Task PatchAsync<T>(Thing thing, Action<SurrealJsonPatchBuilder<T>> patches, CancellationToken ct = default);

    Task<T?> DeleteAsync<T>(Thing thing, CancellationToken ct = default);
}
