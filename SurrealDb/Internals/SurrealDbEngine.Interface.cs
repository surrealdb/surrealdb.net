using SurrealDb.Models;
using SurrealDb.Models.Auth;
using SurrealDb.Models.Response;

namespace SurrealDb.Internals;

internal interface ISurrealDbEngine
{
    Task Authenticate(Jwt jwt, CancellationToken cancellationToken);
	void Configure(string? ns, string? db, string? username, string? password);
	Task Connect(CancellationToken cancellationToken);
    Task<T> Create<T>(T data, CancellationToken cancellationToken) where T : Record;
    Task<T> Create<T>(string table, T data, CancellationToken cancellationToken);
    Task Delete(string table, CancellationToken cancellationToken);
    Task<bool> Delete(Thing thing, CancellationToken cancellationToken);
    void Invalidate();
	Task<TOutput> Patch<TPatch, TOutput>(TPatch data, CancellationToken cancellationToken) where TPatch : Record;
	Task<T> Patch<T>(Thing thing, Dictionary<string, object> data, CancellationToken cancellationToken);
	Task<SurrealDbResponse> Query(string query, IReadOnlyDictionary<string, object> parameters, CancellationToken cancellationToken);
    Task<List<T>> Select<T>(string table, CancellationToken cancellationToken);
    Task<T?> Select<T>(Thing thing, CancellationToken cancellationToken);
    Task Set(string key, object value, CancellationToken cancellationToken);
    Task SignIn(RootAuth root, CancellationToken cancellationToken);
	Task SignIn(NamespaceAuth nsAuth, CancellationToken cancellationToken);
	Task SignIn(DatabaseAuth dbAuth, CancellationToken cancellationToken);
	Task<Jwt> SignIn<T>(T scopeAuth, CancellationToken cancellationToken) where T : ScopeAuth;
	Task<Jwt> SignUp<T>(T scopeAuth, CancellationToken cancellationToken) where T : ScopeAuth;
	void Unset(string key);
	Task<T> Upsert<T>(T data, CancellationToken cancellationToken) where T : Record;
	Task Use(string ns, string db, CancellationToken cancellationToken);
}
