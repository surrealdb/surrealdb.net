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
    public Task Authenticate(Jwt jwt, CancellationToken cancellationToken = default)
    {
        return RunWithTelemetryAsync(Engine.Authenticate(jwt, cancellationToken), "authenticate");
    }

    public Task Connect(CancellationToken cancellationToken = default)
    {
        return RunWithTelemetryAsync(Engine.Connect(cancellationToken), "connect");
    }

    public Task<T> Create<T>(T data, CancellationToken cancellationToken = default)
        where T : IRecord
    {
        return RunWithTelemetryAsync(Engine.Create(data, cancellationToken), "create");
    }

    public Task<T> Create<T>(
        string table,
        T? data = default,
        CancellationToken cancellationToken = default
    )
    {
        return RunWithTelemetryAsync(
            Engine.Create(table, data, cancellationToken),
            "create",
            table
        );
    }

    public Task<TOutput> Create<TData, TOutput>(
        StringRecordId recordId,
        TData? data = default,
        CancellationToken cancellationToken = default
    )
        where TOutput : IRecord
    {
        return RunWithTelemetryAsync(
            Engine.Create<TData, TOutput>(recordId, data, cancellationToken),
            "create"
        );
    }

    public Task Delete(string table, CancellationToken cancellationToken = default)
    {
        return RunWithTelemetryAsync(Engine.Delete(table, cancellationToken), "delete", table);
    }

    public Task<bool> Delete(RecordId recordId, CancellationToken cancellationToken = default)
    {
        return RunWithTelemetryAsync(
            Engine.Delete(recordId, cancellationToken),
            "delete",
            recordId.Table
        );
    }

    public Task<bool> Delete(StringRecordId recordId, CancellationToken cancellationToken = default)
    {
        return RunWithTelemetryAsync(Engine.Delete(recordId, cancellationToken), "delete");
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

    public Task<string> Export(
        ExportOptions? options = null,
        CancellationToken cancellationToken = default
    )
    {
        return RunWithTelemetryAsync(InternalExport(options, cancellationToken), "export");
    }

    private async Task<string> InternalExport(
        ExportOptions? options,
        CancellationToken cancellationToken
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
        return RunWithTelemetryAsync(Engine.Health(cancellationToken), "health");
    }

    public Task Import(string input, CancellationToken cancellationToken = default)
    {
        return RunWithTelemetryAsync(InternalImport(input, cancellationToken), "import");
    }

    private async Task InternalImport(string input, CancellationToken cancellationToken)
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
        return RunWithTelemetryAsync(Engine.Info<T>(cancellationToken), "info");
    }

    public Task<IEnumerable<T>> Insert<T>(
        string table,
        IEnumerable<T> data,
        CancellationToken cancellationToken = default
    )
        where T : IRecord
    {
        return RunWithTelemetryAsync(
            Engine.Insert(table, data, cancellationToken),
            "insert",
            table
        );
    }

    public Task<T> InsertRelation<T>(T data, CancellationToken cancellationToken = default)
        where T : IRelationRecord
    {
        return RunWithTelemetryAsync(
            Engine.InsertRelation(data, cancellationToken),
            "insertrelation"
        );
    }

    public Task<T> InsertRelation<T>(
        string table,
        T data,
        CancellationToken cancellationToken = default
    )
        where T : IRelationRecord
    {
        return RunWithTelemetryAsync(
            Engine.InsertRelation(table, data, cancellationToken),
            "insertrelation",
            table
        );
    }

    public Task Invalidate(CancellationToken cancellationToken = default)
    {
        return RunWithTelemetryAsync(Engine.Invalidate(cancellationToken), "invalidate");
    }

    public Task Kill(Guid queryUuid, CancellationToken cancellationToken = default)
    {
        return RunWithTelemetryAsync(
            Engine.Kill(queryUuid, SurrealDbLiveQueryClosureReason.QueryKilled, cancellationToken),
            "kill"
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
        return RunWithTelemetryAsync(
            Engine.Merge<TMerge, TOutput>(data, cancellationToken),
            "merge"
        );
    }

    public Task<T> Merge<T>(
        RecordId recordId,
        Dictionary<string, object> data,
        CancellationToken cancellationToken = default
    )
    {
        return RunWithTelemetryAsync(
            Engine.Merge<T>(recordId, data, cancellationToken),
            "merge",
            recordId.Table
        );
    }

    public Task<T> Merge<T>(
        StringRecordId recordId,
        Dictionary<string, object> data,
        CancellationToken cancellationToken = default
    )
    {
        return RunWithTelemetryAsync(Engine.Merge<T>(recordId, data, cancellationToken), "merge");
    }

    public Task<IEnumerable<TOutput>> Merge<TMerge, TOutput>(
        string table,
        TMerge data,
        CancellationToken cancellationToken = default
    )
        where TMerge : class
    {
        return RunWithTelemetryAsync(
            Engine.Merge<TMerge, TOutput>(table, data, cancellationToken),
            "merge",
            table
        );
    }

    public Task<IEnumerable<T>> Merge<T>(
        string table,
        Dictionary<string, object> data,
        CancellationToken cancellationToken = default
    )
    {
        return RunWithTelemetryAsync(
            Engine.Merge<T>(table, data, cancellationToken),
            "merge",
            table
        );
    }

    public Task<T> Patch<T>(
        RecordId recordId,
        JsonPatchDocument<T> patches,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        return RunWithTelemetryAsync(
            Engine.Patch(recordId, patches, cancellationToken),
            "patch",
            recordId.Table
        );
    }

    public Task<T> Patch<T>(
        StringRecordId recordId,
        JsonPatchDocument<T> patches,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        return RunWithTelemetryAsync(Engine.Patch(recordId, patches, cancellationToken), "patch");
    }

    public Task<IEnumerable<T>> Patch<T>(
        string table,
        JsonPatchDocument<T> patches,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        return RunWithTelemetryAsync(
            Engine.Patch(table, patches, cancellationToken),
            "patch",
            table
        );
    }

#if NET6_0_OR_GREATER
    public Task<SurrealDbResponse> Query(
        QueryInterpolatedStringHandler query,
        CancellationToken cancellationToken = default
    )
    {
        return RunQueryWithTelemetryAsync(
            Engine.RawQuery(query.FormattedText, query.Parameters, cancellationToken),
            query.FormattedText,
            query.Parameters
        );
    }
#else
    public Task<SurrealDbResponse> Query(
        FormattableString query,
        CancellationToken cancellationToken = default
    )
    {
        var (formattedQuery, parameters) = query.ExtractRawQueryParams();
        return RunQueryWithTelemetryAsync(
            Engine.RawQuery(formattedQuery, parameters, cancellationToken),
            formattedQuery,
            parameters
        );
    }
#endif

    public Task<SurrealDbResponse> RawQuery(
        string query,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default
    )
    {
        var @params = parameters ?? ImmutableDictionary<string, object?>.Empty;
        return RunQueryWithTelemetryAsync(
            Engine.RawQuery(query, @params, cancellationToken),
            query,
            @params
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
        var task = Engine.Relate<TOutput, object>(
            table,
            new[] { @in },
            new[] { @out },
            null,
            cancellationToken
        );

        var outputs = await RunWithTelemetryAsync(task, "relate", table).ConfigureAwait(false);
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
        var task = Engine.Relate<TOutput, TData>(
            table,
            new[] { @in },
            new[] { @out },
            data,
            cancellationToken
        );

        var outputs = await RunWithTelemetryAsync(task, "relate", table).ConfigureAwait(false);
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
        return RunWithTelemetryAsync(
            Engine.Relate<TOutput, object>(table, ins, new[] { @out }, null, cancellationToken),
            "relate",
            table
        );
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
        return RunWithTelemetryAsync(
            Engine.Relate<TOutput, TData>(table, ins, new[] { @out }, data, cancellationToken),
            "relate",
            table
        );
    }

    public Task<IEnumerable<TOutput>> Relate<TOutput>(
        string table,
        RecordId @in,
        IEnumerable<RecordId> outs,
        CancellationToken cancellationToken = default
    )
        where TOutput : class
    {
        return RunWithTelemetryAsync(
            Engine.Relate<TOutput, object>(table, new[] { @in }, outs, null, cancellationToken),
            "relate",
            table
        );
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
        return RunWithTelemetryAsync(
            Engine.Relate<TOutput, TData>(table, new[] { @in }, outs, data, cancellationToken),
            "relate",
            table
        );
    }

    public Task<IEnumerable<TOutput>> Relate<TOutput>(
        string table,
        IEnumerable<RecordId> ins,
        IEnumerable<RecordId> outs,
        CancellationToken cancellationToken = default
    )
        where TOutput : class
    {
        return RunWithTelemetryAsync(
            Engine.Relate<TOutput, object>(table, ins, outs, null, cancellationToken),
            "relate",
            table
        );
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
        return RunWithTelemetryAsync(
            Engine.Relate<TOutput, TData>(table, ins, outs, data, cancellationToken),
            "relate",
            table
        );
    }

    public Task<TOutput> Relate<TOutput>(
        RecordId recordId,
        RecordId @in,
        RecordId @out,
        CancellationToken cancellationToken = default
    )
        where TOutput : class
    {
        return RunWithTelemetryAsync(
            Engine.Relate<TOutput, object>(recordId, @in, @out, null, cancellationToken),
            "relate",
            recordId.Table
        );
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
        return RunWithTelemetryAsync(
            Engine.Relate<TOutput, TData>(recordId, @in, @out, data, cancellationToken),
            "relate",
            recordId.Table
        );
    }

    public Task<T> Run<T>(
        string name,
        object[]? args = null,
        CancellationToken cancellationToken = default
    )
    {
        return RunWithTelemetryAsync(
            Engine.Run<T>(name, null, args, cancellationToken),
            "run",
            name
        );
    }

    public Task<T> Run<T>(
        string name,
        string version,
        object[]? args = null,
        CancellationToken cancellationToken = default
    )
    {
        return RunWithTelemetryAsync(
            Engine.Run<T>(name, version, args, cancellationToken),
            "run",
            name
        );
    }

    public Task<IEnumerable<T>> Select<T>(
        string table,
        CancellationToken cancellationToken = default
    )
    {
        return RunWithTelemetryAsync(Engine.Select<T>(table, cancellationToken), "select", table);
    }

    public Task<T?> Select<T>(RecordId recordId, CancellationToken cancellationToken = default)
    {
        return RunWithTelemetryAsync(
            Engine.Select<T?>(recordId, cancellationToken),
            "select",
            recordId.Table
        );
    }

    public Task<T?> Select<T>(
        StringRecordId recordId,
        CancellationToken cancellationToken = default
    )
    {
        return RunWithTelemetryAsync(Engine.Select<T?>(recordId, cancellationToken), "select");
    }

    public Task<IEnumerable<TOutput>> Select<TStart, TEnd, TOutput>(
        RecordIdRange<TStart, TEnd> recordIdRange,
        CancellationToken cancellationToken = default
    )
    {
        return RunWithTelemetryAsync(
            Engine.Select<TStart, TEnd, TOutput>(recordIdRange, cancellationToken),
            "select"
        );
    }

    public Task Set(string key, object value, CancellationToken cancellationToken = default)
    {
        return RunWithTelemetryAsync(Engine.Set(key, value, cancellationToken), "set");
    }

    public Task SignIn(RootAuth root, CancellationToken cancellationToken = default)
    {
        return RunWithTelemetryAsync(Engine.SignIn(root, cancellationToken), "signin");
    }

    public Task<Jwt> SignIn(NamespaceAuth nsAuth, CancellationToken cancellationToken = default)
    {
        return RunWithTelemetryAsync(Engine.SignIn(nsAuth, cancellationToken), "signin");
    }

    public Task<Jwt> SignIn(DatabaseAuth dbAuth, CancellationToken cancellationToken = default)
    {
        return RunWithTelemetryAsync(Engine.SignIn(dbAuth, cancellationToken), "signin");
    }

    public Task<Jwt> SignIn<T>(T scopeAuth, CancellationToken cancellationToken = default)
        where T : ScopeAuth
    {
        return RunWithTelemetryAsync(Engine.SignIn(scopeAuth, cancellationToken), "signin");
    }

    public Task<Jwt> SignUp<T>(T scopeAuth, CancellationToken cancellationToken = default)
        where T : ScopeAuth
    {
        return RunWithTelemetryAsync(Engine.SignUp(scopeAuth, cancellationToken), "signup");
    }

    public Task Unset(string key, CancellationToken cancellationToken = default)
    {
        return RunWithTelemetryAsync(Engine.Unset(key, cancellationToken), "unset");
    }

    public Task<T> Update<T>(T data, CancellationToken cancellationToken = default)
        where T : IRecord
    {
        return RunWithTelemetryAsync(Engine.Update(data, cancellationToken), "update");
    }

    public Task<TOutput> Update<TData, TOutput>(
        StringRecordId recordId,
        TData data,
        CancellationToken cancellationToken = default
    )
        where TOutput : IRecord
    {
        return RunWithTelemetryAsync(
            Engine.Update<TData, TOutput>(recordId, data, cancellationToken),
            "update"
        );
    }

    public Task<IEnumerable<T>> Update<T>(
        string table,
        T data,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        return RunWithTelemetryAsync(
            Engine.Update(table, data, cancellationToken),
            "update",
            table
        );
    }

    public Task<IEnumerable<TOutput>> Update<TData, TOutput>(
        string table,
        TData data,
        CancellationToken cancellationToken = default
    )
        where TOutput : IRecord
    {
        return RunWithTelemetryAsync(
            Engine.Update<TData, TOutput>(table, data, cancellationToken),
            "update",
            table
        );
    }

    public Task<TOutput> Update<TData, TOutput>(
        RecordId recordId,
        TData data,
        CancellationToken cancellationToken = default
    )
        where TOutput : IRecord
    {
        return RunWithTelemetryAsync(
            Engine.Update<TData, TOutput>(recordId, data, cancellationToken),
            "update",
            recordId.Table
        );
    }

    public Task<T> Upsert<T>(T data, CancellationToken cancellationToken = default)
        where T : IRecord
    {
        return RunWithTelemetryAsync(Engine.Upsert(data, cancellationToken), "upsert");
    }

    public Task<TOutput> Upsert<TData, TOutput>(
        StringRecordId recordId,
        TData data,
        CancellationToken cancellationToken = default
    )
        where TOutput : IRecord
    {
        return RunWithTelemetryAsync(
            Engine.Upsert<TData, TOutput>(recordId, data, cancellationToken),
            "upsert"
        );
    }

    public Task<IEnumerable<T>> Upsert<T>(
        string table,
        T data,
        CancellationToken cancellationToken = default
    )
        where T : class
    {
        return RunWithTelemetryAsync(
            Engine.Upsert(table, data, cancellationToken),
            "upsert",
            table
        );
    }

    public Task<IEnumerable<TOutput>> Upsert<TData, TOutput>(
        string table,
        TData data,
        CancellationToken cancellationToken = default
    )
        where TOutput : IRecord
    {
        return RunWithTelemetryAsync(
            Engine.Upsert<TData, TOutput>(table, data, cancellationToken),
            "upsert",
            table
        );
    }

    public Task<TOutput> Upsert<TData, TOutput>(
        RecordId recordId,
        TData data,
        CancellationToken cancellationToken = default
    )
        where TOutput : IRecord
    {
        return RunWithTelemetryAsync(
            Engine.Upsert<TData, TOutput>(recordId, data, cancellationToken),
            "upsert",
            recordId.Table
        );
    }

    public Task Use(string ns, string db, CancellationToken cancellationToken = default)
    {
        return RunWithTelemetryAsync(Engine.Use(ns, db, cancellationToken), "use");
    }

    public Task<string> Version(CancellationToken cancellationToken = default)
    {
        return RunWithTelemetryAsync(Engine.Version(cancellationToken), "version");
    }
}
