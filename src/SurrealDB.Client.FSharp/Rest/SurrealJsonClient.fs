namespace SurrealDB.Client.FSharp.Rest

open System.Net.Http
open System.Text.Json
open System.Text.Json.Nodes
open System.Threading
open System.Threading.Tasks

open SurrealDB.Client.FSharp

/// <summary>
/// The SurrealDB HTTP RESTful Endpoints, using JSON as the data format.
/// </summary>
/// <see href="https://surrealdb.com/docs/integration/http"/>
type ISurrealJsonClient =
    inherit System.IDisposable

    /// <summary>
    /// The SQL endpoint enables advanced SurrealQL queries.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#sql"/>
    /// <param name="query">The query to execute.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The records as a JSON Node.</returns>
    abstract SqlAsync : query: string * ct: CancellationToken -> Task<RestApiResult<JsonNode>>


    /// <summary>
    /// This HTTP RESTful endpoint selects all records in a specific table in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#select-all"/>
    /// <param name="table">The table to select from.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The records as a JSON Node.</returns>
    abstract ListAsync : table: string * ct: CancellationToken -> Task<RestApiResult<JsonNode>>


    /// <summary>
    /// This HTTP RESTful endpoint creates a record in a specific table in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#create-all"/>
    /// <param name="table">The table to create the record in.</param>
    /// <param name="record">The record to create as a JSON Node.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created record as a JSON Node.</returns>
    abstract CreateAsync : table: string * record: JsonNode * ct: CancellationToken -> Task<RestApiResult<JsonNode>>

    /// <summary>
    /// This HTTP RESTful endpoint deletes all records from the specified table in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#delete-all"/>
    /// <param name="table">The table to delete all records from.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An empty array as a JSON Node.</returns>
    abstract DeleteAllAsync : table: string * ct: CancellationToken -> Task<RestApiResult<JsonNode>>

    /// <summary>
    /// This HTTP RESTful endpoint selects a specific record from the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#select-one"/>
    /// <param name="table">The table to select the record from.</param>
    /// <param name="id">The id of the record to select.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An array with the record (or empty) as a JSON Node.</returns>
    abstract FindAsync : table: string * id: string * ct: CancellationToken -> Task<RestApiResult<JsonNode>>

    /// <summary>
    /// This HTTP RESTful endpoint creates a single specific record into the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#create-one"/>
    /// <param name="table">The table to create the record in.</param>
    /// <param name="id">The id of the record to create.</param>
    /// <param name="record">The record to create as a JSON Node.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created record as a JSON Node.</returns>
    abstract InsertAsync :
        table: string * id: string * record: JsonNode * ct: CancellationToken -> Task<RestApiResult<JsonNode>>

    /// <summary>
    /// This HTTP RESTful endpoint creates or updates a single specific record in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#update-one"/>
    /// <param name="table">The table to create or update the record in.</param>
    /// <param name="id">The id of the record to create or update.</param>
    /// <param name="record">The record to create or update as a JSON Node.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created or updated record as a JSON Node.</returns>
    abstract ReplaceAsync :
        table: string * id: string * record: JsonNode * ct: CancellationToken -> Task<RestApiResult<JsonNode>>

    /// <summary>
    /// This HTTP RESTful endpoint creates or updates a single specific record in the database.
    /// If the record already exists, then only the specified fields will be updated.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#modify-one"/>
    /// <param name="table">The table to update the record in.</param>
    /// <param name="id">The id of the record to update.</param>
    /// <param name="record">The partial record to update as a JSON Node.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The updated record as a JSON Node.</returns>
    abstract ModifyAsync :
        table: string * id: string * record: JsonNode * ct: CancellationToken -> Task<RestApiResult<JsonNode>>

    /// <summary>
    /// This HTTP RESTful endpoint deletes a single specific record from the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#delete-one"/>
    /// <param name="table">The table to delete the record from.</param>
    /// <param name="id">The id of the record to delete.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An empty array as a JSON Node.</returns>
    abstract DeleteAsync : table: string * id: string * ct: CancellationToken -> Task<RestApiResult<JsonNode>>

/// <summary>
/// The SurrealDB RESTful API endpoints implementation.
/// </summary>
type SurrealJsonClient(config: SurrealConfig, httpClient: HttpClient, jsonOptions: JsonSerializerOptions) =
    do applyConfig config httpClient

    member this.SqlAsync(query: string, ct: CancellationToken) =
        executeTextRequest<JsonNode> jsonOptions httpClient ct HttpMethod.Post SQL_ENDPOINT query

    member this.ListAsync(table: string, ct: CancellationToken) =
        executeEmptyRequest<JsonNode> jsonOptions httpClient ct HttpMethod.Get (keyTable table)

    member this.CreateAsync(table: string, record: JsonNode, ct: CancellationToken) =
        executeJsonRequest<JsonNode> jsonOptions httpClient ct HttpMethod.Post (keyTable table) record

    member this.DeleteAllAsync(table: string, ct: CancellationToken) =
        executeEmptyRequest<JsonNode> jsonOptions httpClient ct HttpMethod.Delete (keyTable table)

    member this.FindAsync(table: string, id: string, ct: CancellationToken) =
        executeEmptyRequest<JsonNode> jsonOptions httpClient ct HttpMethod.Get (keyTableId table id)

    member this.InsertAsync(table: string, id: string, record: JsonNode, ct: CancellationToken) =
        executeJsonRequest<JsonNode> jsonOptions httpClient ct HttpMethod.Post (keyTableId table id) record

    member this.ReplaceAsync(table: string, id: string, record: JsonNode, ct: CancellationToken) =
        executeJsonRequest<JsonNode> jsonOptions httpClient ct HttpMethod.Put (keyTableId table id) record

    member this.ModifyAsync(table: string, id: string, record: JsonNode, ct: CancellationToken) =
        executeJsonRequest<JsonNode> jsonOptions httpClient ct HttpMethod.Patch (keyTableId table id) record

    member this.DeleteAsync(table: string, id: string, ct: CancellationToken) =
        executeEmptyRequest<JsonNode> jsonOptions httpClient ct HttpMethod.Delete (keyTableId table id)

    member this.Dispose() = httpClient.Dispose()

    interface ISurrealJsonClient with
        member this.SqlAsync(query, ct) = this.SqlAsync(query, ct)
        member this.ListAsync(table, ct) = this.ListAsync(table, ct)
        member this.CreateAsync(table, record, ct) = this.CreateAsync(table, record, ct)
        member this.DeleteAllAsync(table, ct) = this.DeleteAllAsync(table, ct)
        member this.FindAsync(table, id, ct) = this.FindAsync(table, id, ct)

        member this.InsertAsync(table, id, record, ct) =
            this.InsertAsync(table, id, record, ct)

        member this.ReplaceAsync(table, id, record, ct) =
            this.ReplaceAsync(table, id, record, ct)

        member this.ModifyAsync(table, id, record, ct) =
            this.ModifyAsync(table, id, record, ct)

        member this.DeleteAsync(table, id, ct) = this.DeleteAsync(table, id, ct)
        member this.Dispose() = this.Dispose()
