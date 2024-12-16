using System.Collections.Immutable;
using Dahomey.Cbor;
using Semver;
using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Internals;
using SurrealDb.Net.Internals.Auth;
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

namespace SurrealDb.Net;

public abstract partial class BaseSurrealDbClient
{
    public Task Authenticate(Jwt jwt, CancellationToken cancellationToken = default)
    {
        return _engine.Authenticate(jwt, cancellationToken);
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

    public Task<TOutput> Create<TData, TOutput>(
        StringRecordId recordId,
        TData? data = default,
        CancellationToken cancellationToken = default
    )
        where TOutput : IRecord
    {
        return _engine.Create<TData, TOutput>(recordId, data, cancellationToken);
    }

    public Task Delete(string table, CancellationToken cancellationToken = default)
    {
        return _engine.Delete(table, cancellationToken);
    }

    public Task<bool> Delete(RecordId recordId, CancellationToken cancellationToken = default)
    {
        return _engine.Delete(recordId, cancellationToken);
    }

    public Task<bool> Delete(StringRecordId recordId, CancellationToken cancellationToken = default)
    {
        return _engine.Delete(recordId, cancellationToken);
    }

    public void Dispose()
    {
        _engine.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_poolTask is not null)
        {
            // 💡 Prevent engine disposal as it will be reuse in an object pool
            await _poolTask.Invoke().ConfigureAwait(false);
            return;
        }

        Dispose();
    }

    public async Task<string> Export(
        ExportOptions? options = default,
        CancellationToken cancellationToken = default
    )
    {
        if (_engine is ISurrealDbProviderEngine providerEngine)
        {
            return await providerEngine.Export(options, cancellationToken).ConfigureAwait(false);
        }

        const string path = "/export";

        var exportUri = Uri.Scheme switch
        {
            "http" or "https" => new Uri(Uri, path),
            "ws" => new Uri(new Uri(Uri.AbsoluteUri.Replace("ws://", "http://")), path),
            "wss" => new Uri(new Uri(Uri.AbsoluteUri.Replace("wss://", "https://")), path),
            _ => throw new NotImplementedException(),
        };

        SemVersion? version;
        string? ns;
        string? db;
        IAuth? auth;
        Action<CborOptions>? configureCborOptions;

        switch (_engine)
        {
            case SurrealDbHttpEngine httpEngine:
                // 💡 Ensures underlying engine is started to retrieve some information
                await httpEngine.Connect(cancellationToken).ConfigureAwait(false);

                version = httpEngine._version;
                ns = httpEngine._config.Ns;
                db = httpEngine._config.Db;
                auth = httpEngine._config.Auth;
                configureCborOptions = httpEngine._configureCborOptions;
                break;
            case SurrealDbWsEngine wsEngine:
                // 💡 Ensures underlying engine is started to retrieve some information
                await wsEngine.InternalConnectAsync(true, cancellationToken).ConfigureAwait(false);

                version = wsEngine._version;
                ns = wsEngine._config.Ns;
                db = wsEngine._config.Db;
                auth = wsEngine._config.Auth;
                configureCborOptions = wsEngine._configureCborOptions;
                break;
            default:
                throw new SurrealDbException("No underlying engine is started.");
        }

        if (string.IsNullOrWhiteSpace(ns))
        {
            throw new SurrealDbException("Namespace should be provided to export data.");
        }
        if (string.IsNullOrWhiteSpace(db))
        {
            throw new SurrealDbException("Database should be provided to export data.");
        }

        using var httpContent = SurrealDbHttpEngine.CreateBodyContent(
            null,
            configureCborOptions,
            options ?? new()
        );

        using var httpClient = new HttpClient();

        SurrealDbHttpEngine.SetNsDbHttpClientHeaders(httpClient, version, ns, db);
        SurrealDbHttpEngine.SetAuthHttpClientHeaders(httpClient, auth);

        bool shouldUsePostRequest = version is { Major: >= 2, Minor: >= 1 };

        var httpRequestTask = shouldUsePostRequest
            ? httpClient.PostAsync(exportUri, httpContent, cancellationToken)
            : httpClient.GetAsync(exportUri, cancellationToken);

        using var response = await httpRequestTask.ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

#if NET6_0_OR_GREATER
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
    }

