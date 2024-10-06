﻿using Dahomey.Cbor;
using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Net.Extensions.DependencyInjection;
using SurrealDb.Net.Internals.Models.LiveQuery;
using SurrealDb.Net.Models;
using SurrealDb.Net.Models.Auth;
using SurrealDb.Net.Models.LiveQuery;
using SurrealDb.Net.Models.Response;
using SystemTextJsonPatch;

namespace SurrealDb.Net.Internals;

public interface ISurrealDbEngine : IDisposable
{
    Task Authenticate(Jwt jwt, CancellationToken cancellationToken);
    Task Connect(CancellationToken cancellationToken);
    Task<T> Create<T>(T data, CancellationToken cancellationToken)
        where T : Record;
    Task<T> Create<T>(string table, T? data, CancellationToken cancellationToken);
    Task<TOutput> Create<TData, TOutput>(
        StringRecordId recordId,
        TData? data,
        CancellationToken cancellationToken
    )
        where TOutput : Record;
    Task Delete(string table, CancellationToken cancellationToken);
    Task<bool> Delete(RecordId recordId, CancellationToken cancellationToken);
    Task<bool> Delete(StringRecordId recordId, CancellationToken cancellationToken);
    Task<bool> Health(CancellationToken cancellationToken);
    Task<T> Info<T>(CancellationToken cancellationToken);
    Task<IEnumerable<T>> Insert<T>(
        string table,
        IEnumerable<T> data,
        CancellationToken cancellationToken
    )
        where T : Record;
    Task<T> InsertRelation<T>(T data, CancellationToken cancellationToken)
        where T : RelationRecord;
    Task<T> InsertRelation<T>(string table, T data, CancellationToken cancellationToken)
        where T : RelationRecord;
    Task Invalidate(CancellationToken cancellationToken);
    Task Kill(
        Guid queryUuid,
        SurrealDbLiveQueryClosureReason reason,
        CancellationToken cancellationToken
    );
    SurrealDbLiveQuery<T> ListenLive<T>(Guid queryUuid);
    Task<SurrealDbLiveQuery<T>> LiveRawQuery<T>(
        string query,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken
    );
    Task<SurrealDbLiveQuery<T>> LiveTable<T>(
        string table,
        bool diff,
        CancellationToken cancellationToken
    );
    Task<TOutput> Merge<TMerge, TOutput>(TMerge data, CancellationToken cancellationToken)
        where TMerge : Record;
    Task<T> Merge<T>(
        RecordId recordId,
        Dictionary<string, object> data,
        CancellationToken cancellationToken
    );
    Task<T> Merge<T>(
        StringRecordId recordId,
        Dictionary<string, object> data,
        CancellationToken cancellationToken
    );
    Task<IEnumerable<TOutput>> Merge<TMerge, TOutput>(
        string table,
        TMerge data,
        CancellationToken cancellationToken
    )
        where TMerge : class;
    Task<IEnumerable<T>> Merge<T>(
        string table,
        Dictionary<string, object> data,
        CancellationToken cancellationToken
    );
    Task<T> Patch<T>(
        RecordId recordId,
        JsonPatchDocument<T> patches,
        CancellationToken cancellationToken
    )
        where T : class;
    Task<T> Patch<T>(
        StringRecordId recordId,
        JsonPatchDocument<T> patches,
        CancellationToken cancellationToken
    )
        where T : class;
    Task<IEnumerable<T>> Patch<T>(
        string table,
        JsonPatchDocument<T> patches,
        CancellationToken cancellationToken
    )
        where T : class;
    Task<SurrealDbResponse> RawQuery(
        string query,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken
    );
    Task<IEnumerable<TOutput>> Relate<TOutput, TData>(
        string table,
        IEnumerable<RecordId> ins,
        IEnumerable<RecordId> outs,
        TData? data,
        CancellationToken cancellationToken
    )
        where TOutput : class;
    Task<TOutput> Relate<TOutput, TData>(
        RecordId recordId,
        RecordId @in,
        RecordId @out,
        TData? data,
        CancellationToken cancellationToken
    )
        where TOutput : class;
    Task<T> Run<T>(
        string name,
        string? version,
        object[]? args,
        CancellationToken cancellationToken
    );
    Task<IEnumerable<T>> Select<T>(string table, CancellationToken cancellationToken);
    Task<T?> Select<T>(RecordId recordId, CancellationToken cancellationToken);
    Task<T?> Select<T>(StringRecordId recordId, CancellationToken cancellationToken);
    Task Set(string key, object value, CancellationToken cancellationToken);
    Task SignIn(RootAuth root, CancellationToken cancellationToken);
    Task<Jwt> SignIn(NamespaceAuth nsAuth, CancellationToken cancellationToken);
    Task<Jwt> SignIn(DatabaseAuth dbAuth, CancellationToken cancellationToken);
    Task<Jwt> SignIn<T>(T scopeAuth, CancellationToken cancellationToken)
        where T : ScopeAuth;
    Task<Jwt> SignUp<T>(T scopeAuth, CancellationToken cancellationToken)
        where T : ScopeAuth;
    SurrealDbLiveQueryChannel SubscribeToLiveQuery(Guid id);
    Task Unset(string key, CancellationToken cancellationToken);
    Task<IEnumerable<T>> Update<T>(string table, T data, CancellationToken cancellationToken)
        where T : class;
    Task<T> Upsert<T>(T data, CancellationToken cancellationToken)
        where T : Record;
    Task<TOutput> Upsert<TData, TOutput>(
        StringRecordId recordId,
        TData data,
        CancellationToken cancellationToken
    )
        where TOutput : Record;
    Task Use(string ns, string db, CancellationToken cancellationToken);
    Task<string> Version(CancellationToken cancellationToken);
}

public interface ISurrealDbProviderEngine : ISurrealDbEngine
{
    /// <summary>
    /// Initializes engine dynamically, due to DependencyInjection interop.
    /// </summary>
    void Initialize(
        SurrealDbOptions configuration,
        Action<CborOptions>? configureCborOptions,
        ISurrealDbLoggerFactory? surrealDbLoggerFactory
    );
}

public interface ISurrealDbInMemoryEngine : ISurrealDbProviderEngine { }
