using System.Collections.Immutable;
using System.Text;
using Semver;
using SurrealDb.Net.Internals;
using SurrealDb.Net.Models;
using SurrealDb.Net.Models.Auth;
using SurrealDb.Net.Models.LiveQuery;
using SurrealDb.Net.Models.Response;
#if NET10_0_OR_GREATER
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
#else
using SystemTextJsonPatch;
#endif
#if NET6_0_OR_GREATER
using SurrealDb.Net.Handlers;
#else
using SurrealDb.Net.Internals.Extensions;
#endif

namespace SurrealDb.Net;

public abstract partial class BaseSurrealDbClient
{
    public Task Authenticate(Tokens tokens, CancellationToken cancellationToken = default)
    {
        return Engine.Authenticate(tokens, cancellationToken);
    }

    public Task Connect(CancellationToken cancellationToken = default)
    {
        return Engine.Connect(cancellationToken);
    }

    public Task<T> Create<T>(T data, CancellationToken cancellationToken = default)
        where T : IRecord
    {
        return Engine.Create(data, cancellationToken);
    }

    public Task<T> Create<T>(
        string table,
        T? data = default,
        CancellationToken cancellationToken = default
    )
    {
        return Engine.Create(table, data, cancellationToken);
    }

    public Task<TOutput> Create<TData, TOutput>(
        StringRecordId recordId,
        TData? data = default,
        CancellationToken cancellationToken = default
    )
        where TOutput : IRecord
    {
        return Engine.Create<TData, TOutput>(recordId, data, cancellationToken);
    }

    public Task Delete(string table, CancellationToken cancellationToken = default)
    {
        return Engine.Delete(table, cancellationToken);
    }

    public Task<bool> Delete(RecordId recordId, CancellationToken cancellationToken = default)
    {
        return Engine.Delete(recordId, cancellationToken);
    }

    public Task<bool> Delete(StringRecordId recordId, CancellationToken cancellationToken = default)
    {
        return Engine.Delete(recordId, cancellationToken);
    }

    public void Dispose()
    {
        Engine.Dispose();
    }

    public async ValueTask DisposeAsync()
    {
        if (_poolTask is not null)
        {
            // 💡 Prevent engine disposal as it will be reused in an object pool
            await _poolTask.Invoke().ConfigureAwait(false);
            return;
        }

        await Engine.DisposeAsync();
    }

    public async Task<string> Export(
        ExportOptions? options = default,
        CancellationToken cancellationToken = default
    )
    {
        if (Engine is ISurrealDbProviderEngine providerEngine)
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

        using var wrapper = await CreateCommonHttpWrapperAsync(cancellationToken)
            .ConfigureAwait(false);

        using var httpContent = SurrealDbHttpEngine.CreateBodyContent(
            wrapper.ConfigureCborOptions,
            options ?? new(),
            null
        );

        bool shouldUsePostRequest =
            wrapper.Version is not null
            && wrapper.Version.Satisfies(SemVersionRange.AtLeast(new(2, 1), true));

        using var response = shouldUsePostRequest
            ? await wrapper.HttpClient.PostAsync(exportUri, httpContent, cancellationToken)
            : await wrapper.HttpClient.GetAsync(exportUri, cancellationToken);
        response.EnsureSuccessStatusCode();

#if NET6_0_OR_GREATER
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
        return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif
    }

    public Task<bool> Health(CancellationToken cancellationToken = default)
    {
        return Engine.Health(cancellationToken);
    }

