namespace SurrealDB.Client.FSharp.Rest

open System.Net.Http
open System.Text.Json
open System.Threading
open System.Threading.Tasks

open SurrealDB.Client.FSharp


/// <summary>
/// The SurrealDB RESTful API Key Table endpoints using record types.
/// </summary>
type ISurrealKeyTableEndpoints =
    inherit System.IDisposable

    /// <summary>
    /// This HTTP RESTful endpoint selects all records in a specific table in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#select-all"/>
    /// <typeparam name="record">The type of the records to return.</typeparam>
    /// <param name="table">The table to select from.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An API result with a single statement with given records, or an error.</returns>
    abstract GetKeyTableResponse<'record> :
        table: string * ct: CancellationToken -> Task<RestApiSingleResult<'record []>>

    /// <summary>
    /// This HTTP RESTful endpoint selects all records in a specific table in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#select-all"/>
    /// <typeparam name="record">The type of the records to return.</typeparam>
    /// <param name="table">The table to select from.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The records as records of type 'record, or an error.</returns>
    abstract GetKeyTable<'record> : table: string * ct: CancellationToken -> Task<RequestResult<'record []>>


    /// <summary>
    /// This HTTP RESTful endpoint creates a single record in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#create-all"/>
    /// <typeparam name="recordData">The type of the record to create, as a partial record with no id field.</typeparam>
    /// <typeparam name="record">The type of the record to return, as a full record with an id field.</typeparam>
    /// <param name="table">The table to create the record in.</param>
    /// <param name="record">The record to create as a JSON Node.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An API result with a single statement with the created record, or an error.</returns>
    abstract PostPartialKeyTableResponse<'recordData, 'record> :
        table: string * record: 'recordData * ct: CancellationToken -> Task<RestApiSingleResult<'record>>

    /// <summary>
    /// This HTTP RESTful endpoint creates a single record in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#create-all"/>
    /// <typeparam name="recordData">The type of the record to create, as a partial record with no id field.</typeparam>
    /// <typeparam name="record">The type of the record to return, as a full record with an id field.</typeparam>
    /// <param name="table">The table to create the record in.</param>
    /// <param name="record">The record to create as a JSON Node.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created record as a record of type 'record, or an error.</returns>
    abstract PostPartialKeyTable<'recordData, 'record> :
        table: string * record: 'recordData * ct: CancellationToken -> Task<RequestResult<'record>>

    /// <summary>
    /// This HTTP RESTful endpoint creates a single record in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#create-all"/>
    /// <typeparam name="record">The type of the record to create and return.</typeparam>
    /// <param name="table">The table to create the record in.</param>
    /// <param name="record">The record to create as a partial record of type 'record.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An API result with a single statement with the created record, or an error.</returns>
    abstract PostKeyTableResponse<'record> :
        table: string * record: 'record * ct: CancellationToken -> Task<RestApiSingleResult<'record>>

    /// <summary>
    /// This HTTP RESTful endpoint creates a single record in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#create-all"/>
    /// <typeparam name="record">The type of the record to create and return.</typeparam>
    /// <param name="table">The table to create the record in.</param>
    /// <param name="record">The record to create as a partial record of type 'record.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created record as a record of type 'record, or an error.</returns>
    abstract PostKeyTable<'record> :
        table: string * record: 'record * ct: CancellationToken -> Task<RequestResult<'record>>

    /// <summary>
    /// This HTTP RESTful endpoint deletes all records in a specific table in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#delete-all"/>
    /// <param name="table">The table to delete from.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An API result with a single statement with no records, or an error.</returns>
    abstract DeleteKeyTableResponse : table: string * ct: CancellationToken -> Task<RestApiSingleResult<unit>>

    /// <summary>
    /// This HTTP RESTful endpoint deletes all records in a specific table in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#delete-all"/>
    /// <param name="table">The table to delete from.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An empty result, or an error.</returns>
    abstract DeleteKeyTable : table: string * ct: CancellationToken -> Task<RequestResult<unit>>

    /// <summary>
    /// This HTTP RESTful endpoint selects a single record in a specific table in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#select-one"/>
    /// <typeparam name="record">The type of the record to return.</typeparam>
    /// <param name="table">The table to select from.</param>
    /// <param name="id">The id of the record to select.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An API result with a single statement with the optional record, or an error.</returns>
    abstract GetKeyTableIdResponse<'record> :
        table: string * id: string * ct: CancellationToken -> Task<RestApiSingleResult<'record voption>>

    /// <summary>
    /// This HTTP RESTful endpoint selects a single record in a specific table in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#select-one"/>
    /// <typeparam name="record">The type of the record to return.</typeparam>
    /// <param name="table">The table to select from.</param>
    /// <param name="id">The id of the record to select.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The optional record as a record of type 'record, or an error.</returns>
    abstract GetKeyTableId<'record> :
        table: string * id: string * ct: CancellationToken -> Task<RequestResult<'record voption>>

    /// <summary>
    /// This HTTP RESTful endpoint creates a single record in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#create-one"/>
    /// <typeparam name="recordData">The type of the record to create, as a partial record with no id field.</typeparam>
    /// <typeparam name="record">The type of the record to return, as a full record with an id field.</typeparam>
    /// <param name="table">The table to create the record in.</param>
    /// <param name="id">The id of the record to create.</param>
    /// <param name="record">The record to create as a partial record of type 'recordData.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An API result with a single statement with the created record, or an error.</returns>
    abstract PostPartialKeyTableIdResponse<'recordData, 'record> :
        table: string * id: string * record: 'recordData * ct: CancellationToken -> Task<RestApiSingleResult<'record>>

    /// <summary>
    /// This HTTP RESTful endpoint creates a single record in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#create-one"/>
    /// <typeparam name="recordData">The type of the record to create, as a partial record with no id field.</typeparam>
    /// <typeparam name="record">The type of the record to return, as a full record with an id field.</typeparam>
    /// <param name="table">The table to create the record in.</param>
    /// <param name="id">The id of the record to create.</param>
    /// <param name="record">The record to create as a partial record of type 'recordData.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created record as a record of type 'record, or an error.</returns>
    abstract PostPartialKeyTableId<'recordData, 'record> :
        table: string * id: string * record: 'recordData * ct: CancellationToken -> Task<RequestResult<'record>>

    /// <summary>
    /// This HTTP RESTful endpoint creates a single record in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#create-one"/>
    /// <typeparam name="record">The type of the record to create and return.</typeparam>
    /// <param name="table">The table to create the record in.</param>
    /// <param name="id">The id of the record to create.</param>
    /// <param name="record">The record to create as a record of type 'record.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An API result with a single statement with the created record, or an error.</returns>
    abstract PostKeyTableIdResponse<'record> :
        table: string * id: string * record: 'record * ct: CancellationToken -> Task<RestApiSingleResult<'record>>

    /// <summary>
    /// This HTTP RESTful endpoint creates a single record in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#create-one"/>
    /// <typeparam name="record">The type of the record to create and return.</typeparam>
    /// <param name="table">The table to create the record in.</param>
    /// <param name="id">The id of the record to create.</param>
    /// <param name="record">The record to create as a record of type 'record.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created record as a record of type 'record, or an error.</returns>
    abstract PostKeyTableId<'record> :
        table: string * id: string * record: 'record * ct: CancellationToken -> Task<RequestResult<'record>>

    /// <summary>
    /// This HTTP RESTful endpoint creates or updates a single specific record in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#update-one"/>
    /// <typeparam name="recordData">The type of the record to create or update, as a partial record with no id field.</typeparam>
    /// <typeparam name="record">The type of the record to return, as a full record with an id field.</typeparam>
    /// <param name="table">The table to create or update the record in.</param>
    /// <param name="id">The id of the record to create or update.</param>
    /// <param name="record">The record to create or update as a partial record of type 'recordData.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An API result with a single statement with the created or updated record, or an error.</returns>
    abstract PutPartialKeyTableIdResponse<'recordData, 'record> :
        table: string * id: string * record: 'recordData * ct: CancellationToken -> Task<RestApiSingleResult<'record>>

    /// <summary>
    /// This HTTP RESTful endpoint creates or updates a single specific record in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#update-one"/>
    /// <typeparam name="recordData">The type of the record to create or update, as a partial record with no id field.</typeparam>
    /// <typeparam name="record">The type of the record to return, as a full record with an id field.</typeparam>
    /// <param name="table">The table to create or update the record in.</param>
    /// <param name="id">The id of the record to create or update.</param>
    /// <param name="record">The record to create or update as a partial record of type 'recordData.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created or updated record as a record of type 'record, or an error.</returns>
    abstract PutPartialKeyTableId<'recordData, 'record> :
        table: string * id: string * record: 'recordData * ct: CancellationToken -> Task<RequestResult<'record>>

    /// <summary>
    /// This HTTP RESTful endpoint creates or updates a single specific record in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#update-one"/>
    /// <typeparam name="record">The type of the record to create or update and return.</typeparam>
    /// <param name="table">The table to create or update the record in.</param>
    /// <param name="id">The id of the record to create or update.</param>
    /// <param name="record">The record to create or update as a record of type 'record.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An API result with a single statement with the created or updated record, or an error.</returns>
    abstract PutKeyTableIdResponse<'record> :
        table: string * id: string * record: 'record * ct: CancellationToken -> Task<RestApiSingleResult<'record>>

    /// <summary>
    /// This HTTP RESTful endpoint creates or updates a single specific record in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#update-one"/>
    /// <typeparam name="record">The type of the record to create or update and return.</typeparam>
    /// <param name="table">The table to create or update the record in.</param>
    /// <param name="id">The id of the record to create or update.</param>
    /// <param name="record">The record to create or update as a record of type 'record.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created or updated record as a record of type 'record, or an error.</returns>
    abstract PutKeyTableId<'record> :
        table: string * id: string * record: 'record * ct: CancellationToken -> Task<RequestResult<'record>>

    /// <summary>
    /// This HTTP RESTful endpoint creates or modify a single specific record in the database.
    /// If the record already exists, then only the specified fields will be updated.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#modify-one"/>
    /// <typeparam name="recordData">The type of the record to create or update, as a partial record with just the fields to update.</typeparam>
    /// <typeparam name="record">The type of the record to return, as a full record with all fields.</typeparam>
    /// <param name="table">The table to update the record in.</param>
    /// <param name="id">The id of the record to update.</param>
    /// <param name="record">The record to update as a partial record.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An API result with a single statement with the updated record, or an error.</returns>
    abstract PatchPartialKeyTableIdResponse<'recordData, 'record> :
        table: string * id: string * record: 'recordData * ct: CancellationToken -> Task<RestApiSingleResult<'record>>

    /// <summary>
    /// This HTTP RESTful endpoint creates or modify a single specific record in the database.
    /// If the record already exists, then only the specified fields will be updated.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#modify-one"/>
    /// <typeparam name="recordData">The type of the record to create or update, as a partial record with just the fields to update.</typeparam>
    /// <typeparam name="record">The type of the record to return, as a full record with all fields.</typeparam>
    /// <param name="table">The table to update the record in.</param>
    /// <param name="id">The id of the record to update.</param>
    /// <param name="record">The record to update as a partial record.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The updated record as a record of type 'record, or an error.</returns>
    abstract PatchPartialKeyTableId<'recordData, 'record> :
        table: string * id: string * record: 'recordData * ct: CancellationToken -> Task<RequestResult<'record>>

    /// <summary>
    /// This HTTP RESTful endpoint deletes a single specific record in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#delete-one"/>
    /// <param name="table">The table to delete the record from.</param>
    /// <param name="id">The id of the record to delete.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An API result with a single statement with no data, or an error.</returns>
    abstract DeleteKeyTableIdResponse :
        table: string * id: string * ct: CancellationToken -> Task<RestApiSingleResult<unit>>

    /// <summary>
    /// This HTTP RESTful endpoint deletes a single specific record in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#delete-one"/>
    /// <param name="table">The table to delete the record from.</param>
    /// <param name="id">The id of the record to delete.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Unit, or an error.</returns>
    abstract DeleteKeyTableId : table: string * id: string * ct: CancellationToken -> Task<RequestResult<unit>>

