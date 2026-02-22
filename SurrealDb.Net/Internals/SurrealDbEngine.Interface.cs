using Dahomey.Cbor;
using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Net.Extensions.DependencyInjection;
using SurrealDb.Net.Internals.DependencyInjection;
using SurrealDb.Net.Internals.Models.LiveQuery;
using SurrealDb.Net.Models;
using SurrealDb.Net.Models.Auth;
using SurrealDb.Net.Models.LiveQuery;
using SurrealDb.Net.Models.Response;
#if NET10_0_OR_GREATER
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
#else
using SystemTextJsonPatch;
#endif

namespace SurrealDb.Net.Internals;

public interface ISurrealDbEngineWithSessions
{
    Task CloseSession(Guid sessionId, CancellationToken cancellationToken);
    Task<Guid> CreateSession(CancellationToken cancellationToken);
    Task<Guid> CreateSession(Guid from, CancellationToken cancellationToken);
}

public interface ISurrealDbEngine : ISurrealDbEngineWithSessions, IDisposable, IAsyncDisposable
{
    Uri Uri { get; }

    Task Attach(Guid sessionId, CancellationToken cancellationToken);
    Task Authenticate(Tokens tokens, Guid? sessionId, CancellationToken cancellationToken);
    Task Connect(CancellationToken cancellationToken);
    Task<T> Create<T>(T data, Guid? sessionId, CancellationToken cancellationToken)
        where T : IRecord;
    Task<T> Create<T>(string table, T? data, Guid? sessionId, CancellationToken cancellationToken);
    Task<TOutput> Create<TData, TOutput>(
        StringRecordId recordId,
        TData? data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where TOutput : IRecord;
    Task Delete(string table, Guid? sessionId, CancellationToken cancellationToken);
    Task<bool> Delete(RecordId recordId, Guid? sessionId, CancellationToken cancellationToken);
    Task<bool> Delete(
        StringRecordId recordId,
        Guid? sessionId,
        CancellationToken cancellationToken
    );
    Task Detach(Guid sessionId, CancellationToken cancellationToken);
    Task<bool> Health(CancellationToken cancellationToken);
    Task<T> Info<T>(Guid? sessionId, CancellationToken cancellationToken);
    Task<IEnumerable<T>> Insert<T>(
        string table,
        IEnumerable<T> data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where T : IRecord;
    Task<T> InsertRelation<T>(T data, Guid? sessionId, CancellationToken cancellationToken)
        where T : IRelationRecord;
    Task<T> InsertRelation<T>(
        string table,
        T data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where T : IRelationRecord;
    Task Invalidate(Guid? sessionId, CancellationToken cancellationToken);
    Task Kill(
        Guid queryUuid,
        SurrealDbLiveQueryClosureReason reason,
        Guid? sessionId,
        CancellationToken cancellationToken
    );
    SurrealDbLiveQuery<T> ListenLive<T>(Guid queryUuid, Guid? sessionId);
    Task<SurrealDbLiveQuery<T>> LiveRawQuery<T>(
        string query,
        IReadOnlyDictionary<string, object?> parameters,
        Guid? sessionId,
        CancellationToken cancellationToken
    );
    Task<SurrealDbLiveQuery<T>> LiveTable<T>(
        string table,
        bool diff,
        Guid? sessionId,
        CancellationToken cancellationToken
    );
    Task<TOutput> Merge<TMerge, TOutput>(
        TMerge data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where TMerge : IRecord;
    Task<T> Merge<T>(
        RecordId recordId,
        Dictionary<string, object> data,
        Guid? sessionId,
        CancellationToken cancellationToken
    );
    Task<T> Merge<T>(
        StringRecordId recordId,
        Dictionary<string, object> data,
        Guid? sessionId,
        CancellationToken cancellationToken
    );
    Task<IEnumerable<TOutput>> Merge<TMerge, TOutput>(
        string table,
        TMerge data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where TMerge : class;
    Task<IEnumerable<T>> Merge<T>(
        string table,
        Dictionary<string, object> data,
        Guid? sessionId,
        CancellationToken cancellationToken
    );
    Task<T> Patch<T>(
        RecordId recordId,
        JsonPatchDocument<T> patches,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where T : class;
    Task<T> Patch<T>(
        StringRecordId recordId,
        JsonPatchDocument<T> patches,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where T : class;
    Task<IEnumerable<T>> Patch<T>(
        string table,
        JsonPatchDocument<T> patches,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where T : class;
    Task<SurrealDbResponse> RawQuery(
        string query,
        IReadOnlyDictionary<string, object?> parameters,
        Guid? sessionId,
        CancellationToken cancellationToken
    );
    Task<IEnumerable<TOutput>> Relate<TOutput, TData>(
        string table,
        IEnumerable<RecordId> ins,
        IEnumerable<RecordId> outs,
        TData? data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where TOutput : class;
    Task<TOutput> Relate<TOutput, TData>(
        RecordId recordId,
        RecordId @in,
        RecordId @out,
        TData? data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where TOutput : class;
    Task<T> Run<T>(
        string name,
        string? version,
        object[]? args,
        Guid? sessionId,
        CancellationToken cancellationToken
    );
    Task<IEnumerable<T>> Select<T>(
        string table,
        Guid? sessionId,
        CancellationToken cancellationToken
    );
    Task<T?> Select<T>(RecordId recordId, Guid? sessionId, CancellationToken cancellationToken);
    Task<T?> Select<T>(
        StringRecordId recordId,
        Guid? sessionId,
        CancellationToken cancellationToken
    );
    Task<IEnumerable<TOutput>> Select<TStart, TEnd, TOutput>(
        RecordIdRange<TStart, TEnd> recordIdRange,
        Guid? sessionId,
        CancellationToken cancellationToken
    );

    Task<IEnumerable<Guid>> Sessions(CancellationToken cancellationToken);
    Task Set(string key, object value, Guid? sessionId, CancellationToken cancellationToken);
    Task SignIn(RootAuth root, Guid? sessionId, CancellationToken cancellationToken);
    Task<Tokens> SignIn(NamespaceAuth nsAuth, Guid? sessionId, CancellationToken cancellationToken);
    Task<Tokens> SignIn(DatabaseAuth dbAuth, Guid? sessionId, CancellationToken cancellationToken);
    Task<Tokens> SignIn<T>(T scopeAuth, Guid? sessionId, CancellationToken cancellationToken)
        where T : ScopeAuth;
    Task<Tokens> SignUp<T>(T scopeAuth, Guid? sessionId, CancellationToken cancellationToken)
        where T : ScopeAuth;
    SurrealDbLiveQueryChannel SubscribeToLiveQuery(Guid id);
    Task Unset(string key, Guid? sessionId, CancellationToken cancellationToken);
    Task<T> Update<T>(T data, Guid? sessionId, CancellationToken cancellationToken)
        where T : IRecord;
    Task<TOutput> Update<TData, TOutput>(
        StringRecordId recordId,
        TData data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where TOutput : IRecord;
    Task<IEnumerable<T>> Update<T>(
        string table,
        T data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where T : class;

    Task<IEnumerable<TOutput>> Update<TData, TOutput>(
        string table,
        TData data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where TOutput : IRecord;
    Task<TOutput> Update<TData, TOutput>(
        RecordId recordId,
        TData data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where TOutput : IRecord;
    Task<T> Upsert<T>(T data, Guid? sessionId, CancellationToken cancellationToken)
        where T : IRecord;
    Task<TOutput> Upsert<TData, TOutput>(
        StringRecordId recordId,
        TData data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where TOutput : IRecord;
    Task<IEnumerable<T>> Upsert<T>(
        string table,
        T data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where T : class;
    Task<IEnumerable<TOutput>> Upsert<TData, TOutput>(
        string table,
        TData data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where TOutput : IRecord;
    Task<TOutput> Upsert<TData, TOutput>(
        RecordId recordId,
        TData data,
        Guid? sessionId,
        CancellationToken cancellationToken
    )
        where TOutput : IRecord;
    Task Use(string ns, string db, Guid? sessionId, CancellationToken cancellationToken);
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
        ISurrealDbLoggerFactory? surrealDbLoggerFactory,
        ISessionizer? sessionizer
    );

    /// <summary>
    /// Export the database as a SurrealQL script.
    /// </summary>
    /// <param name="options">Export configuration options.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>SurrealQL script as <see cref="String"/></returns>
    Task<string> Export(ExportOptions? options, CancellationToken cancellationToken);

    /// <summary>
    /// This method imports data into a SurrealDB database.
    /// </summary>
    /// <remarks>
    /// This method is only supported by SurrealDB v2.0.0 or higher.
    /// </remarks>
    /// <param name="input"></param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    Task Import(string input, CancellationToken cancellationToken);
}

public interface ISurrealDbInMemoryEngine : ISurrealDbProviderEngine { }

public interface ISurrealDbRocksDbEngine : ISurrealDbProviderEngine { }

public interface ISurrealDbKvEngine : ISurrealDbProviderEngine { }
