namespace SurrealDB.Client.FSharp.Rest

open System.Net.Http
open System.Text.Json
open System.Threading
open System.Threading.Tasks

open SurrealDB.Client.FSharp

/// <summary>
/// Represents a typed REST client for the SurrealDB Key-Value API.
type ISurrealRestKeyValueClient =
    /// <summary>
    /// Creates a new record in the specified table.
    /// </summary>
    /// <typeparam name="'recordData">The type of the record data. This type could have the id field missing.</typeparam>
    /// <typeparam name="'record">The type of the record. This type could have the id field.</typeparam>
    /// <param name="table">The table to create the record in.</param>
    /// <param name="recordData">The record data to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the operation, including the record with the generated id.</returns>
    abstract CreateDataAsync<'recordData, 'record> :
        table: string * recordData: 'recordData * cancellationToken: CancellationToken ->
        Task<RequestResult<'record>>

    /// <summary>
    /// Creates a new record in the specified table. Use this method if the record data type is the same as the record type.
    /// </summary>
    /// <typeparam name="'record">The type of the record. This type usually has the id field.</typeparam>
    /// <param name="table">The table to create the record in.</param>
    /// <param name="recordData">The record data to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the operation, including the record with the generated id.</returns>
    abstract CreateAsync<'record> :
        table: string * recordData: 'record * cancellationToken: CancellationToken -> Task<RequestResult<'record>>

    /// <summary>
    /// Creates a new record in the specified table.
    /// </summary>
    /// <typeparam name="'recordData">The type of the record data. This type could have the id field missing.</typeparam>
    /// <typeparam name="'record">The type of the record. This type could have the id field.</typeparam>
    /// <param name="table">The table to create the record in.</param>
    /// <param name="id">The id of the record to create.</param>
    /// <param name="recordData">The record data to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the operation, including the record with the generated id.</returns>
    abstract CreateDataAsync<'recordData, 'record> :
        table: string * id: string * recordData: 'recordData * cancellationToken: CancellationToken ->
        Task<RequestResult<'record>>

    /// <summary>
    /// Creates a new record in the specified table. Use this method if the record data type is the same as the record type.
    /// </summary>
    /// <typeparam name="'record">The type of the record. This type usually has the id field.</typeparam>
    /// <param name="table">The table to create the record in.</param>
    /// <param name="id">The id of the record to create.</param>
    /// <param name="recordData">The record data to create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the operation, including the record with the generated id.</returns>
    abstract CreateAsync<'record> :
        table: string * id: string * recordData: 'record * cancellationToken: CancellationToken ->
        Task<RequestResult<'record>>

    /// <summary>
    /// Updates or creates a record in the specified table.
    /// </summary>
    /// <typeparam name="'recordData">The type of the record data. This type could have the id field missing.</typeparam>
    /// <typeparam name="'record">The type of the record. This type could have the id field.</typeparam>
    /// <param name="table">The table to update or create the record in.</param>
    /// <param name="id">The id of the record to update or create.</param>
    /// <param name="recordData">The record data to update or create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the operation, including the record with the generated id, if the record was created.</returns>
    abstract InsertOrUpdateDataAsync<'recordData, 'record> :
        table: string * id: string * recordData: 'recordData * cancellationToken: CancellationToken ->
        Task<RequestResult<'record>>

    /// <summary>
    /// Updates or creates a record in the specified table. Use this method if the record data type is the same as the record type.
    /// </summary>
    /// <typeparam name="'record">The type of the record. This type usually has the id field.</typeparam>
    /// <param name="table">The table to update or create the record in.</param>
    /// <param name="id">The id of the record to update or create.</param>
    /// <param name="recordData">The record data to update or create.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the operation, including the record with the generated id, if the record was created.</returns>
    abstract InsertOrUpdateAsync<'record> :
        table: string * id: string * recordData: 'record * cancellationToken: CancellationToken ->
        Task<RequestResult<'record>>

    /// <summary>
    /// Patches a record in the specified table. If the record does not exist, it will be created with the specified fields.
    /// </summary>
    /// <typeparam name="'recordData">The type of the record data. This type could have the id field missing.</typeparam>
    /// <typeparam name="'record">The type of the record. This type could have the id field.</typeparam>
    /// <param name="table">The table to patch the record in.</param>
    /// <param name="id">The id of the record to patch.</param>
    /// <param name="recordData">The record data to patch.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the operation, including the record with the generated id, if the record was created.</returns>
    abstract PatchDataAsync<'recordData, 'record> :
        table: string * id: string * recordData: 'recordData * cancellationToken: CancellationToken ->
        Task<RequestResult<'record>>

    /// <summary>
    /// Returns all records in the specified table.
    /// </summary>
    /// <typeparam name="'record">The type of the record. This type usually has the id field.</typeparam>
    /// <param name="table">The table to get the records from.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the operation, including the records.</returns>
    abstract GetAllAsync<'record> :
        table: string * cancellationToken: CancellationToken -> Task<RequestResult<'record []>>

    /// <summary>
    /// Returns the record with the specified id from the specified table.
    /// </summary>
    /// <typeparam name="'record">The type of the record. This type usually has the id field.</typeparam>
    /// <param name="table">The table to get the record from.</param>
    /// <param name="id">The id of the record to get.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the operation, including the record. If the record does not exist, the result will be ValueNone.</returns>
    abstract GetAsync<'record> :
        table: string * id: string * cancellationToken: CancellationToken -> Task<RequestResult<'record voption>>

    /// <summary>
    /// Deletes all records in the specified table.
    /// </summary>
    /// <param name="table">The table to delete the records from.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    abstract DeleteAllAsync : table: string * cancellationToken: CancellationToken -> Task<RequestResult<unit>>

    /// <summary>
    /// Deletes the record with the specified id from the specified table.
    /// </summary>
    /// <param name="table">The table to delete the record from.</param>
    /// <param name="id">The id of the record to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    abstract DeleteAsync :
        table: string * id: string * cancellationToken: CancellationToken -> Task<RequestResult<unit>>

