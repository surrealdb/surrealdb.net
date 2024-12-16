using Microsoft.Extensions.ObjectPool;
using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Models;
using SurrealDb.Net.Models.Auth;
using SurrealDb.Net.Models.LiveQuery;
using SurrealDb.Net.Models.Response;
using SystemTextJsonPatch;
#if NET6_0_OR_GREATER
using SurrealDb.Net.Handlers;
#endif

namespace SurrealDb.Net;

/// <summary>
/// The entry point to communicate with a SurrealDB instance.
/// Authenticate, use namespace/database, execute queries, etc...
/// </summary>
public interface ISurrealDbClient : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// The uri linked to the SurrealDB instance target.
    /// </summary>
    Uri Uri { get; }

    /// <summary>
    /// The naming policy used to serialize and deserialize data (eg. JSON for table name and record field names).
    /// Default: PascalCase.
    /// </summary>
    string? NamingPolicy { get; }

    /// <summary>
    /// Authenticates the current connection with a JWT.
    /// </summary>
    /// <param name="jwt">The JWT holder of the token.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task Authenticate(Jwt jwt, CancellationToken cancellationToken = default);

    /// <summary>
    /// Connects to the SurrealDB instance. This can improve performance to avoid cold starts.<br /><br />
    ///
    /// * Using HTTP(S) protocol: initializes a new HTTP connection<br />
    /// * Using WS(S) protocol: will start a websocket connection so that further calls will be triggered immediately
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task Connect(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates the specific record in the database.
    /// </summary>
    /// <remarks>
    /// Note: This method creates only a single record. If the record already exist, it will throw an error.
    /// </remarks>
    /// <typeparam name="T">The type of the record to create.</typeparam>
    /// <param name="data">The record to create.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The record created.</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<T> Create<T>(T data, CancellationToken cancellationToken = default)
        where T : IRecord;

    /// <summary>
    /// Creates a record in a table in the database.
    /// </summary>
    /// <typeparam name="T">The type of the record to create.</typeparam>
    /// <param name="table">The table name where the record will be stored.</param>
    /// <param name="data">The record to create.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The record created.</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<T> Create<T>(
        string table,
        T? data = default,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Creates the specific record in the database.
    /// </summary>
    /// <typeparam name="TData">The type of data contained in the record.</typeparam>
    /// <typeparam name="TOutput">The type of the record created.</typeparam>
    /// <param name="recordId">The record id.</param>
    /// <param name="data">The data contained in the record.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The record created.</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<TOutput> Create<TData, TOutput>(
        StringRecordId recordId,
        TData? data = default,
        CancellationToken cancellationToken = default
    )
        where TOutput : IRecord;

    /// <summary>
    /// Deletes all records in a table from the database.
    /// </summary>
    /// <param name="table">The name of the database table</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task Delete(string table, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the specified record from the database.
    /// </summary>
    /// <param name="recordId">The record id.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>Returns true if the record was removed successfully.</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<bool> Delete(RecordId recordId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the specified record from the database.
    /// </summary>
    /// <param name="recordId">The record id.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>Returns true if the record was removed successfully.</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<bool> Delete(StringRecordId recordId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Export the database as a SurrealQL script.
    /// </summary>
    /// <param name="options">Export configuration options.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>SurrealQL script as <see cref="String"/></returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="NotImplementedException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<string> Export(
        ExportOptions? options = default,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Checks the status of the database server and storage engine.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>Returns true if the database server and storage engine are healthy.</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<bool> Health(CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves information about the authenticated scope user.
    /// </summary>
    /// <typeparam name="T">The scope user type.</typeparam>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>Returns the record of an authenticated scope user.</returns>
    Task<T> Info<T>(CancellationToken cancellationToken = default);

    /// <summary>
    /// Inserts a collection of records in the database.
    /// </summary>
    /// <remarks>
    /// Note: This method allows you to create multiple records at once.
    /// In case a record already exist, it will not throw error and it will not update the existing record.
    /// </remarks>
    /// <typeparam name="T">The type of the record to create.</typeparam>
    /// <param name="table">The table name where the records will be stored.</param>
    /// <param name="data">The records to create.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The records created.</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<IEnumerable<T>> Insert<T>(
        string table,
        IEnumerable<T> data,
        CancellationToken cancellationToken = default
    )
        where T : IRecord;

    /// <summary>
    /// Inserts a new relation record in the database.
    /// </summary>
    /// <typeparam name="T">The type of the record to create.</typeparam>
    /// <param name="data">The record data used to create the record.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The record created.</returns>
    Task<T> InsertRelation<T>(T data, CancellationToken cancellationToken = default)
        where T : RelationRecord;

    /// <summary>
    /// Inserts a new relation record in the database.
    /// </summary>
    /// <typeparam name="T">The type of the record to create.</typeparam>
    /// <param name="table">The table name where the records will be stored.</param>
    /// <param name="data">The record data used to create the record.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The record created.</returns>
    Task<T> InsertRelation<T>(string table, T data, CancellationToken cancellationToken = default)
        where T : RelationRecord;

    /// <summary>
    /// Invalidates the authentication for the current connection.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task Invalidate(CancellationToken cancellationToken = default);

    /// <summary>
    /// Kills an active live query.<br /><br />
    ///
    /// Not supported on HTTP(S) protocol.
    /// </summary>
    /// <param name="queryUuid">The UUID of the live query to kill.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task Kill(Guid queryUuid, CancellationToken cancellationToken = default);

    /// <summary>
    /// Listen for live query responses, using live query UUID.<br /><br />
    ///
    /// Not supported on HTTP(S) protocol.
    /// </summary>
    /// <typeparam name="T">The type of the data returned by the live query.</typeparam>
    /// <param name="queryUuid">The UUID of the live query to consume.</param>
    /// <returns>A Live Query instance used to consume data in realtime.</returns>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    SurrealDbLiveQuery<T> ListenLive<T>(Guid queryUuid);

    /// <summary>
    /// Initiates a live query from an interpolated string representing a SurrealQL query.<br /><br />
    ///
    /// Not supported on HTTP(S) protocol.
    /// </summary>
    /// <typeparam name="T">The type of the data returned by the live query.</typeparam>
    /// <param name="query">The SurrealQL query.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>A Live Query instance used to consume data in realtime.</returns>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<SurrealDbLiveQuery<T>> LiveQuery<T>(
