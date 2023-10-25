using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Net.Internals;
using SurrealDb.Net.Internals.Models;
using SurrealDb.Net.Models;
using SurrealDb.Net.Models.Auth;
using SurrealDb.Net.Models.Response;
using System.Text.Json;

namespace SurrealDb.Net;

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
    /// <param name="configureJsonSerializerOptions">An optional action to configure <see cref="JsonSerializerOptions"/>.</param>
    /// <exception cref="ArgumentException"></exception>
    public SurrealDbClient(
        string endpoint,
        IHttpClientFactory? httpClientFactory = null,
        Action<JsonSerializerOptions>? configureJsonSerializerOptions = null
    )
        : this(
            new SurrealDbClientParams(endpoint),
            httpClientFactory,
            configureJsonSerializerOptions
        ) { }

    /// <summary>
    /// Creates a new SurrealDbClient using a specific configuration.
    /// </summary>
    /// <param name="configuration">The configuration options for the SurrealDbClient.</param>
    /// <param name="httpClientFactory">An IHttpClientFactory instance, or none.</param>
    /// <param name="configureJsonSerializerOptions">An optional action to configure <see cref="JsonSerializerOptions"/>.</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public SurrealDbClient(
        SurrealDbOptions configuration,
        IHttpClientFactory? httpClientFactory = null,
        Action<JsonSerializerOptions>? configureJsonSerializerOptions = null
    )
        : this(
            new SurrealDbClientParams(configuration),
            httpClientFactory,
            configureJsonSerializerOptions
        ) { }

    internal SurrealDbClient(
        SurrealDbClientParams parameters,
        IHttpClientFactory? httpClientFactory = null,
        Action<JsonSerializerOptions>? configureJsonSerializerOptions = null
    )
    {
        if (parameters.Endpoint is null)
            throw new ArgumentNullException(nameof(parameters), "The endpoint is required.");

        Uri = new Uri(parameters.Endpoint);
        _httpClientFactory = httpClientFactory;

        var protocol = Uri.Scheme;

        _engine = protocol switch
        {
            "http"
            or "https"
                => new SurrealDbHttpEngine(Uri, _httpClientFactory, configureJsonSerializerOptions),
            "ws" or "wss" => new SurrealDbWsEngine(Uri, configureJsonSerializerOptions),
            _ => throw new ArgumentException("This protocol is not supported."),
        };

        if (parameters.Username is not null)
            Configure(parameters.Ns, parameters.Db, parameters.Username, parameters.Password);
        else
            Configure(parameters.Ns, parameters.Db, parameters.Token);
    }

    public Task Authenticate(Jwt jwt, CancellationToken cancellationToken = default)
    {
        return _engine.Authenticate(jwt, cancellationToken);
    }

    public void Configure(string? ns, string? db, string? username, string? password)
    {
        _engine.Configure(ns, db, username, password);
    }

    public void Configure(string? ns, string? db, string? token = null)
    {
        _engine.Configure(ns, db, token);
    }

    public Task Connect(CancellationToken cancellationToken = default)
    {
        return _engine.Connect(cancellationToken);
    }

    public Task<T> Create<T>(T data, CancellationToken cancellationToken = default)
        where T : Record
    {
        return _engine.Create(data, cancellationToken);
    }

    public Task<T> Create<T>(
        string table,
        T? data = default,
        CancellationToken cancellationToken = default
    )
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

    public void Dispose()
    {
        _engine.Dispose();
    }

    public Task<bool> Health(CancellationToken cancellationToken = default)
    {
        return _engine.Health(cancellationToken);
    }

    public Task Invalidate(CancellationToken cancellationToken = default)
    {
        return _engine.Invalidate(cancellationToken);
    }

    public Task<TOutput> Merge<TMerge, TOutput>(
        TMerge data,
        CancellationToken cancellationToken = default
    )
        where TMerge : Record
    {
        return _engine.Merge<TMerge, TOutput>(data, cancellationToken);
    }

    public Task<T> Merge<T>(
        Thing thing,
        Dictionary<string, object> data,
        CancellationToken cancellationToken = default
    )
    {
        return _engine.Merge<T>(thing, data, cancellationToken);
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

    public Task<T?> Select<T>(
        string table,
        string id,
        CancellationToken cancellationToken = default
    )
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

    public Task<Jwt> SignIn(NamespaceAuth nsAuth, CancellationToken cancellationToken = default)
    {
        return _engine.SignIn(nsAuth, cancellationToken);
    }

    public Task<Jwt> SignIn(DatabaseAuth dbAuth, CancellationToken cancellationToken = default)
    {
        return _engine.SignIn(dbAuth, cancellationToken);
    }

    public Task<Jwt> SignIn<T>(T scopeAuth, CancellationToken cancellationToken = default)
        where T : ScopeAuth
    {
        return _engine.SignIn(scopeAuth, cancellationToken);
    }

    public Task<Jwt> SignUp<T>(T scopeAuth, CancellationToken cancellationToken = default)
        where T : ScopeAuth
    {
        return _engine.SignUp(scopeAuth, cancellationToken);
    }

    public Task Unset(string key, CancellationToken cancellationToken = default)
    {
        return _engine.Unset(key, cancellationToken);
    }

    public Task<T> Upsert<T>(T data, CancellationToken cancellationToken = default)
        where T : Record
    {
        return _engine.Upsert(data, cancellationToken);
    }

    public Task Use(string ns, string db, CancellationToken cancellationToken = default)
    {
        return _engine.Use(ns, db, cancellationToken);
    }

    public Task<string> Version(CancellationToken cancellationToken = default)
    {
        return _engine.Version(cancellationToken);
    }
}
