using SurrealDB.NET.Json;
using SurrealDB.NET.Rpc;

namespace SurrealDB.NET;

public sealed partial class SurrealClient
{
    public async Task<T?> GetAsync<T>(Thing thing, CancellationToken ct = default)
    {
        return await _textClient.SelectAsync<T>(thing, ct).ConfigureAwait(false);
    }

    public async Task<IEnumerable<T>> GetAsync<T>(Table table, CancellationToken ct = default)
    {
        return await _textClient.SelectManyAsync<T>(table, ct).ConfigureAwait(false);
    }

    public async Task<T> CreateAsync<T>(Thing thing, T data, CancellationToken ct = default)
    {
        return await _textClient.CreateAsync(thing, data, ct).ConfigureAwait(false);
    }

    public async Task<T> InsertAsync<T>(Table table, T data, CancellationToken ct = default)
    {
        return await _textClient.InsertAsync(table, data, ct).ConfigureAwait(false);
    }

    public async Task<IEnumerable<T>> BulkInsertAsync<T>(Table table, IEnumerable<T> data, CancellationToken ct = default)
    {
        return await _textClient.InsertAsync(table, data, ct).ConfigureAwait(false);
    }

    public async Task<T?> UpdateAsync<T>(Thing thing, T data, CancellationToken ct = default)
    {
        return await _textClient.UpdateAsync(thing, data, ct).ConfigureAwait(false);
    }

    public async Task<IEnumerable<T>> BulkUpdateAsync<T>(Table table, T data, CancellationToken ct = default)
    {
        return await _textClient.BulkUpdateAsync(table, data, ct).ConfigureAwait(false);
    }

    public async Task<T?> ModifyAsync<T>(Thing thing, Action<SurrealJsonPatchBuilder<T>> patcher, CancellationToken ct = default)
    {
        return await _textClient.PatchAsync<T>(thing, patcher, ct).ConfigureAwait(false);
    }

    public async Task<IEnumerable<T>> BulkModifyAsync<T>(Table table, Action<SurrealJsonPatchBuilder<T>> patcher, CancellationToken ct = default)
    {
        return await _textClient.BulkPatchAsync(table, patcher, ct).ConfigureAwait(false);
    }

    public async Task<SurrealLiveQueryId> SubscribeToLiveQueryAsync<T>(Table table, Func<T, SurrealEventType, Task> callback, bool diff = false, CancellationToken ct = default)
    {
        return await _textClient.LiveAsync(table, callback, diff, ct).ConfigureAwait(false);
    }

    public async Task<SurrealLiveQueryId> SubscribeToLiveQueryAsync<T>(Table table, Action<T, SurrealEventType> callback, bool diff = false, CancellationToken ct = default)
    {
        return await _textClient.LiveAsync(table, callback, diff, ct).ConfigureAwait(false);
    }

    public async Task<SurrealQueryResult> QueryAsync(string sql, object? vars = null, CancellationToken ct = default)
    {
        return await _textClient.QueryAsync(sql, vars, ct).ConfigureAwait(false);
    }
}
