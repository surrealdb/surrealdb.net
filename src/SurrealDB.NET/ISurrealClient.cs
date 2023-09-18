using SurrealDB.NET.Json;
using SurrealDB.NET.Rpc;

namespace SurrealDB.NET;

/// <summary>
/// Defines a client for SurrealDB that covers the full set of capabilities.
/// </summary>
public interface ISurrealClient
{
    /// <summary>
    /// <para>
    /// Sets the active namespace and database on this client.
    /// </para>
    /// <para>
    /// This is equivalent to the
	/// <seealso href="https://surrealdb.com/docs/surrealql/statements/use">
	///		<c>USE</c>
	///	</seealso>
	/// statement in SurrealQL.
    /// </para>
	/// <para>
	/// This method is only supported on the <c>RPC</c>/<c>WebSocket</c> engine.
	/// </para>
    /// </summary>
    /// <param name="namespace">The namespace to activate.</param>
    /// <param name="database">The database to active.</param>
    Task UseAsync(string @namespace, string database, CancellationToken ct = default);

    /// <summary>
    /// <para>
    /// Retrieves information about the currently authenticated scope.
    /// </para>
    /// </summary>
    /// <typeparam name="TScope">The scope specific data type.</typeparam>
    /// <returns>
    /// The scope data or <see langword="null"/> if there is no authenticated scope.
    /// </returns>
    Task<TScope?> InfoAsync<TScope>(CancellationToken ct = default);

    Task<string> SignupAsync<TScope>(
        string @namespace,
        string database,
        string scope,
        TScope user,
        CancellationToken ct = default);

    Task<string> SignupAsync<TScope>(
        string scope,
        TScope user,
        CancellationToken ct = default);

    Task SigninRootAsync(
        string username,
        string password,
        CancellationToken ct = default);

    Task SigninNamespaceAsync(
        string username,
        string password,
        CancellationToken ct = default);

    Task SigninAsync<TScope>(
        string @namespace,
        string database,
        string scope,
        TScope user,
        CancellationToken ct = default);

    Task SigninAsync<TScope>(string scope, TScope user, CancellationToken ct = default);

    Task Authenticate(string token, CancellationToken ct = default);

    Task ImportAsync(Stream source, CancellationToken ct = default);

    Task ExportAsync(Stream destination, CancellationToken ct = default);

    Task<bool> HealthAsync(CancellationToken ct = default);

    Task<bool> StatusAsync(CancellationToken ct = default);

    Task<string> VersionAsync(CancellationToken ct = default);

    Task<T?> GetAsync<T>(Thing thing, CancellationToken ct = default);

    Task<IEnumerable<T>> GetAsync<T>(Table table, CancellationToken ct = default);

    Task<T> CreateAsync<T>(Thing thing, T data, CancellationToken ct = default);

    Task<T> InsertAsync<T>(Table table, T data, CancellationToken ct = default);

    Task<IEnumerable<T>> BulkInsertAsync<T>(
        Table table,
        IEnumerable<T> data,
        CancellationToken ct = default);

    Task<T?> UpdateAsync<T>(Thing thing, T data, CancellationToken ct = default);

    Task<IEnumerable<T>> BulkUpdateAsync<T>(
        Table table,
        T data,
        CancellationToken ct = default);

    Task<T?> ModifyAsync<T>(
        Thing thing,
        Action<SurrealJsonPatchBuilder<T>> patcher,
        CancellationToken ct = default);

    Task<IEnumerable<T>> BulkModifyAsync<T>(
        Table table,
        Action<SurrealJsonPatchBuilder<T>> patcher,
        CancellationToken ct = default);

    Task<SurrealLiveQueryId> SubscribeToLiveQueryAsync<T>(
        Table table,
        Func<T, SurrealEventType, Task> callback,
        bool diff = false,
        CancellationToken ct = default);

    Task<SurrealLiveQueryId> SubscribeToLiveQueryAsync<T>(
        Table table,
        Action<T, SurrealEventType> callback,
        bool diff = false,
        CancellationToken ct = default);

    Task LetAsync<T>(string name, T value, CancellationToken ct = default);

    Task UnsetAsync<T>(string name, CancellationToken ct = default);

    Task<SurrealQueryResult> QueryAsync(
        string sql,
        object? vars = null,
        CancellationToken ct = default);
}