    public Task<bool> Health(CancellationToken cancellationToken = default)
    {
        return _engine.Health(cancellationToken);
    }

    public Task<T> Info<T>(CancellationToken cancellationToken = default)
    {
        return _engine.Info<T>(cancellationToken);
    }

    public Task<IEnumerable<T>> Insert<T>(
        string table,
        IEnumerable<T> data,
        CancellationToken cancellationToken = default
    )
        where T : IRecord
    {
        return _engine.Insert(table, data, cancellationToken);
    }

    public Task<T> InsertRelation<T>(T data, CancellationToken cancellationToken = default)
        where T : RelationRecord
    {
        return _engine.InsertRelation(data, cancellationToken);
    }

    public Task<T> InsertRelation<T>(
        string table,
        T data,
        CancellationToken cancellationToken = default
    )
        where T : RelationRecord
    {
        return _engine.InsertRelation(table, data, cancellationToken);
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

#if NET6_0_OR_GREATER
    public Task<SurrealDbLiveQuery<T>> LiveQuery<T>(
        QueryInterpolatedStringHandler query,
        CancellationToken cancellationToken = default
    )
    {
        return _engine.LiveRawQuery<T>(query.FormattedText, query.Parameters, cancellationToken);
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
        RecordId recordId,
        Dictionary<string, object> data,
        CancellationToken cancellationToken = default
    )
    {
        return _engine.Merge<T>(recordId, data, cancellationToken);
    }

    public Task<T> Merge<T>(
        StringRecordId recordId,
        Dictionary<string, object> data,
        CancellationToken cancellationToken = default
    )
    {
        return _engine.Merge<T>(recordId, data, cancellationToken);
    }

    public Task<IEnumerable<TOutput>> Merge<TMerge, TOutput>(
        string table,
        TMerge data,
        CancellationToken cancellationToken = default
    )
        where TMerge : class
    {
        return _engine.Merge<TMerge, TOutput>(table, data, cancellationToken);
    }

    public Task<IEnumerable<T>> Merge<T>(
        string table,
        Dictionary<string, object> data,
        CancellationToken cancellationToken = default
    )
    {
        return _engine.Merge<T>(table, data, cancellationToken);
    }

    public Task<T> Patch<T>(
        RecordId recordId,
        JsonPatchDocument<T> patches,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        return _engine.Patch(recordId, patches, cancellationToken);
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

    public Task<IEnumerable<T>> Patch<T>(
        string table,
        JsonPatchDocument<T> patches,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        return _engine.Patch(table, patches, cancellationToken);
    }

#if NET6_0_OR_GREATER
    public Task<SurrealDbResponse> Query(
        QueryInterpolatedStringHandler query,
        CancellationToken cancellationToken = default
    )
    {
        return _engine.RawQuery(query.FormattedText, query.Parameters, cancellationToken);
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
        RecordId @in,
        RecordId @out,
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
        RecordId @in,
        RecordId @out,
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
        IEnumerable<RecordId> ins,
        RecordId @out,
        CancellationToken cancellationToken = default
    )
        where TOutput : class
    {
        return _engine.Relate<TOutput, object>(table, ins, new[] { @out }, null, cancellationToken);
    }

    public Task<IEnumerable<TOutput>> Relate<TOutput, TData>(
        string table,
        IEnumerable<RecordId> ins,
        RecordId @out,
        TData? data,
        CancellationToken cancellationToken = default
    )
        where TOutput : class
    {
        return _engine.Relate<TOutput, TData>(table, ins, new[] { @out }, data, cancellationToken);
    }

    public Task<IEnumerable<TOutput>> Relate<TOutput>(
        string table,
        RecordId @in,
        IEnumerable<RecordId> outs,
        CancellationToken cancellationToken = default
    )
        where TOutput : class
    {
        return _engine.Relate<TOutput, object>(table, new[] { @in }, outs, null, cancellationToken);
    }

    public Task<IEnumerable<TOutput>> Relate<TOutput, TData>(
        string table,
        RecordId @in,
        IEnumerable<RecordId> outs,
        TData? data,
        CancellationToken cancellationToken = default
    )
        where TOutput : class
    {
        return _engine.Relate<TOutput, TData>(table, new[] { @in }, outs, data, cancellationToken);
    }

    public Task<IEnumerable<TOutput>> Relate<TOutput>(
        string table,
        IEnumerable<RecordId> ins,
        IEnumerable<RecordId> outs,
        CancellationToken cancellationToken = default
    )
        where TOutput : class
    {
        return _engine.Relate<TOutput, object>(table, ins, outs, null, cancellationToken);
    }

    public Task<IEnumerable<TOutput>> Relate<TOutput, TData>(
        string table,
        IEnumerable<RecordId> ins,
        IEnumerable<RecordId> outs,
        TData? data,
        CancellationToken cancellationToken = default
    )
        where TOutput : class
    {
        return _engine.Relate<TOutput, TData>(table, ins, outs, data, cancellationToken);
    }

    public Task<TOutput> Relate<TOutput>(
        RecordId recordId,
        RecordId @in,
        RecordId @out,
        CancellationToken cancellationToken = default
    )
        where TOutput : class
    {
        return _engine.Relate<TOutput, object>(recordId, @in, @out, null, cancellationToken);
    }

    public Task<TOutput> Relate<TOutput, TData>(
        RecordId recordId,
        RecordId @in,
        RecordId @out,
        TData? data,
        CancellationToken cancellationToken = default
    )
        where TOutput : class
    {
        return _engine.Relate<TOutput, TData>(recordId, @in, @out, data, cancellationToken);
    }

    public Task<T> Run<T>(
        string name,
        object[]? args = null,
        CancellationToken cancellationToken = default
    )
    {
        return _engine.Run<T>(name, null, args, cancellationToken);
    }

    public Task<T> Run<T>(
        string name,
        string version,
        object[]? args = null,
        CancellationToken cancellationToken = default
    )
    {
        return _engine.Run<T>(name, version, args, cancellationToken);
    }

    public Task<IEnumerable<T>> Select<T>(
        string table,
        CancellationToken cancellationToken = default
    )
    {
        return _engine.Select<T>(table, cancellationToken);
    }

    public Task<T?> Select<T>(RecordId recordId, CancellationToken cancellationToken = default)
    {
        return _engine.Select<T?>(recordId, cancellationToken);
    }

    public Task<T?> Select<T>(
        StringRecordId recordId,
        CancellationToken cancellationToken = default
    )
    {
        return _engine.Select<T?>(recordId, cancellationToken);
    }

    public Task<IEnumerable<TOutput>> Select<TStart, TEnd, TOutput>(
        RecordIdRange<TStart, TEnd> recordIdRange,
        CancellationToken cancellationToken = default
    )
    {
        return _engine.Select<TStart, TEnd, TOutput>(recordIdRange, cancellationToken);
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

    public Task<T> Update<T>(T data, CancellationToken cancellationToken = default)
        where T : IRecord
    {
        return _engine.Update(data, cancellationToken);
    }

    public Task<TOutput> Update<TData, TOutput>(
        StringRecordId recordId,
        TData data,
        CancellationToken cancellationToken = default
    )
        where TOutput : IRecord
    {
        return _engine.Update<TData, TOutput>(recordId, data, cancellationToken);
    }

    public Task<IEnumerable<T>> Update<T>(
        string table,
        T data,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        return _engine.Update(table, data, cancellationToken);
    }

    public Task<TOutput> Update<TData, TOutput>(
        RecordId recordId,
        TData data,
        CancellationToken cancellationToken = default
    )
        where TOutput : IRecord
    {
        return _engine.Update<TData, TOutput>(recordId, data, cancellationToken);
    }

    public Task<T> Upsert<T>(T data, CancellationToken cancellationToken = default)
        where T : IRecord
    {
        return _engine.Upsert(data, cancellationToken);
    }

    public Task<TOutput> Upsert<TData, TOutput>(
        StringRecordId recordId,
        TData data,
        CancellationToken cancellationToken = default
    )
        where TOutput : IRecord
    {
        return _engine.Upsert<TData, TOutput>(recordId, data, cancellationToken);
    }

    public Task<IEnumerable<T>> Upsert<T>(
        string table,
        T data,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        return _engine.Upsert(table, data, cancellationToken);
    }

    public Task<TOutput> Upsert<TData, TOutput>(
        RecordId recordId,
        TData data,
        CancellationToken cancellationToken = default
    )
        where TOutput : IRecord
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