type internal SurrealRestKeyValueClient
    (
        jsonClient: ISurrealRestJsonClient,
        httpClient: HttpClient,
        jsonOptions: JsonSerializerOptions
    ) =

    interface ISurrealRestKeyValueClient with
        member this.CreateDataAsync<'recordData, 'record>(table, recordData, cancellationToken) =
            Endpoints.Typed.postKeyTableData<'recordData, 'record> jsonOptions httpClient cancellationToken table recordData

        member this.CreateAsync<'record>(table, recordData, cancellationToken) =
            Endpoints.Typed.postKeyTable<'record> jsonOptions httpClient cancellationToken table recordData

        member this.CreateDataAsync<'recordData, 'record>(table, id, recordData, cancellationToken) =
            Endpoints.Typed.postKeyTableIdData<'recordData, 'record> jsonOptions httpClient cancellationToken table id recordData

        member this.CreateAsync<'record>(table, id, recordData, cancellationToken) =
            Endpoints.Typed.postKeyTableId<'record> jsonOptions httpClient cancellationToken table id recordData

        member this.InsertOrUpdateDataAsync<'recordData, 'record>(table, id, recordData, cancellationToken) =
            Endpoints.Typed.putKeyTableIdData<'recordData, 'record> jsonOptions httpClient cancellationToken table id recordData

        member this.InsertOrUpdateAsync<'record>(table, id, recordData, cancellationToken) =
            Endpoints.Typed.putKeyTableId<'record> jsonOptions httpClient cancellationToken table id recordData

        member this.PatchDataAsync<'recordData, 'record>(table, id, recordData, cancellationToken) =
            Endpoints.Typed.patchKeyTableIdData<'recordData, 'record> jsonOptions httpClient cancellationToken table id recordData

        member this.GetAllAsync<'record>(table, cancellationToken) =
            Endpoints.Typed.getKeyTable<'record> jsonOptions httpClient cancellationToken table

        member this.GetAsync<'record>(table, id, cancellationToken) =
            Endpoints.Typed.getKeyTableId<'record> jsonOptions httpClient cancellationToken table id

        member this.DeleteAllAsync(table, cancellationToken) =
            Endpoints.Typed.deleteKeyTable jsonOptions httpClient cancellationToken table

        member this.DeleteAsync(table, id, cancellationToken) =
            Endpoints.Typed.deleteKeyTableId jsonOptions httpClient cancellationToken table id
