using System.Collections.Immutable;
using Dahomey.Cbor;
using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Embedded.InMemory.Internals;
using SurrealDb.Net;
using SurrealDb.Net.Internals;
using SurrealDb.Net.Internals.Models;
using SurrealDb.Net.Models;
using SurrealDb.Net.Models.Auth;
using SurrealDb.Net.Models.LiveQuery;
using SurrealDb.Net.Models.Response;
using SystemTextJsonPatch;
#if NET6_0_OR_GREATER
using SurrealDb.Net.Handlers;
#else
using SurrealDb.Net.Internals.Extensions;
#endif

namespace SurrealDb.Embedded.InMemory;

public class SurrealDbMemoryClient : ISurrealDbClient
{
    private const string ENDPOINT = "mem://";
    private readonly ISurrealDbInMemoryEngine _engine;

    public Uri Uri { get; }
    public string? NamingPolicy { get; }

    /// <summary>
    /// Creates a new SurrealDbMemoryClient.
    /// </summary>
    /// <param name="namingPolicy">The naming policy to use for serialization.</param>
    /// <exception cref="ArgumentException"></exception>
    public SurrealDbMemoryClient(string? namingPolicy = null)
        : this(new SurrealDbClientParams(ENDPOINT, namingPolicy)) { }

    /// <summary>
    /// Creates a new SurrealDbMemoryClient using a specific configuration.
    /// </summary>
    /// <param name="configuration">The configuration options for the SurrealDbClient.</param>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="ArgumentNullException"></exception>
    public SurrealDbMemoryClient(SurrealDbOptions configuration)
        : this(new SurrealDbClientParams(configuration)) { }

