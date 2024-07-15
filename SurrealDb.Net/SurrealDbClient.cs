using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Net.Internals;
using SurrealDb.Net.Internals.Models;
using SurrealDb.Net.Models;
using SurrealDb.Net.Models.Auth;
using SurrealDb.Net.Models.LiveQuery;
using SurrealDb.Net.Models.Response;
using SystemTextJsonPatch;

namespace SurrealDb.Net;

/// <summary>
/// The entry point to communicate with a SurrealDB instance.
/// Authenticate, use namespace/database, execute queries, etc...
/// </summary>
public class SurrealDbClient : ISurrealDbClient
{
    private readonly ISurrealDbEngine _engine;

    public Uri Uri { get; }
    public string? NamingPolicy { get; }

    /// <summary>
    /// Creates a new SurrealDbClient, with the defined endpoint.
    /// </summary>
    /// <param name="endpoint">The endpoint to access a SurrealDB instance.</param>
    /// <param name="namingPolicy">The naming policy to use for serialization.</param>
    /// <param name="httpClientFactory">An IHttpClientFactory instance, or none.</param>
    /// <param name="configureJsonSerializerOptions">An optional action to configure <see cref="JsonSerializerOptions"/>.</param>
    /// <param name="prependJsonSerializerContexts">
    /// An option function to retrieve the <see cref="JsonSerializerContext"/> to use and prepend to the current list of contexts,
    /// in AoT mode.
    /// </param>
    /// <param name="appendJsonSerializerContexts">
    /// An option function to retrieve the <see cref="JsonSerializerContext"/> to use and append to the current list of contexts,
    /// in AoT mode.
    /// </param>
    /// <exception cref="ArgumentException"></exception>
    public SurrealDbClient(
        string endpoint,
        string? namingPolicy = null,
        IHttpClientFactory? httpClientFactory = null,
        Action<JsonSerializerOptions>? configureJsonSerializerOptions = null,
        Func<JsonSerializerContext[]>? prependJsonSerializerContexts = null,
        Func<JsonSerializerContext[]>? appendJsonSerializerContexts = null
    )
        : this(
            new SurrealDbClientParams(endpoint, namingPolicy),
            httpClientFactory,
            configureJsonSerializerOptions,
            prependJsonSerializerContexts,
            appendJsonSerializerContexts
        ) { }

