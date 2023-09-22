namespace SurrealDB.NET.Http;

public interface ISurrealHttpClient : IDisposable
{
	void AttachToken(string token);

	// https://surrealdb.com/docs/integration/http#export
	Task ExportAsync(Stream destination, CancellationToken ct = default);

	// https://surrealdb.com/docs/integration/http#health
	Task<bool> HealthAsync(CancellationToken ct = default);

	// https://surrealdb.com/docs/integration/http#import
	Task ImportAsync(Stream source, CancellationToken ct = default);

	// https://surrealdb.com/docs/integration/http#select-all
	Task<IEnumerable<T>> GetAllAsync<T>(Table table, CancellationToken ct = default);

	// https://surrealdb.com/docs/integration/http#create-all
	Task<T> InsertAsync<T>(Table table, T data, CancellationToken ct = default);

	// https://surrealdb.com/docs/integration/http#delete-all
	Task<IEnumerable<T>> DropAsync<T>(Table table, CancellationToken ct = default);

	// https://surrealdb.com/docs/integration/http#select-one
	Task<T?> GetAsync<T>(Thing thing, CancellationToken ct = default);

	// https://surrealdb.com/docs/integration/http#create-one
	Task<T> CreateAsync<T>(Thing thing, T data, CancellationToken ct = default);

	// https://surrealdb.com/docs/integration/http#update-one
	Task<T?> UpdateAsync<T>(Thing thing, T data, CancellationToken ct = default);

	// https://surrealdb.com/docs/integration/http#modify-one
	Task<T?> MergeAsync<T>(Thing thing, object merge, CancellationToken ct = default);

	// https://surrealdb.com/docs/integration/http#delete-one
	Task<T?> DeleteAsync<T>(Thing thing, CancellationToken ct = default);

	// https://surrealdb.com/docs/integration/http#signup
	Task<string> SignupAsync<T>(string @namespace, string database, string scope, T user, CancellationToken ct = default);

	// https://surrealdb.com/docs/integration/http#signin
	Task<string> SigninScopeAsync<T>(string @namespace, string database, string scope, T user, CancellationToken ct = default);

	// https://surrealdb.com/docs/integration/http#signin
	Task<string> SigninNamespaceAsync(string @namespace, string username, string password, CancellationToken ct = default);

	// https://surrealdb.com/docs/integration/http#signin
	Task<string> SigninRootAsync(string username, string password, CancellationToken ct = default);

	// https://surrealdb.com/docs/integration/http#sql
	Task<SurrealQueryResult> QueryAsync(string query, object? vars = null, CancellationToken ct = default);

	//https://surrealdb.com/docs/integration/http#status
	Task<bool> StatusAsync(CancellationToken ct = default);

	// https://surrealdb.com/docs/integration/http#version
	Task<string> VersionAsync(CancellationToken ct = default);
}