    internal SurrealDbMemoryClient(
        SurrealDbClientParams parameters,
        Action<CborOptions>? configureCborOptions = null
    )
    {
        Uri = new Uri(ENDPOINT);
        NamingPolicy = parameters.NamingPolicy;

        _engine = new SurrealDbInMemoryEngine();
        _engine.Initialize(parameters, configureCborOptions);

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

    public Task<TOutput> Create<TData, TOutput>(
        StringRecordId recordId,
        TData? data = default,
        CancellationToken cancellationToken = default
    )
        where TOutput : Record
    {
        return _engine.Create<TData, TOutput>(recordId, data, cancellationToken);
    }

    public Task Delete(string table, CancellationToken cancellationToken = default)
    {
        return _engine.Delete(table, cancellationToken);
    }

    public Task<bool> Delete(Thing thing, CancellationToken cancellationToken = default)
    {
        return _engine.Delete(thing, cancellationToken);
    }

    public Task<bool> Delete(StringRecordId recordId, CancellationToken cancellationToken = default)
    {
        return _engine.Delete(recordId, cancellationToken);
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

#if NET6_0_OR_GREATER
    public Task<SurrealDbLiveQuery<T>> LiveQuery<T>(
        QueryInterpolatedStringHandler query,
        CancellationToken cancellationToken = default
    )
    {
        return _engine.LiveRawQuery<T>(
            query.GetFormattedText(),
            query.GetParameters(),
            cancellationToken
        );
    }
#else
    public Task<SurrealDbLiveQuery<T>> LiveQuery<T>(
        FormattableString query,
        CancellationToken cancellationToken = default
    )
    {
        var (formattedQuery, parameters) = query.ExtractRawQueryParams();
        return _engine.LiveRawQuery<T>(formattedQuery, parameters, cancellationToken);
    }
#endif

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

    public Task<T> Merge<T>(
        StringRecordId recordId,
        Dictionary<string, object> data,
        CancellationToken cancellationToken = default
    )
    {
        return _engine.Merge<T>(recordId, data, cancellationToken);
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

    public Task<T> Patch<T>(
        StringRecordId recordId,
        JsonPatchDocument<T> patches,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        return _engine.Patch(recordId, patches, cancellationToken);
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

#if NET6_0_OR_GREATER
    public Task<SurrealDbResponse> Query(
        QueryInterpolatedStringHandler query,
        CancellationToken cancellationToken = default
    )
    {
        return _engine.RawQuery(query.GetFormattedText(), query.GetParameters(), cancellationToken);
    }
#else
    public Task<SurrealDbResponse> Query(
        FormattableString query,
        CancellationToken cancellationToken = default
    )
    {
        var (formattedQuery, parameters) = query.ExtractRawQueryParams();
        return _engine.RawQuery(formattedQuery, parameters, cancellationToken);
    }
#endif

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

    public async Task<TOutput> Relate<TOutput>(
        string table,
        Thing @in,
        Thing @out,
        CancellationToken cancellationToken = default
    )
        where TOutput : class
    {
        var outputs = await _engine
            .Relate<TOutput, object>(table, new[] { @in }, new[] { @out }, null, cancellationToken)
            .ConfigureAwait(false);

        return outputs.Single();
    }

    public async Task<TOutput> Relate<TOutput, TData>(
        string table,
        Thing @in,
        Thing @out,
        TData? data,
        CancellationToken cancellationToken = default
    )
        where TOutput : class
    {
        var outputs = await _engine
            .Relate<TOutput, TData>(table, new[] { @in }, new[] { @out }, data, cancellationToken)
            .ConfigureAwait(false);

        return outputs.Single();
    }

    public Task<IEnumerable<TOutput>> Relate<TOutput>(
        string table,
        IEnumerable<Thing> ins,
        Thing @out,
        CancellationToken cancellationToken = default
    )
        where TOutput : class
    {
        return _engine.Relate<TOutput, object>(table, ins, new[] { @out }, null, cancellationToken);
    }

    public Task<IEnumerable<TOutput>> Relate<TOutput, TData>(
        string table,
        IEnumerable<Thing> ins,
        Thing @out,
        TData? data,
        CancellationToken cancellationToken = default
    )
        where TOutput : class
    {
        return _engine.Relate<TOutput, TData>(table, ins, new[] { @out }, data, cancellationToken);
    }

    public Task<IEnumerable<TOutput>> Relate<TOutput>(
        string table,
        Thing @in,
        IEnumerable<Thing> outs,
        CancellationToken cancellationToken = default
    )
        where TOutput : class
    {
        return _engine.Relate<TOutput, object>(table, new[] { @in }, outs, null, cancellationToken);
    }

    public Task<IEnumerable<TOutput>> Relate<TOutput, TData>(
        string table,
        Thing @in,
        IEnumerable<Thing> outs,
        TData? data,
        CancellationToken cancellationToken = default
    )
        where TOutput : class
    {
        return _engine.Relate<TOutput, TData>(table, new[] { @in }, outs, data, cancellationToken);
    }

    public Task<IEnumerable<TOutput>> Relate<TOutput>(
        string table,
        IEnumerable<Thing> ins,
        IEnumerable<Thing> outs,
        CancellationToken cancellationToken = default
    )
        where TOutput : class
    {
        return _engine.Relate<TOutput, object>(table, ins, outs, null, cancellationToken);
    }

    public Task<IEnumerable<TOutput>> Relate<TOutput, TData>(
        string table,
        IEnumerable<Thing> ins,
        IEnumerable<Thing> outs,
        TData? data,
        CancellationToken cancellationToken = default
    )
        where TOutput : class
    {
        return _engine.Relate<TOutput, TData>(table, ins, outs, data, cancellationToken);
    }

    public Task<TOutput> Relate<TOutput>(
        Thing thing,
        Thing @in,
        Thing @out,
        CancellationToken cancellationToken = default
    )
        where TOutput : class
    {
        return _engine.Relate<TOutput, object>(thing, @in, @out, null, cancellationToken);
    }

    public Task<TOutput> Relate<TOutput, TData>(
        Thing thing,
        Thing @in,
        Thing @out,
        TData? data,
        CancellationToken cancellationToken = default
    )
        where TOutput : class
    {
        return _engine.Relate<TOutput, TData>(thing, @in, @out, data, cancellationToken);
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

    public Task<T?> Select<T>(
        StringRecordId recordId,
        CancellationToken cancellationToken = default
    )
    {
        return _engine.Select<T?>(recordId, cancellationToken);
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
        where T : Record
    {
        return _engine.Upsert(data, cancellationToken);
    }

    public Task<TOutput> Upsert<TData, TOutput>(
        StringRecordId recordId,
        TData data,
        CancellationToken cancellationToken = default
    )
        where TOutput : Record
    {
        return _engine.Upsert<TData, TOutput>(recordId, data, cancellationToken);
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
