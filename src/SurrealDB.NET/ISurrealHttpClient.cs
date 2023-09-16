namespace SurrealDB.NET;

public interface ISurrealHttpClient
{
	// https://surrealdb.com/docs/integration/http#export
	Task ExportAsync(CancellationToken ct = default);

	// https://surrealdb.com/docs/integration/http#health
	Task<bool> HealthAsync(CancellationToken ct = default);

	// https://surrealdb.com/docs/integration/http#import
	Task ImportAsync(CancellationToken ct = default);

	// https://surrealdb.com/docs/integration/http#select-all
	Task<IEnumerable<T>> GetAllAsync<T>(Table table, CancellationToken ct = default);

	// https://surrealdb.com/docs/integration/http#create-all
	Task<T> InsertAsync<T>(Table table, T data, CancellationToken ct = default);

	// https://surrealdb.com/docs/integration/http#delete-all
	Task DropAsync(Table table, CancellationToken ct = default);

	// https://surrealdb.com/docs/integration/http#select-one
	Task<T> GetAsync<T>(Thing thing, CancellationToken ct = default);

	// https://surrealdb.com/docs/integration/http#create-one
	Task<T> CreateAsync<T>(Thing thing, T data, CancellationToken ct = default);

	// https://surrealdb.com/docs/integration/http#update-one
	Task<T> UpdateAsync<T>(Thing thing, T data, CancellationToken ct = default);

	// https://surrealdb.com/docs/integration/http#modify-one
	Task ModifyAsync<T>(Thing thing, Action<SurrealJsonPatchBuilder<T>> patches, CancellationToken ct = default);

	// https://surrealdb.com/docs/integration/http#delete-one
	Task<T> DeleteAsync<T>(Thing thing, CancellationToken ct = default);

	// https://surrealdb.com/docs/integration/http#signup
	Task<string> SignupAsync<T>(string @namespace, string database, string scope, T user, CancellationToken ct = default);

	// https://surrealdb.com/docs/integration/http#signin
	Task<string> SigninScopeAsync<T>(string @namespace, string database, string scope, T user, CancellationToken ct = default);

	// https://surrealdb.com/docs/integration/http#signin
	Task<string> SigninNamespaceAsync<T>(string @namespace, T user, CancellationToken ct = default);

	// https://surrealdb.com/docs/integration/http#signin
	Task<string> SigninRootAsync(string username, string password, CancellationToken ct = default);

	// https://surrealdb.com/docs/integration/http#sql
	Task QueryAsync(CancellationToken ct = default);

	//https://surrealdb.com/docs/integration/http#status
	Task<bool> StatusAsync(CancellationToken ct = default);

	// https://surrealdb.com/docs/integration/http#version
	Task<string> VersionAsync(CancellationToken ct = default);
}
