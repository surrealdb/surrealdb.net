using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Internals;
using SurrealDb.Models;
using SurrealDb.Models.Auth;
using SurrealDb.Models.Response;

namespace SurrealDb;

/// <summary>
/// The entry point to communicate with a SurrealDB instance.
/// Authenticate, use namespace/database, execute queries, etc...
/// </summary>
public class SurrealDbClient : ISurrealDbClient
{
    private readonly IHttpClientFactory? _httpClientFactory;
    private readonly ISurrealDbEngine _engine;

	public Uri Uri { get; }

	/// <summary>
	/// Creates a new SurrealDbClient, with the defined endpoint.
	/// </summary>
	/// <param name="endpoint">The endpoint to access a SurrealDB instance.</param>
	/// <param name="httpClientFactory">An IHttpClientFactory instance, or none.</param>
	/// <exception cref="ArgumentException"></exception>
	public SurrealDbClient(string endpoint, IHttpClientFactory? httpClientFactory = null)
		: this(endpoint, null, null, null, null, httpClientFactory) { }

	/// <summary>
	/// Creates a new SurrealDbClient using a specific configuration.
	/// </summary>
	/// <param name="configuration">The configuration options for the SurrealDbClient.</param>
	/// <param name="httpClientFactory">An IHttpClientFactory instance, or none.</param>
	/// <exception cref="ArgumentException"></exception>
	/// <exception cref="ArgumentNullException"></exception>
	public SurrealDbClient(SurrealDbOptions configuration, IHttpClientFactory? httpClientFactory = null)
		: this(configuration.Endpoint, configuration.Namespace, configuration.Database, configuration.Username, configuration.Password, httpClientFactory) { }

	internal SurrealDbClient(
		string? endpoint,
		string? ns,
		string? db,
		string? username, // TODO : Auth
		string? password, // TODO : Auth
		IHttpClientFactory? httpClientFactory = null // TODO : avoid n arguments
	)
    {
		if (endpoint is null)
			throw new ArgumentNullException(nameof(endpoint));

        Uri = new Uri(endpoint);
        _httpClientFactory = httpClientFactory;

        var protocol = Uri.Scheme;

        _engine = protocol switch
        {
            "http" or "https" => new SurrealDbHttpEngine(Uri, _httpClientFactory),
            "ws" or "wss" => new SurrealDbWsEngine(Uri),
            _ => throw new ArgumentException("This protocol is not supported."),
        };

		_engine.Configure(ns, db, username, password);
	}

	public Task Authenticate(Jwt jwt, CancellationToken cancellationToken = default)
    {
        return _engine.Authenticate(jwt, cancellationToken);
    }

    public Task Connect(CancellationToken cancellationToken = default)
    {
        return _engine.Connect(cancellationToken);
    }

    public Task<T> Create<T>(T data, CancellationToken cancellationToken = default) where T : Record
    {
        return _engine.Create(data, cancellationToken);
    }
    public Task<T> Create<T>(string table, T? data = default, CancellationToken cancellationToken = default)
    {
        return _engine.Create(table, data, cancellationToken);
    }

    public Task Delete(string table, CancellationToken cancellationToken = default)
    {
        return _engine.Delete(table, cancellationToken);
    }
    public Task<bool> Delete(string table, string id, CancellationToken cancellationToken = default)
    {
        return Delete(new Thing(table, id), cancellationToken);
    }
    public Task<bool> Delete(Thing thing, CancellationToken cancellationToken = default)
    {
        return _engine.Delete(thing, cancellationToken);
    }

    public void Invalidate()
    {
        _engine.Invalidate();
	}

	public Task<TOutput> Patch<TPatch, TOutput>(TPatch data, CancellationToken cancellationToken = default) where TPatch : Record
	{
		return _engine.Patch<TPatch, TOutput>(data, cancellationToken);
	}
	public Task<T> Patch<T>(Thing thing, Dictionary<string, object> data, CancellationToken cancellationToken = default)
	{
		return _engine.Patch<T>(thing, data, cancellationToken);
	}

	public Task<SurrealDbResponse> Query(
		string query,
		IReadOnlyDictionary<string, object>? parameters = null,
		CancellationToken cancellationToken = default
	)
    {
        return _engine.Query(
            query, 
            parameters ?? new Dictionary<string, object>(),
            cancellationToken
        );
    }

    public Task<List<T>> Select<T>(string table, CancellationToken cancellationToken = default)
    {
        return _engine.Select<T>(table, cancellationToken);
    }
    public Task<T?> Select<T>(string table, string id, CancellationToken cancellationToken = default)
    {
        return Select<T?>(new Thing(table, id), cancellationToken);
    }
    public Task<T?> Select<T>(Thing thing, CancellationToken cancellationToken = default)
    {
        return _engine.Select<T?>(thing, cancellationToken);
    }

    public Task Set(string key, object value, CancellationToken cancellationToken = default)
    {
        return _engine.Set(key, value, cancellationToken);
    }

    public Task SignIn(RootAuth root, CancellationToken cancellationToken = default)
    {
        return _engine.SignIn(root, cancellationToken);
	}
	public Task SignIn(NamespaceAuth nsAuth, CancellationToken cancellationToken = default)
	{
		return _engine.SignIn(nsAuth, cancellationToken);
	}
	public Task SignIn(DatabaseAuth dbAuth, CancellationToken cancellationToken = default)
	{
		return _engine.SignIn(dbAuth, cancellationToken);
	}
	public Task<Jwt> SignIn<T>(T scopeAuth, CancellationToken cancellationToken = default) where T : ScopeAuth
	{
		return _engine.SignIn(scopeAuth, cancellationToken);
	}

	public Task<Jwt> SignUp<T>(T scopeAuth, CancellationToken cancellationToken = default) where T : ScopeAuth
	{
		return _engine.SignUp(scopeAuth, cancellationToken);
	}

	public void Unset(string key)
    {
        _engine.Unset(key);
	}

	public Task<T> Upsert<T>(T data, CancellationToken cancellationToken = default) where T : Record
	{
		return _engine.Upsert(data, cancellationToken);
	}

	public Task Use(string ns, string db, CancellationToken cancellationToken = default)
    {
        return _engine.Use(ns, db, cancellationToken);
    }

    public async Task<string> Version()
    {
        var httpEngine = (_engine as SurrealDbHttpEngine) ?? new SurrealDbHttpEngine(Uri, _httpClientFactory);
        using var client = httpEngine.CreateHttpClient();

        return await client.GetStringAsync("/version");
    }
}