#if NET6_0_OR_GREATER
        QueryInterpolatedStringHandler query,
#else
        FormattableString query,
#endif
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Initiates a live query from a raw SurrealQL query.<br /><br />
    ///
    /// Not supported on HTTP(S) protocol.
    /// </summary>
    /// <typeparam name="T">The type of the data returned by the live query.</typeparam>
    /// <param name="query">The SurrealQL query.</param>
    /// <param name="parameters">A list of parameters to be used inside the SurrealQL query.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>A Live Query instance used to consume data in realtime.</returns>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<SurrealDbLiveQuery<T>> LiveRawQuery<T>(
        string query,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Initiates a live query on a table.<br /><br />
    ///
    /// Not supported on HTTP(S) protocol.
    /// </summary>
    /// <typeparam name="T">The type of the data returned by the live query.</typeparam>
    /// <param name="table">The name of the database table to watch.</param>
    /// <param name="diff">
    /// If set to true, live notifications will include an array of JSON Patch objects,
    /// rather than the entire record for each notification.
    /// </param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>A Live Query instance used to consume data in realtime.</returns>
    /// <exception cref="NotSupportedException"></exception>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<SurrealDbLiveQuery<T>> LiveTable<T>(
        string table,
        bool diff = false,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Modifies the specified record in the database.
    /// </summary>
    /// <typeparam name="TMerge">The type of the merge update.</typeparam>
    /// <typeparam name="TOutput">The type of the record updated.</typeparam>
    /// <param name="data">The data to merge with the current record.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The record updated.</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<TOutput> Merge<TMerge, TOutput>(TMerge data, CancellationToken cancellationToken = default)
        where TMerge : IRecord;

    /// <summary>
    /// Modifies the specified record in the database.
    /// </summary>
    /// <typeparam name="T">The type of the record updated.</typeparam>
    /// <param name="recordId">The record id.</param>
    /// <param name="data">A list of key-value pairs that contains properties to change.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The record updated.</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<T> Merge<T>(
        RecordId recordId,
        Dictionary<string, object> data,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Modifies the specified record in the database.
    /// </summary>
    /// <typeparam name="T">The type of the record updated.</typeparam>
    /// <param name="recordId">The record id.</param>
    /// <param name="data">A list of key-value pairs that contains properties to change.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The record updated.</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<T> Merge<T>(
        StringRecordId recordId,
        Dictionary<string, object> data,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Modifies all records in the database.
    /// </summary>
    /// <typeparam name="TMerge">The type of the merge update.</typeparam>
    /// <typeparam name="TOutput">The type of the record updated.</typeparam>
    /// <param name="table">The name of the database table.</param>
    /// <param name="data">The data to merge with the current record.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The list of updated records.</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<IEnumerable<TOutput>> Merge<TMerge, TOutput>(
        string table,
        TMerge data,
        CancellationToken cancellationToken = default
    )
        where TMerge : class;

    /// <summary>
    /// Modifies all records in the database.
    /// </summary>
    /// <typeparam name="T">The type of the record updated.</typeparam>
    /// <param name="table">The name of the database table.</param>
    /// <param name="data">A list of key-value pairs that contains properties to change.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The list of updated records.</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<IEnumerable<T>> Merge<T>(
        string table,
        Dictionary<string, object> data,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Modifies the specified record in the database, using JSON Patch specification (https://jsonpatch.com/).
    /// </summary>
    /// <typeparam name="T">The type of the record updated.</typeparam>
    /// <param name="recordId">The record id.</param>
    /// <param name="patches">A list of JSON Patch operations.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The record updated.</returns>
    Task<T> Patch<T>(
        RecordId recordId,
        JsonPatchDocument<T> patches,
        CancellationToken cancellationToken = default
    )
        where T : class;

    /// <summary>
    /// Modifies the specified record in the database, using JSON Patch specification (https://jsonpatch.com/).
    /// </summary>
    /// <typeparam name="T">The type of the record updated.</typeparam>
    /// <param name="recordId">The record id.</param>
    /// <param name="patches">A list of JSON Patch operations.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The record updated.</returns>
    Task<T> Patch<T>(
        StringRecordId recordId,
        JsonPatchDocument<T> patches,
        CancellationToken cancellationToken = default
    )
        where T : class;

    /// <summary>
    /// Modifies all records in the database, using JSON Patch specification (https://jsonpatch.com/).
    /// </summary>
    /// <typeparam name="T">The type of the record updated.</typeparam>
    /// <param name="table">The name of the database table</param>
    /// <param name="patches">A list of JSON Patch operations.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The list of updated records.</returns>
    Task<IEnumerable<T>> Patch<T>(
        string table,
        JsonPatchDocument<T> patches,
        CancellationToken cancellationToken = default
    )
        where T : class;

    /// <summary>
    /// Executes SurrealQL queries based on an interpolated string representing a SurrealQL query.
    /// </summary>
    /// <param name="query">The SurrealQL query.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The list of results from the SurrealQL queries.</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<SurrealDbResponse> Query(
#if NET6_0_OR_GREATER
        QueryInterpolatedStringHandler query,
#else
        FormattableString query,
#endif
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Executes SurrealQL queries based on a raw SurrealQL query.
    /// </summary>
    /// <param name="query">The SurrealQL query.</param>
    /// <param name="parameters">A list of parameters to be used inside the SurrealQL query.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The list of results from the SurrealQL queries.</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<SurrealDbResponse> RawQuery(
        string query,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Creates a relation between two records.
    /// </summary>
    /// <typeparam name="TOutput">The type of the ouput record created.</typeparam>
    /// <param name="table">The name of the database table.</param>
    /// <param name="in">The record from where the relation starts.</param>
    /// <param name="out">The record to where the relation ends.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The record created.</returns>
    Task<TOutput> Relate<TOutput>(
        string table,
        RecordId @in,
        RecordId @out,
        CancellationToken cancellationToken = default
    )
        where TOutput : class;

    /// <summary>
    /// Creates a relation between two records.
    /// </summary>
    /// <typeparam name="TOutput">The type of the ouput record created.</typeparam>
    /// <typeparam name="TData">The type of the additional data to add to the relation record.</typeparam>
    /// <param name="table">The name of the database table.</param>
    /// <param name="in">The record from where the relation starts.</param>
    /// <param name="out">The record to where the relation ends.</param>
    /// <param name="data">Additional data to store in the relation record.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The record created.</returns>
    Task<TOutput> Relate<TOutput, TData>(
        string table,
        RecordId @in,
        RecordId @out,
        TData? data,
        CancellationToken cancellationToken = default
    )
        where TOutput : class;

    /// <summary>
    /// Creates relations between records.
    /// </summary>
    /// <typeparam name="TOutput">The type of the ouput record created.</typeparam>
    /// <param name="table">The name of the database table.</param>
    /// <param name="ins">The records from where the relation starts.</param>
    /// <param name="out">The record to where the relation ends.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The record created.</returns>
    Task<IEnumerable<TOutput>> Relate<TOutput>(
        string table,
        IEnumerable<RecordId> ins,
        RecordId @out,
        CancellationToken cancellationToken = default
    )
        where TOutput : class;

    /// <summary>
    /// Creates relations between records.
    /// </summary>
    /// <typeparam name="TOutput">The type of the ouput record created.</typeparam>
    /// <typeparam name="TData">The type of the additional data to add to the relation record.</typeparam>
    /// <param name="table">The name of the database table.</param>
    /// <param name="ins">The records from where the relation starts.</param>
    /// <param name="out">The record to where the relation ends.</param>
    /// <param name="data">Additional data to store in the relation record.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The record created.</returns>
    Task<IEnumerable<TOutput>> Relate<TOutput, TData>(
        string table,
        IEnumerable<RecordId> ins,
        RecordId @out,
        TData? data,
        CancellationToken cancellationToken = default
    )
        where TOutput : class;

    /// <summary>
    /// Creates relations between records.
    /// </summary>
    /// <typeparam name="TOutput">The type of the ouput record created.</typeparam>
    /// <param name="table">The name of the database table.</param>
    /// <param name="in">The record from where the relation starts.</param>
    /// <param name="outs">The records to where the relation ends.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The record created.</returns>
    Task<IEnumerable<TOutput>> Relate<TOutput>(
        string table,
        RecordId @in,
        IEnumerable<RecordId> outs,
        CancellationToken cancellationToken = default
    )
        where TOutput : class;

    /// <summary>
    /// Creates relations between records.
    /// </summary>
    /// <typeparam name="TOutput">The type of the ouput record created.</typeparam>
    /// <typeparam name="TData">The type of the additional data to add to the relation record.</typeparam>
    /// <param name="table">The name of the database table.</param>
    /// <param name="in">The record from where the relation starts.</param>
    /// <param name="outs">The records to where the relation ends.</param>
    /// <param name="data">Additional data to store in the relation record.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The record created.</returns>
    Task<IEnumerable<TOutput>> Relate<TOutput, TData>(
        string table,
        RecordId @in,
        IEnumerable<RecordId> outs,
        TData? data,
        CancellationToken cancellationToken = default
    )
        where TOutput : class;

    /// <summary>
    /// Creates relations between records.
    /// </summary>
    /// <typeparam name="TOutput">The type of the ouput record created.</typeparam>
    /// <param name="table">The name of the database table.</param>
    /// <param name="ins">The records from where the relation starts.</param>
    /// <param name="outs">The records to where the relation ends.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The record created.</returns>
    Task<IEnumerable<TOutput>> Relate<TOutput>(
        string table,
        IEnumerable<RecordId> ins,
        IEnumerable<RecordId> outs,
        CancellationToken cancellationToken = default
    )
        where TOutput : class;

    /// <summary>
    /// Creates relations between records.
    /// </summary>
    /// <typeparam name="TOutput">The type of the ouput record created.</typeparam>
    /// <typeparam name="TData">The type of the additional data to add to the relation record.</typeparam>
    /// <param name="table">The name of the database table.</param>
    /// <param name="ins">The records from where the relation starts.</param>
    /// <param name="outs">The records to where the relation ends.</param>
    /// <param name="data">Additional data to store in the relation record.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The record created.</returns>
    Task<IEnumerable<TOutput>> Relate<TOutput, TData>(
        string table,
        IEnumerable<RecordId> ins,
        IEnumerable<RecordId> outs,
        TData? data,
        CancellationToken cancellationToken = default
    )
        where TOutput : class;

    /// <summary>
    /// Creates a relation between two records.
    /// </summary>
    /// <typeparam name="TOutput">The type of the ouput record created.</typeparam>
    /// <param name="recordId">The record id.</param>
    /// <param name="in">The record from where the relation starts.</param>
    /// <param name="out">The record to where the relation ends.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The record created.</returns>
    Task<TOutput> Relate<TOutput>(
        RecordId recordId,
        RecordId @in,
        RecordId @out,
        CancellationToken cancellationToken = default
    )
        where TOutput : class;

    /// <summary>
    /// Creates a relation between two records.
    /// </summary>
    /// <typeparam name="TOutput">The type of the ouput record created.</typeparam>
    /// <typeparam name="TData">The type of the additional data to add to the relation record.</typeparam>
    /// <param name="recordId">The record id.</param>
    /// <param name="in">The record from where the relation starts.</param>
    /// <param name="out">The record to where the relation ends.</param>
    /// <param name="data">Additional data to store in the relation record.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The record created.</returns>
    Task<TOutput> Relate<TOutput, TData>(
        RecordId recordId,
        RecordId @in,
        RecordId @out,
        TData? data,
        CancellationToken cancellationToken = default
    )
        where TOutput : class;

    /// <summary>
    /// Runs a SurrealQL function.
    /// </summary>
    /// <typeparam name="T">The type of the output of the function called.</typeparam>
    /// <param name="name">The full name of the function.</param>
    /// <param name="args">The arguments supplied to the function.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The output of the function called.</returns>
    Task<T> Run<T>(
        string name,
        object[]? args = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Runs a versioned SurrealQL function.
    /// </summary>
    /// <typeparam name="T">The type of the output of the function called.</typeparam>
    /// <param name="name">The full name of the function.</param>
    /// <param name="version">The version of the function.</param>
    /// <param name="args">The arguments supplied to the function.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The output of the function called.</returns>
    Task<T> Run<T>(
        string name,
        string version,
        object[]? args = null,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Selects all records in a table.
    /// </summary>
    /// <typeparam name="T">The type of record to extract</typeparam>
    /// <param name="table">The name of the database table</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The list of extracted records</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<IEnumerable<T>> Select<T>(string table, CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects a single record.
    /// </summary>
    /// <typeparam name="T">The type of the record</typeparam>
    /// <param name="recordId">The record id.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The extracted record</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<T?> Select<T>(RecordId recordId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects a single record.
    /// </summary>
    /// <typeparam name="T">The type of the record</typeparam>
    /// <param name="recordId">The record id.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The extracted record</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<T?> Select<T>(StringRecordId recordId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Selects a range of records.
    /// </summary>
    /// <typeparam name="TStart">The type of the left/start bound range limit</typeparam>
    /// <typeparam name="TEnd">The type of the right/end bound range limit</typeparam>
    /// <typeparam name="TOutput">The type of the record</typeparam>
    /// <param name="recordIdRange">The record id range.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The list of extracted records</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<IEnumerable<TOutput>> Select<TStart, TEnd, TOutput>(
        RecordIdRange<TStart, TEnd> recordIdRange,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Assigns a value as a parameter for this connection.
    /// </summary>
    /// <param name="key">The name of the parameter.</param>
    /// <param name="value">The value of the parameter.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task Set(string key, object value, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sign in as a root user.
    /// </summary>
    /// <param name="root">Credentials to sign in as a root user</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task SignIn(RootAuth root, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sign in as a namespace user.
    /// </summary>
    /// <param name="nsAuth">Credentials to sign in as a namespace user</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<Jwt> SignIn(NamespaceAuth nsAuth, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sign in as a database user.
    /// </summary>
    /// <param name="dbAuth">Credentials to sign in as a database user</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<Jwt> SignIn(DatabaseAuth dbAuth, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sign in as a scoped user.
    /// </summary>
    /// <typeparam name="T">Type of the params used in the SIGNIN scope function</typeparam>
    /// <param name="scopeAuth">Credentials to sign in as a scoped user</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The JSON Web Token that can be used to authenticate the user</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<Jwt> SignIn<T>(T scopeAuth, CancellationToken cancellationToken = default)
        where T : ScopeAuth;

    /// <summary>
    /// Sign up a new scoped user.
    /// </summary>
    /// <typeparam name="T">Type of the params used in the SIGNUP scope function</typeparam>
    /// <param name="scopeAuth">Credentials to sign up as a scoped user</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The JSON Web Token that can be used to authenticate the user</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<Jwt> SignUp<T>(T scopeAuth, CancellationToken cancellationToken = default)
        where T : ScopeAuth;

    /// <summary>
    /// Removes a parameter from this connection.
    /// </summary>
    /// <param name="key">The name of the parameter.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task Unset(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the specified record in the database.
    /// </summary>
    /// <typeparam name="T">The type of the record to update.</typeparam>
    /// <param name="data">The record to update.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The updated record.</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<T> Update<T>(T data, CancellationToken cancellationToken = default)
        where T : IRecord;

    /// <summary>
    /// Updates the specified record in the database.
    /// </summary>
    /// <typeparam name="TData">The type of data contained in the record.</typeparam>
    /// <typeparam name="TOutput">The type of the record updated.</typeparam>
    /// <param name="recordId">The record id.</param>
    /// <param name="data">The record data to update.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The updated record.</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<TOutput> Update<TData, TOutput>(
        StringRecordId recordId,
        TData data,
        CancellationToken cancellationToken = default
    )
        where TOutput : IRecord;

    /// <summary>
    /// Updates all records in the database.
    /// </summary>
    /// <typeparam name="T">The type of the record to update.</typeparam>
    /// <param name="table">The name of the database table.</param>
    /// <param name="data">The record data to update.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The list of updated records.</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<IEnumerable<T>> Update<T>(
        string table,
        T data,
        CancellationToken cancellationToken = default
    )
        where T : class;

    /// <summary>
    /// Updates the specified record in the database.
    /// </summary>
    /// <typeparam name="TData">The type of data contained in the record.</typeparam>
    /// <typeparam name="TOutput">The type of the record updated.</typeparam>
    /// <param name="recordId">The record id.</param>
    /// <param name="data">The record data to update.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The updated record.</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<TOutput> Update<TData, TOutput>(
        RecordId recordId,
        TData data,
        CancellationToken cancellationToken = default
    )
        where TOutput : IRecord;

    /// <summary>
    /// Updates or creates the specified record in the database.
    /// </summary>
    /// <typeparam name="T">The type of the record to create.</typeparam>
    /// <param name="data">The record to create or update.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The record created or updated.</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<T> Upsert<T>(T data, CancellationToken cancellationToken = default)
        where T : IRecord;

    /// <summary>
    /// Updates or creates the specified record in the database.
    /// </summary>
    /// <typeparam name="TData">The type of data contained in the record.</typeparam>
    /// <typeparam name="TOutput">The type of the record created.</typeparam>
    /// <param name="recordId">The record id.</param>
    /// <param name="data">The record to create or update.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The record created or updated.</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<TOutput> Upsert<TData, TOutput>(
        StringRecordId recordId,
        TData data,
        CancellationToken cancellationToken = default
    )
        where TOutput : IRecord;

    /// <summary>
    /// Creates a random record in the database.
    /// Prior to SurrealDB v2.1.0, this method would update or create all records in the database.
    /// </summary>
    /// <typeparam name="T">The type of the record to upsert.</typeparam>
    /// <param name="table">The name of the database table.</param>
    /// <param name="data">The record to create or update.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The list of created or updated records.</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<IEnumerable<T>> Upsert<T>(
        string table,
        T data,
        CancellationToken cancellationToken = default
    )
        where T : class; // TODO : Change return type from "IEnumerable<T>" to "T" in the future

    /// <summary>
    /// Updates or creates the specified record in the database.
    /// </summary>
    /// <typeparam name="TData">The type of data contained in the record.</typeparam>
    /// <typeparam name="TOutput">The type of the record created.</typeparam>
    /// <param name="recordId">The record id.</param>
    /// <param name="data">The record to create or update.</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The record created or updated.</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<TOutput> Upsert<TData, TOutput>(
        RecordId recordId,
        TData data,
        CancellationToken cancellationToken = default
    )
        where TOutput : IRecord;

    /// <summary>
    /// Switch to a specific namespace and database.
    /// </summary>
    /// <param name="ns">Name of the namespace</param>
    /// <param name="db">Name of the database</param>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task Use(string ns, string db, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve the version of the SurrealDB instance.
    /// </summary>
    /// <param name="cancellationToken">The cancellationToken enables graceful cancellation of asynchronous operations</param>
    /// <returns>The version of the SurrealDB instance</returns>
    /// <exception cref="OperationCanceledException"></exception>
    /// <exception cref="HttpRequestException"></exception>
    /// <exception cref="InvalidOperationException"></exception>
    /// <exception cref="SurrealDbException"></exception>
    Task<string> Version(CancellationToken cancellationToken = default);
}