    /// <summary>
    /// Creates a new SurrealDbClient using a specific configuration.
    /// </summary>
    /// <param name="configuration">The configuration options for the SurrealDbClient.</param>
    /// <param name="httpClientFactory">An IHttpClientFactory instance, or none.</param>
    /// <param name="configureJsonSerializerOptions">An optional action to configure <see cref="JsonSerializerOptions"/>.</param>
    /// <param name="prependJsonSerializerContexts">
    /// An option function to retrieve the <see cref="JsonSerializerContext"/> to use and prepend to the current list of contexts,
    /// in AoT mode.
    /// </param>
    /// <param name="appendJsonSerializerContexts">
    /// An option function to retrieve the <see cref="JsonSerializerContext"/> to use and append to the current list of contexts,
    /// in AoT mode.
    /// </param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public SurrealDbClient(
        SurrealDbOptions configuration,
        IHttpClientFactory? httpClientFactory = null,
        Action<JsonSerializerOptions>? configureJsonSerializerOptions = null,
        Func<JsonSerializerContext[]>? prependJsonSerializerContexts = null,
        Func<JsonSerializerContext[]>? appendJsonSerializerContexts = null
    )
        : this(
            new SurrealDbClientParams(configuration),
            httpClientFactory,
            configureJsonSerializerOptions,
            prependJsonSerializerContexts,
            appendJsonSerializerContexts
        ) { }

    internal SurrealDbClient(
        SurrealDbClientParams parameters,
        IHttpClientFactory? httpClientFactory = null,
        Action<JsonSerializerOptions>? configureJsonSerializerOptions = null,
        Func<JsonSerializerContext[]>? prependJsonSerializerContexts = null,
        Func<JsonSerializerContext[]>? appendJsonSerializerContexts = null
    )
    {
        if (parameters.Endpoint is null)
            throw new ArgumentNullException(nameof(parameters), "The endpoint is required.");

        Uri = new Uri(parameters.Endpoint);
        NamingPolicy = parameters.NamingPolicy;

        var protocol = Uri.Scheme;

        _engine = protocol switch
        {
            "http"
            or "https"
                => new SurrealDbHttpEngine(
                    parameters,
                    httpClientFactory,
                    configureJsonSerializerOptions,
                    prependJsonSerializerContexts,
                    appendJsonSerializerContexts
                ),
            "ws"
            or "wss"
                => new SurrealDbWsEngine(
                    parameters,
                    configureJsonSerializerOptions,
                    prependJsonSerializerContexts,
                    appendJsonSerializerContexts
                ),
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
        where T : IRecord
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

    public Task<T> Info<T>(CancellationToken cancellationToken = default)
    {
        return _engine.Info<T>(cancellationToken);
    }

    public Task Invalidate(CancellationToken cancellationToken = default)
    {
        return _engine.Invalidate(cancellationToken);
    }

    public Task Kill(Guid queryUuid, CancellationToken cancellationToken = default)
    {
        return _engine.Kill(
            queryUuid,
            SurrealDbLiveQueryClosureReason.QueryKilled,
            cancellationToken
        );
    }

    public SurrealDbLiveQuery<T> ListenLive<T>(Guid queryUuid)
    {
        return _engine.ListenLive<T>(queryUuid);
    }

    public Task<SurrealDbLiveQuery<T>> LiveRawQuery<T>(
        string query,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default
    )
    {
        return _engine.LiveRawQuery<T>(
            query,
            parameters ?? ImmutableDictionary<string, object?>.Empty,
            cancellationToken
        );
    }

    public Task<SurrealDbLiveQuery<T>> LiveQuery<T>(
        FormattableString query,
        CancellationToken cancellationToken = default
    )
    {
        return _engine.LiveQuery<T>(query, cancellationToken);
    }

    public Task<SurrealDbLiveQuery<T>> LiveTable<T>(
        string table,
        bool diff = false,
        CancellationToken cancellationToken = default
    )
    {
        return _engine.LiveTable<T>(table, diff, cancellationToken);
    }

    public Task<TOutput> Merge<TMerge, TOutput>(
        TMerge data,
        CancellationToken cancellationToken = default
    )
        where TMerge : IRecord
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

    public Task<IEnumerable<TOutput>> MergeAll<TMerge, TOutput>(
        string table,
        TMerge data,
        CancellationToken cancellationToken = default
    )
        where TMerge : class
    {
        return _engine.MergeAll<TMerge, TOutput>(table, data, cancellationToken);
    }

    public Task<IEnumerable<T>> MergeAll<T>(
        string table,
        Dictionary<string, object> data,
        CancellationToken cancellationToken = default
    )
    {
        return _engine.MergeAll<T>(table, data, cancellationToken);
    }

    public Task<T> Patch<T>(
        Thing thing,
        JsonPatchDocument<T> patches,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        return _engine.Patch(thing, patches, cancellationToken);
    }

    public Task<IEnumerable<T>> PatchAll<T>(
        string table,
        JsonPatchDocument<T> patches,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        return _engine.PatchAll(table, patches, cancellationToken);
    }

    public Task<SurrealDbResponse> Query(
        FormattableString query,
        CancellationToken cancellationToken = default
    )
    {
        return _engine.Query(query, cancellationToken);
    }

    public Task<SurrealDbResponse> RawQuery(
        string query,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default
    )
    {
        return _engine.RawQuery(
            query,
            parameters ?? ImmutableDictionary<string, object?>.Empty,
            cancellationToken
        );
    }

    public Task<IEnumerable<T>> Select<T>(
        string table,
        CancellationToken cancellationToken = default
    )
    {
        return _engine.Select<T>(table, cancellationToken);
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

    public Task<IEnumerable<T>> UpdateAll<T>(
        string table,
        T data,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        return _engine.UpdateAll(table, data, cancellationToken);
    }

    public Task<T> Upsert<T>(T data, CancellationToken cancellationToken = default)
        where T : IRecord
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