    public async Task Import(string input, CancellationToken cancellationToken = default)
    {
        if (Engine is ISurrealDbProviderEngine providerEngine)
        {
            await providerEngine.Import(input, cancellationToken).ConfigureAwait(false);
            return;
        }

        const string path = "/import";

        var importUri = Uri.Scheme switch
        {
            "http" or "https" => new Uri(Uri, path),
            "ws" => new Uri(new Uri(Uri.AbsoluteUri.Replace("ws://", "http://")), path),
            "wss" => new Uri(new Uri(Uri.AbsoluteUri.Replace("wss://", "https://")), path),
            _ => throw new NotImplementedException(),
        };

        using var wrapper = await CreateCommonHttpWrapperAsync(cancellationToken)
            .ConfigureAwait(false);

        using var httpContent = new StringContent(input, Encoding.UTF8, "plain/text");

        using var response = await wrapper
            .HttpClient.PostAsync(importUri, httpContent, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    public Task<T> Info<T>(CancellationToken cancellationToken = default)
    {
        return Engine.Info<T>(cancellationToken);
    }

    public Task<IEnumerable<T>> Insert<T>(
        string table,
        IEnumerable<T> data,
        CancellationToken cancellationToken = default
    )
        where T : IRecord
    {
        return Engine.Insert(table, data, cancellationToken);
    }

    public Task<T> InsertRelation<T>(T data, CancellationToken cancellationToken = default)
        where T : IRelationRecord
    {
        return Engine.InsertRelation(data, cancellationToken);
    }

    public Task<T> InsertRelation<T>(
        string table,
        T data,
        CancellationToken cancellationToken = default
    )
        where T : IRelationRecord
    {
        return Engine.InsertRelation(table, data, cancellationToken);
    }

    public Task Invalidate(CancellationToken cancellationToken = default)
    {
        return Engine.Invalidate(cancellationToken);
    }

    public Task Kill(Guid queryUuid, CancellationToken cancellationToken = default)
    {
        return Engine.Kill(
            queryUuid,
            SurrealDbLiveQueryClosureReason.QueryKilled,
            cancellationToken
        );
    }

    public SurrealDbLiveQuery<T> ListenLive<T>(Guid queryUuid)
    {
        return Engine.ListenLive<T>(queryUuid);
    }

#if NET6_0_OR_GREATER
    public Task<SurrealDbLiveQuery<T>> LiveQuery<T>(
        QueryInterpolatedStringHandler query,
        CancellationToken cancellationToken = default
    )
    {
        return Engine.LiveRawQuery<T>(query.FormattedText, query.Parameters, cancellationToken);
    }
#else
    public Task<SurrealDbLiveQuery<T>> LiveQuery<T>(
        FormattableString query,
        CancellationToken cancellationToken = default
    )
    {
        var (formattedQuery, parameters) = query.ExtractRawQueryParams();
        return Engine.LiveRawQuery<T>(formattedQuery, parameters, cancellationToken);
    }
#endif

    public Task<SurrealDbLiveQuery<T>> LiveRawQuery<T>(
        string query,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default
    )
    {
        return Engine.LiveRawQuery<T>(
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
        return Engine.LiveTable<T>(table, diff, cancellationToken);
    }

    public Task<TOutput> Merge<TMerge, TOutput>(
        TMerge data,
        CancellationToken cancellationToken = default
    )
        where TMerge : IRecord
    {
        return Engine.Merge<TMerge, TOutput>(data, cancellationToken);
    }

    public Task<T> Merge<T>(
        RecordId recordId,
        Dictionary<string, object> data,
        CancellationToken cancellationToken = default
    )
    {
        return Engine.Merge<T>(recordId, data, cancellationToken);
    }

    public Task<T> Merge<T>(
        StringRecordId recordId,
        Dictionary<string, object> data,
        CancellationToken cancellationToken = default
    )
    {
        return Engine.Merge<T>(recordId, data, cancellationToken);
    }

    public Task<IEnumerable<TOutput>> Merge<TMerge, TOutput>(
        string table,
        TMerge data,
        CancellationToken cancellationToken = default
    )
        where TMerge : class
    {
        return Engine.Merge<TMerge, TOutput>(table, data, cancellationToken);
    }

    public Task<IEnumerable<T>> Merge<T>(
        string table,
        Dictionary<string, object> data,
        CancellationToken cancellationToken = default
    )
    {
        return Engine.Merge<T>(table, data, cancellationToken);
    }

    public Task<T> Patch<T>(
        RecordId recordId,
        JsonPatchDocument<T> patches,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        return Engine.Patch(recordId, patches, cancellationToken);
    }

    public Task<T> Patch<T>(
        StringRecordId recordId,
        JsonPatchDocument<T> patches,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        return Engine.Patch(recordId, patches, cancellationToken);
    }

    public Task<IEnumerable<T>> Patch<T>(
        string table,
        JsonPatchDocument<T> patches,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        return Engine.Patch(table, patches, cancellationToken);
    }

#if NET6_0_OR_GREATER
    public Task<SurrealDbResponse> Query(
        QueryInterpolatedStringHandler query,
        CancellationToken cancellationToken = default
    )
    {
        return Engine.RawQuery(query.FormattedText, query.Parameters, cancellationToken);
    }
#else
    public Task<SurrealDbResponse> Query(
        FormattableString query,
        CancellationToken cancellationToken = default
    )
    {
        var (formattedQuery, parameters) = query.ExtractRawQueryParams();
        return Engine.RawQuery(formattedQuery, parameters, cancellationToken);
    }
#endif

    public Task<SurrealDbResponse> RawQuery(
        string query,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default
    )
    {
        return Engine.RawQuery(
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
        var outputs = await Engine
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
        var outputs = await Engine
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
        return Engine.Relate<TOutput, object>(table, ins, new[] { @out }, null, cancellationToken);
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
        return Engine.Relate<TOutput, TData>(table, ins, new[] { @out }, data, cancellationToken);
    }

    public Task<IEnumerable<TOutput>> Relate<TOutput>(
        string table,
        RecordId @in,
        IEnumerable<RecordId> outs,
        CancellationToken cancellationToken = default
    )
        where TOutput : class
    {
        return Engine.Relate<TOutput, object>(table, new[] { @in }, outs, null, cancellationToken);
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
        return Engine.Relate<TOutput, TData>(table, new[] { @in }, outs, data, cancellationToken);
    }

    public Task<IEnumerable<TOutput>> Relate<TOutput>(
        string table,
        IEnumerable<RecordId> ins,
        IEnumerable<RecordId> outs,
        CancellationToken cancellationToken = default
    )
        where TOutput : class
    {
        return Engine.Relate<TOutput, object>(table, ins, outs, null, cancellationToken);
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
        return Engine.Relate<TOutput, TData>(table, ins, outs, data, cancellationToken);
    }

    public Task<TOutput> Relate<TOutput>(
        RecordId recordId,
        RecordId @in,
        RecordId @out,
        CancellationToken cancellationToken = default
    )
        where TOutput : class
    {
        return Engine.Relate<TOutput, object>(recordId, @in, @out, null, cancellationToken);
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
        return Engine.Relate<TOutput, TData>(recordId, @in, @out, data, cancellationToken);
    }

    public Task<T> Run<T>(
        string name,
        object[]? args = null,
        CancellationToken cancellationToken = default
    )
    {
        return Engine.Run<T>(name, null, args, cancellationToken);
    }

    public Task<T> Run<T>(
        string name,
        string version,
        object[]? args = null,
        CancellationToken cancellationToken = default
    )
    {
        return Engine.Run<T>(name, version, args, cancellationToken);
    }

    public Task<IEnumerable<T>> Select<T>(
        string table,
        CancellationToken cancellationToken = default
    )
    {
        return Engine.Select<T>(table, cancellationToken);
    }

    public Task<T?> Select<T>(RecordId recordId, CancellationToken cancellationToken = default)
    {
        return Engine.Select<T?>(recordId, cancellationToken);
    }

    public Task<T?> Select<T>(
        StringRecordId recordId,
        CancellationToken cancellationToken = default
    )
    {
        return Engine.Select<T?>(recordId, cancellationToken);
    }

    public Task<IEnumerable<TOutput>> Select<TStart, TEnd, TOutput>(
        RecordIdRange<TStart, TEnd> recordIdRange,
        CancellationToken cancellationToken = default
    )
    {
        return Engine.Select<TStart, TEnd, TOutput>(recordIdRange, cancellationToken);
    }

    public Task Set(string key, object value, CancellationToken cancellationToken = default)
    {
        return Engine.Set(key, value, cancellationToken);
    }

    public Task SignIn(RootAuth root, CancellationToken cancellationToken = default)
    {
        return Engine.SignIn(root, cancellationToken);
    }

    public Task<Tokens> SignIn(NamespaceAuth nsAuth, CancellationToken cancellationToken = default)
    {
        return Engine.SignIn(nsAuth, cancellationToken);
    }

    public Task<Tokens> SignIn(DatabaseAuth dbAuth, CancellationToken cancellationToken = default)
    {
        return Engine.SignIn(dbAuth, cancellationToken);
    }

    public Task<Tokens> SignIn<T>(T scopeAuth, CancellationToken cancellationToken = default)
        where T : ScopeAuth
    {
        return Engine.SignIn(scopeAuth, cancellationToken);
    }

    public Task<Tokens> SignUp<T>(T scopeAuth, CancellationToken cancellationToken = default)
        where T : ScopeAuth
    {
        return Engine.SignUp(scopeAuth, cancellationToken);
    }

    public Task Unset(string key, CancellationToken cancellationToken = default)
    {
        return Engine.Unset(key, cancellationToken);
    }

    public Task<T> Update<T>(T data, CancellationToken cancellationToken = default)
        where T : IRecord
    {
        return Engine.Update(data, cancellationToken);
    }

    public Task<TOutput> Update<TData, TOutput>(
        StringRecordId recordId,
        TData data,
        CancellationToken cancellationToken = default
    )
        where TOutput : IRecord
    {
        return Engine.Update<TData, TOutput>(recordId, data, cancellationToken);
    }

    public Task<IEnumerable<T>> Update<T>(
        string table,
        T data,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        return Engine.Update(table, data, cancellationToken);
    }

    public Task<IEnumerable<TOutput>> Update<TData, TOutput>(
        string table,
        TData data,
        CancellationToken cancellationToken = default
    )
        where TOutput : IRecord
    {
        return Engine.Update<TData, TOutput>(table, data, cancellationToken);
    }

    public Task<TOutput> Update<TData, TOutput>(
        RecordId recordId,
        TData data,
        CancellationToken cancellationToken = default
    )
        where TOutput : IRecord
    {
        return Engine.Update<TData, TOutput>(recordId, data, cancellationToken);
    }

    public Task<T> Upsert<T>(T data, CancellationToken cancellationToken = default)
        where T : IRecord
    {
        return Engine.Upsert(data, cancellationToken);
    }

    public Task<TOutput> Upsert<TData, TOutput>(
        StringRecordId recordId,
        TData data,
        CancellationToken cancellationToken = default
    )
        where TOutput : IRecord
    {
        return Engine.Upsert<TData, TOutput>(recordId, data, cancellationToken);
    }

    public Task<IEnumerable<T>> Upsert<T>(
        string table,
        T data,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        return Engine.Upsert(table, data, cancellationToken);
    }

    public Task<IEnumerable<TOutput>> Upsert<TData, TOutput>(
        string table,
        TData data,
        CancellationToken cancellationToken = default
    )
        where TOutput : IRecord
    {
        return Engine.Upsert<TData, TOutput>(table, data, cancellationToken);
    }

    public Task<TOutput> Upsert<TData, TOutput>(
        RecordId recordId,
        TData data,
        CancellationToken cancellationToken = default
    )
        where TOutput : IRecord
    {
        return Engine.Upsert<TData, TOutput>(recordId, data, cancellationToken);
    }

    public Task Use(string ns, string db, CancellationToken cancellationToken = default)
    {
        return Engine.Use(ns, db, cancellationToken);
    }

    public Task<string> Version(CancellationToken cancellationToken = default)
    {
        return Engine.Version(cancellationToken);
    }
}
