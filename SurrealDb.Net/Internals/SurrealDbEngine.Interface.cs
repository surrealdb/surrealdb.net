using SurrealDb.Net.Internals.Models.LiveQuery;
using SurrealDb.Net.Models;
using SurrealDb.Net.Models.Auth;
using SurrealDb.Net.Models.LiveQuery;
using SurrealDb.Net.Models.Response;
using SystemTextJsonPatch;

namespace SurrealDb.Net.Internals;

internal interface ISurrealDbEngine : IDisposable
{
    Task Authenticate(Jwt jwt, CancellationToken cancellationToken);
    void Configure(string? ns, string? db, string? username, string? password);
    void Configure(string? ns, string? db, string? token = null);
    Task Connect(CancellationToken cancellationToken);
    Task<T> Create<T>(T data, CancellationToken cancellationToken)
        where T : IRecord;
    Task<T> Create<T>(string table, T? data, CancellationToken cancellationToken);
    Task Delete(string table, CancellationToken cancellationToken);
    Task<bool> Delete(Thing thing, CancellationToken cancellationToken);
    Task<bool> Health(CancellationToken cancellationToken);
    Task<T> Info<T>(CancellationToken cancellationToken);
    Task Invalidate(CancellationToken cancellationToken);
    Task Kill(
        Guid queryUuid,
        SurrealDbLiveQueryClosureReason reason,
        CancellationToken cancellationToken
    );
    SurrealDbLiveQuery<T> ListenLive<T>(Guid queryUuid);
    Task<SurrealDbLiveQuery<T>> LiveQuery<T>(
        FormattableString query,
        CancellationToken cancellationToken
    );
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
        where TMerge : IRecord;
    Task<T> Merge<T>(
        Thing thing,
        Dictionary<string, object> data,
        CancellationToken cancellationToken
    );
    Task<IEnumerable<TOutput>> MergeAll<TMerge, TOutput>(
        string table,
        TMerge data,
        CancellationToken cancellationToken
    )
        where TMerge : class;
    Task<IEnumerable<T>> MergeAll<T>(
        string table,
        Dictionary<string, object> data,
        CancellationToken cancellationToken
    );
    Task<T> Patch<T>(Thing thing, JsonPatchDocument<T> patches, CancellationToken cancellationToken)
        where T : class;
    Task<IEnumerable<T>> PatchAll<T>(
        string table,
        JsonPatchDocument<T> patches,
        CancellationToken cancellationToken
    )
        where T : class;
    Task<SurrealDbResponse> Query(FormattableString query, CancellationToken cancellationToken);
    Task<SurrealDbResponse> RawQuery(
        string query,
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken
    );
    Task<IEnumerable<T>> Select<T>(string table, CancellationToken cancellationToken);
    Task<T?> Select<T>(Thing thing, CancellationToken cancellationToken);
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
    Task<IEnumerable<T>> UpdateAll<T>(string table, T data, CancellationToken cancellationToken)
        where T : class;
    Task<T> Upsert<T>(T data, CancellationToken cancellationToken)
        where T : IRecord;
    Task Use(string ns, string db, CancellationToken cancellationToken);
    Task<string> Version(CancellationToken cancellationToken);
}
