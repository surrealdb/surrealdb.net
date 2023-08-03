using SurrealDb.Models;
using SurrealDb.Models.Auth;
using SurrealDb.Models.Response;

namespace SurrealDb.Internals;

internal class SurrealDbWsEngine : ISurrealDbEngine
{
    private readonly Uri _uri;

    public SurrealDbWsEngine(Uri uri)
    {
        _uri = uri;
    }

    public Task Authenticate(Jwt jwt, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

	public void Configure(string? ns, string? db, string? username, string? password)
	{
		throw new NotImplementedException();
	}

	public Task Connect(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task<T> Create<T>(string table, T data, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
    public Task<T> Create<T>(T data, CancellationToken cancellationToken) where T : Record
    {
        throw new NotImplementedException();
    }

    public Task Delete(string table, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
    public Task<bool> Delete(Thing thing, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public void Invalidate()
    {
        throw new NotImplementedException();
    }

	public Task<TOutput> Patch<TPatch, TOutput>(TPatch data, CancellationToken cancellationToken) where TPatch : Record
	{
		throw new NotImplementedException();
	}
	public Task<T> Patch<T>(Thing thing, Dictionary<string, object> data, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}

	public Task<SurrealDbResponse> Query(
		string query,
		IReadOnlyDictionary<string, object> parameters,
		CancellationToken cancellationToken
	)
	{
        throw new NotImplementedException();
    }

    public Task<List<T>> Select<T>(string table, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
    public Task<T?> Select<T>(Thing thing, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task Set(string key, object value, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task SignIn(RootAuth root, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
	public Task SignIn(NamespaceAuth nsAuth, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}
	public Task SignIn(DatabaseAuth dbAuth, CancellationToken cancellationToken)
	{
		throw new NotImplementedException();
	}
	public Task<Jwt> SignIn<T>(T scopeAuth, CancellationToken cancellationToken) where T : ScopeAuth
	{
		throw new NotImplementedException();
	}

	public Task<Jwt> SignUp<T>(T scopeAuth, CancellationToken cancellationToken) where T : ScopeAuth
	{
		throw new NotImplementedException();
	}

	public void Unset(string key)
    {
        throw new NotImplementedException();
    }

	public Task<T> Upsert<T>(T data, CancellationToken cancellationToken) where T : Record
	{
		throw new NotImplementedException();
	}

	public Task Use(string ns, string db, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