/// <summary>
/// The SurrealDB RESTful API Key Table endpoints implementation.
/// </summary>
type SurrealKeyTableEndpoints(config: SurrealConfig, httpClient: HttpClient, jsonOptions: JsonSerializerOptions) =
    do applyConfig config httpClient

    let toSimpleResult responseTask =
        task {
            let! response = responseTask

            return StatementResult.toSimpleResult response.statement
        }

    member this.GetKeyTableResponse<'record>
        (
            table: string,
            ct: CancellationToken
        ) : Task<RestApiSingleResult<'record []>> =
        task {
            let! response =
                keyTable table
                |> executeEmptyRequest<'record []> jsonOptions httpClient ct HttpMethod.Get

            return
                response
                |> RestApiSingleResult.ofRestApiResult
                |> RestApiSingleResult.getMultipleRecords
        }

    member this.GetKeyTable<'record>(table: string, ct: CancellationToken) : Task<RequestResult<'record []>> =
        this.GetKeyTableResponse<'record>(table, ct)
        |> toSimpleResult

    member this.PostPartialKeyTableResponse<'recordData, 'record>
        (
            table: string,
            record: 'recordData,
            ct: CancellationToken
        ) : Task<RestApiSingleResult<'record>> =
        task {
            let! response =
                executeRecordRequest<'recordData, 'record []>
                    jsonOptions
                    httpClient
                    ct
                    HttpMethod.Post
                    (keyTable table)
                    record

            return
                response
                |> RestApiSingleResult.ofRestApiResult
                |> RestApiSingleResult.getRequiredRecord
        }

    member this.PostPartialKeyTable<'recordData, 'record>
        (
            table: string,
            record: 'recordData,
            ct: CancellationToken
        ) : Task<RequestResult<'record>> =
        this.PostPartialKeyTableResponse<'recordData, 'record>(table, record, ct)
        |> toSimpleResult

    member this.PostKeyTableResponse<'record>
        (
            table: string,
            record: 'record,
            ct: CancellationToken
        ) : Task<RestApiSingleResult<'record>> =
        this.PostPartialKeyTableResponse<'record, 'record>(table, record, ct)

    member this.PostKeyTable<'record>
        (
            table: string,
            record: 'record,
            ct: CancellationToken
        ) : Task<RequestResult<'record>> =
        this.PostPartialKeyTable<'record, 'record>(table, record, ct)

    member this.DeleteKeyTableResponse(table: string, ct: CancellationToken) : Task<RestApiSingleResult<unit>> =
        task {
            let! response =
                keyTable table
                |> executeEmptyRequest<unit []> jsonOptions httpClient ct HttpMethod.Delete

            return
                response
                |> RestApiSingleResult.ofRestApiResult
                |> RestApiSingleResult.getNoRecords
        }

    member this.DeleteKeyTable(table: string, ct: CancellationToken) : Task<RequestResult<unit>> =
        this.DeleteKeyTableResponse(table, ct)
        |> toSimpleResult

    member this.GetKeyTableIdResponse<'record>
        (
            table: string,
            id: string,
            ct: CancellationToken
        ) : Task<RestApiSingleResult<'record voption>> =
        task {
            let! response =
                keyTableId table id
                |> executeEmptyRequest<'record []> jsonOptions httpClient ct HttpMethod.Get

            return
                response
                |> RestApiSingleResult.ofRestApiResult
                |> RestApiSingleResult.getOptionalRecord
        }

    member this.GetKeyTableId<'record>(table: string, id: string, ct: CancellationToken) =
        this.GetKeyTableIdResponse<'record>(table, id, ct)
        |> toSimpleResult

    member this.PostPartialKeyTableIdResponse<'recordData, 'record>
        (
            table: string,
            id: string,
            record: 'recordData,
            ct: CancellationToken
        ) : Task<RestApiSingleResult<'record>> =
        task {
            let! response =
                executeRecordRequest<'recordData, 'record []>
                    jsonOptions
                    httpClient
                    ct
                    HttpMethod.Post
                    (keyTableId table id)
                    record

            return
                response
                |> RestApiSingleResult.ofRestApiResult
                |> RestApiSingleResult.getRequiredRecord
        }

    member this.PostPartialKeyTableId<'recordData, 'record>
        (
            table: string,
            id: string,
            record: 'recordData,
            ct: CancellationToken
        ) : Task<RequestResult<'record>> =
        this.PostPartialKeyTableIdResponse<'recordData, 'record>(table, id, record, ct)
        |> toSimpleResult

    member this.PostKeyTableIdResponse<'record>
        (
            table: string,
            id: string,
            record: 'record,
            ct: CancellationToken
        ) : Task<RestApiSingleResult<'record>> =
        this.PostPartialKeyTableIdResponse<'record, 'record>(table, id, record, ct)

    member this.PostKeyTableId<'record>
        (
            table: string,
            id: string,
            record: 'record,
            ct: CancellationToken
        ) : Task<RequestResult<'record>> =
        this.PostPartialKeyTableId<'record, 'record>(table, id, record, ct)

    member this.PutPartialKeyTableIdResponse<'recordData, 'record>
        (
            table: string,
            id: string,
            record: 'recordData,
            ct: CancellationToken
        ) : Task<RestApiSingleResult<'record>> =
        task {
            let! response =
                executeRecordRequest<'recordData, 'record []>
                    jsonOptions
                    httpClient
                    ct
                    HttpMethod.Put
                    (keyTableId table id)
                    record

            return
                response
                |> RestApiSingleResult.ofRestApiResult
                |> RestApiSingleResult.getRequiredRecord
        }

    member this.PutPartialKeyTableId<'recordData, 'record>
        (
            table: string,
            id: string,
            record: 'recordData,
            ct: CancellationToken
        ) : Task<RequestResult<'record>> =
        this.PutPartialKeyTableIdResponse<'recordData, 'record>(table, id, record, ct)
        |> toSimpleResult

    member this.PutKeyTableIdResponse<'record>
        (
            table: string,
            id: string,
            record: 'record,
            ct: CancellationToken
        ) : Task<RestApiSingleResult<'record>> =
        this.PutPartialKeyTableIdResponse<'record, 'record>(table, id, record, ct)

    member this.PutKeyTableId<'record>
        (
            table: string,
            id: string,
            record: 'record,
            ct: CancellationToken
        ) : Task<RequestResult<'record>> =
        this.PutPartialKeyTableId<'record, 'record>(table, id, record, ct)

    member this.PatchPartialKeyTableIdResponse<'recordData, 'record>
        (
            table: string,
            id: string,
            record: 'recordData,
            ct: CancellationToken
        ) : Task<RestApiSingleResult<'record>> =
        task {
            let! response =
                executeRecordRequest<'recordData, 'record []>
                    jsonOptions
                    httpClient
                    ct
                    HttpMethod.Patch
                    (keyTableId table id)
                    record

            return
                response
                |> RestApiSingleResult.ofRestApiResult
                |> RestApiSingleResult.getRequiredRecord
        }

    member this.PatchPartialKeyTableId<'recordData, 'record>
        (
            table: string,
            id: string,
            record: 'recordData,
            ct: CancellationToken
        ) : Task<RequestResult<'record>> =
        this.PatchPartialKeyTableIdResponse<'recordData, 'record>(table, id, record, ct)
        |> toSimpleResult

    member this.DeleteKeyTableIdResponse
        (
            table: string,
            id: string,
            ct: CancellationToken
        ) : Task<RestApiSingleResult<unit>> =
        task {
            let! response =
                keyTableId table id
                |> executeEmptyRequest<obj []> jsonOptions httpClient ct HttpMethod.Delete

            return
                response
                |> RestApiSingleResult.ofRestApiResult
                |> RestApiSingleResult.getNoRecords
        }

    member this.DeleteKeyTableId(table: string, id: string, ct: CancellationToken) : Task<RequestResult<unit>> =
        this.DeleteKeyTableIdResponse(table, id, ct)
        |> toSimpleResult

    member this.Dispose() = httpClient.Dispose()

    interface ISurrealKeyTableEndpoints with
        member this.GetKeyTableResponse<'record>(table: string, ct: CancellationToken) =
            this.GetKeyTableResponse<'record>(table, ct)

        member this.GetKeyTable<'record>(table: string, ct: CancellationToken) = this.GetKeyTable<'record>(table, ct)

        member this.PostPartialKeyTableResponse<'recordData, 'record>(table: string, record: 'recordData, ct: CancellationToken) =
            this.PostPartialKeyTableResponse<'recordData, 'record>(table, record, ct)

        member this.PostPartialKeyTable<'recordData, 'record>(table: string, record: 'recordData, ct: CancellationToken) =
            this.PostPartialKeyTable<'recordData, 'record>(table, record, ct)

        member this.PostKeyTableResponse<'record>(table: string, record: 'record, ct: CancellationToken) =
            this.PostKeyTableResponse<'record>(table, record, ct)

        member this.PostKeyTable<'record>(table: string, record: 'record, ct: CancellationToken) =
            this.PostKeyTable<'record>(table, record, ct)

        member this.DeleteKeyTableResponse(table: string, ct: CancellationToken) =
            this.DeleteKeyTableResponse(table, ct)

        member this.DeleteKeyTable(table: string, ct: CancellationToken) = this.DeleteKeyTable(table, ct)

        member this.GetKeyTableIdResponse<'record>(table: string, id: string, ct: CancellationToken) =
            this.GetKeyTableIdResponse<'record>(table, id, ct)

        member this.GetKeyTableId<'record>(table: string, id: string, ct: CancellationToken) =
            this.GetKeyTableId<'record>(table, id, ct)

        member this.PostPartialKeyTableIdResponse<'recordData, 'record>
            (
                table: string,
                id: string,
                record: 'recordData,
                ct: CancellationToken
            ) =
            this.PostPartialKeyTableIdResponse<'recordData, 'record>(table, id, record, ct)

        member this.PostPartialKeyTableId<'recordData, 'record>
            (
                table: string,
                id: string,
                record: 'recordData,
                ct: CancellationToken
            ) =
            this.PostPartialKeyTableId<'recordData, 'record>(table, id, record, ct)

        member this.PostKeyTableIdResponse<'record>(table: string, id: string, record: 'record, ct: CancellationToken) =
            this.PostKeyTableIdResponse<'record>(table, id, record, ct)

        member this.PostKeyTableId<'record>(table: string, id: string, record: 'record, ct: CancellationToken) =
            this.PostKeyTableId<'record>(table, id, record, ct)

        member this.PutPartialKeyTableIdResponse<'recordData, 'record>
            (
                table: string,
                id: string,
                record: 'recordData,
                ct: CancellationToken
            ) =
            this.PutPartialKeyTableIdResponse<'recordData, 'record>(table, id, record, ct)

        member this.PutPartialKeyTableId<'recordData, 'record>
            (
                table: string,
                id: string,
                record: 'recordData,
                ct: CancellationToken
            ) =
            this.PutPartialKeyTableId<'recordData, 'record>(table, id, record, ct)

        member this.PutKeyTableIdResponse<'record>(table: string, id: string, record: 'record, ct: CancellationToken) =
            this.PutKeyTableIdResponse<'record>(table, id, record, ct)

        member this.PutKeyTableId<'record>(table: string, id: string, record: 'record, ct: CancellationToken) =
            this.PutKeyTableId<'record>(table, id, record, ct)

        member this.PatchPartialKeyTableIdResponse<'recordData, 'record>
            (
                table: string,
                id: string,
                record: 'recordData,
                ct: CancellationToken
            ) =
            this.PatchPartialKeyTableIdResponse<'recordData, 'record>(table, id, record, ct)

        member this.PatchPartialKeyTableId<'recordData, 'record>
            (
                table: string,
                id: string,
                record: 'recordData,
                ct: CancellationToken
            ) =
            this.PatchPartialKeyTableId<'recordData, 'record>(table, id, record, ct)

        member this.DeleteKeyTableIdResponse(table: string, id: string, ct: CancellationToken) =
            this.DeleteKeyTableIdResponse(table, id, ct)

        member this.DeleteKeyTableId(table: string, id: string, ct: CancellationToken) =
            this.DeleteKeyTableId(table, id, ct)

        member this.Dispose() = this.Dispose()
