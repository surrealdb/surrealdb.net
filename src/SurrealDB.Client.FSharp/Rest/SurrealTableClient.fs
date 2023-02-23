namespace SurrealDB.Client.FSharp.Rest

open System.Net.Http
open System.Text.Json
open System.Threading
open System.Threading.Tasks

open SurrealDB.Client.FSharp


/// <summary>
/// The SurrealDB RESTful API Key Table endpoints using record types.
/// </summary>
type ISurrealTableClient =
    inherit System.IDisposable

    /// <summary>
    /// This HTTP RESTful endpoint selects all records in a specific table in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#select-all"/>
    /// <typeparam name="record">The type of the records to return.</typeparam>
    /// <param name="table">The table to select from.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An API result with a single statement with given records, or an error.</returns>
    abstract ListResponseAsync<'record> : table: string * ct: CancellationToken -> Task<RestApiSingleResult<'record []>>

    /// <summary>
    /// This HTTP RESTful endpoint selects all records in a specific table in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#select-all"/>
    /// <typeparam name="record">The type of the records to return.</typeparam>
    /// <param name="table">The table to select from.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The records as records of type 'record, or an error.</returns>
    abstract ListAsync<'record> : table: string * ct: CancellationToken -> Task<RequestResult<'record []>>


    /// <summary>
    /// This HTTP RESTful endpoint creates a single record in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#create-all"/>
    /// <typeparam name="data">The type of the record to create, as a partial record with no id field.</typeparam>
    /// <typeparam name="record">The type of the record to return, as a full record with an id field.</typeparam>
    /// <param name="table">The table to create the record in.</param>
    /// <param name="record">The record to create as a JSON Node.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An API result with a single statement with the created record, or an error.</returns>
    abstract CreatePartialResponseAsync<'data, 'record> :
        table: string * record: 'data * ct: CancellationToken -> Task<RestApiSingleResult<'record>>

    /// <summary>
    /// This HTTP RESTful endpoint creates a single record in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#create-all"/>
    /// <typeparam name="data">The type of the record to create, as a partial record with no id field.</typeparam>
    /// <typeparam name="record">The type of the record to return, as a full record with an id field.</typeparam>
    /// <param name="table">The table to create the record in.</param>
    /// <param name="record">The record to create as a JSON Node.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created record as a record of type 'record, or an error.</returns>
    abstract CreatePartialAsync<'data, 'record> :
        table: string * record: 'data * ct: CancellationToken -> Task<RequestResult<'record>>

    /// <summary>
    /// This HTTP RESTful endpoint creates a single record in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#create-all"/>
    /// <typeparam name="record">The type of the record to create and return.</typeparam>
    /// <param name="table">The table to create the record in.</param>
    /// <param name="record">The record to create as a partial record of type 'record.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An API result with a single statement with the created record, or an error.</returns>
    abstract CreateResponseAsync<'record> :
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
    abstract CreateAsync<'record> :
        table: string * record: 'record * ct: CancellationToken -> Task<RequestResult<'record>>

    /// <summary>
    /// This HTTP RESTful endpoint deletes all records in a specific table in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#delete-all"/>
    /// <param name="table">The table to delete from.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An API result with a single statement with no records, or an error.</returns>
    abstract DeleteAllResponseAsync : table: string * ct: CancellationToken -> Task<RestApiSingleResult<unit>>

    /// <summary>
    /// This HTTP RESTful endpoint deletes all records in a specific table in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#delete-all"/>
    /// <param name="table">The table to delete from.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An empty result, or an error.</returns>
    abstract DeleteAllAsync : table: string * ct: CancellationToken -> Task<RequestResult<unit>>

    /// <summary>
    /// This HTTP RESTful endpoint selects a single record in a specific table in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#select-one"/>
    /// <typeparam name="record">The type of the record to return.</typeparam>
    /// <param name="table">The table to select from.</param>
    /// <param name="id">The id of the record to select.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An API result with a single statement with the optional record, or an error.</returns>
    abstract FindResponseAsync<'record> :
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
    abstract FindAsync<'record> :
        table: string * id: string * ct: CancellationToken -> Task<RequestResult<'record voption>>

    /// <summary>
    /// This HTTP RESTful endpoint creates a single record in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#create-one"/>
    /// <typeparam name="data">The type of the record to create, as a partial record with no id field.</typeparam>
    /// <typeparam name="record">The type of the record to return, as a full record with an id field.</typeparam>
    /// <param name="table">The table to create the record in.</param>
    /// <param name="id">The id of the record to create.</param>
    /// <param name="record">The record to create as a partial record of type 'data.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An API result with a single statement with the created record, or an error.</returns>
    abstract InsertPartialResponseAsync<'data, 'record> :
        table: string * id: string * record: 'data * ct: CancellationToken -> Task<RestApiSingleResult<'record>>

    /// <summary>
    /// This HTTP RESTful endpoint creates a single record in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#create-one"/>
    /// <typeparam name="data">The type of the record to create, as a partial record with no id field.</typeparam>
    /// <typeparam name="record">The type of the record to return, as a full record with an id field.</typeparam>
    /// <param name="table">The table to create the record in.</param>
    /// <param name="id">The id of the record to create.</param>
    /// <param name="record">The record to create as a partial record of type 'data.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created record as a record of type 'record, or an error.</returns>
    abstract InsertPartialAsync<'data, 'record> :
        table: string * id: string * record: 'data * ct: CancellationToken -> Task<RequestResult<'record>>

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
    abstract InsertResponseAsync<'record> :
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
    abstract InsertAsync<'record> :
        table: string * id: string * record: 'record * ct: CancellationToken -> Task<RequestResult<'record>>

    /// <summary>
    /// This HTTP RESTful endpoint creates or updates a single specific record in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#update-one"/>
    /// <typeparam name="data">The type of the record to create or update, as a partial record with no id field.</typeparam>
    /// <typeparam name="record">The type of the record to return, as a full record with an id field.</typeparam>
    /// <param name="table">The table to create or update the record in.</param>
    /// <param name="id">The id of the record to create or update.</param>
    /// <param name="record">The record to create or update as a partial record of type 'data.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An API result with a single statement with the created or updated record, or an error.</returns>
    abstract ReplacePartialResponseAsync<'data, 'record> :
        table: string * id: string * record: 'data * ct: CancellationToken -> Task<RestApiSingleResult<'record>>

    /// <summary>
    /// This HTTP RESTful endpoint creates or updates a single specific record in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#update-one"/>
    /// <typeparam name="data">The type of the record to create or update, as a partial record with no id field.</typeparam>
    /// <typeparam name="record">The type of the record to return, as a full record with an id field.</typeparam>
    /// <param name="table">The table to create or update the record in.</param>
    /// <param name="id">The id of the record to create or update.</param>
    /// <param name="record">The record to create or update as a partial record of type 'data.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The created or updated record as a record of type 'record, or an error.</returns>
    abstract ReplacePartialAsync<'data, 'record> :
        table: string * id: string * record: 'data * ct: CancellationToken -> Task<RequestResult<'record>>

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
    abstract ReplaceResponseAsync<'record> :
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
    abstract ReplaceAsync<'record> :
        table: string * id: string * record: 'record * ct: CancellationToken -> Task<RequestResult<'record>>

    /// <summary>
    /// This HTTP RESTful endpoint creates or modify a single specific record in the database.
    /// If the record already exists, then only the specified fields will be updated.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#modify-one"/>
    /// <typeparam name="data">The type of the record to create or update, as a partial record with just the fields to update.</typeparam>
    /// <typeparam name="record">The type of the record to return, as a full record with all fields.</typeparam>
    /// <param name="table">The table to update the record in.</param>
    /// <param name="id">The id of the record to update.</param>
    /// <param name="record">The record to update as a partial record.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An API result with a single statement with the updated record, or an error.</returns>
    abstract ModifyPartialResponseAsync<'data, 'record> :
        table: string * id: string * record: 'data * ct: CancellationToken -> Task<RestApiSingleResult<'record>>

    /// <summary>
    /// This HTTP RESTful endpoint creates or modify a single specific record in the database.
    /// If the record already exists, then only the specified fields will be updated.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#modify-one"/>
    /// <typeparam name="data">The type of the record to create or update, as a partial record with just the fields to update.</typeparam>
    /// <typeparam name="record">The type of the record to return, as a full record with all fields.</typeparam>
    /// <param name="table">The table to update the record in.</param>
    /// <param name="id">The id of the record to update.</param>
    /// <param name="record">The record to update as a partial record.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The updated record as a record of type 'record, or an error.</returns>
    abstract ModifyPartialAsync<'data, 'record> :
        table: string * id: string * record: 'data * ct: CancellationToken -> Task<RequestResult<'record>>

    /// <summary>
    /// This HTTP RESTful endpoint deletes a single specific record in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#delete-one"/>
    /// <param name="table">The table to delete the record from.</param>
    /// <param name="id">The id of the record to delete.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>An API result with a single statement with no data, or an error.</returns>
    abstract DeleteResponseAsync : table: string * id: string * ct: CancellationToken -> Task<RestApiSingleResult<unit>>

    /// <summary>
    /// This HTTP RESTful endpoint deletes a single specific record in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#delete-one"/>
    /// <param name="table">The table to delete the record from.</param>
    /// <param name="id">The id of the record to delete.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>Unit, or an error.</returns>
    abstract DeleteAsync : table: string * id: string * ct: CancellationToken -> Task<RequestResult<unit>>

/// <summary>
/// The SurrealDB RESTful API Key Table endpoints implementation.
/// </summary>
type SurrealTableClient(config: SurrealConfig, httpClient: HttpClient, jsonOptions: JsonSerializerOptions) =
    do applyConfig config httpClient

    member this.ListResponseAsync<'record>
        (
            table: string,
            ct: CancellationToken
        ) : Task<RestApiSingleResult<'record []>> =
        keyTable table
        |> executeEmptyRequest<'record []> jsonOptions httpClient ct HttpMethod.Get
        |> toTableResponse RestApiSingleResult.getMultipleRecords

    member this.ListAsync<'record>(table: string, ct: CancellationToken) : Task<RequestResult<'record []>> =
        this.ListResponseAsync<'record>(table, ct)
        |> toSimpleResult

    member this.CreatePartialResponseAsync<'data, 'record>
        (
            table: string,
            record: 'data,
            ct: CancellationToken
        ) : Task<RestApiSingleResult<'record>> =
        record
        |> executeRecordRequest<'data, 'record []> jsonOptions httpClient ct HttpMethod.Post (keyTable table)
        |> toTableResponse RestApiSingleResult.getRequiredRecord

    member this.CreatePartialAsync<'data, 'record>
        (
            table: string,
            record: 'data,
            ct: CancellationToken
        ) : Task<RequestResult<'record>> =
        this.CreatePartialResponseAsync<'data, 'record>(table, record, ct)
        |> toSimpleResult

    member this.CreateResponseAsync<'record>
        (
            table: string,
            record: 'record,
            ct: CancellationToken
        ) : Task<RestApiSingleResult<'record>> =
        this.CreatePartialResponseAsync<'record, 'record>(table, record, ct)

    member this.CreateAsync<'record>
        (
            table: string,
            record: 'record,
            ct: CancellationToken
        ) : Task<RequestResult<'record>> =
        this.CreatePartialAsync<'record, 'record>(table, record, ct)

    member this.DeleteAllResponseAsync(table: string, ct: CancellationToken) : Task<RestApiSingleResult<unit>> =
        keyTable table
        |> executeEmptyRequest<unit []> jsonOptions httpClient ct HttpMethod.Delete
        |> toTableResponse RestApiSingleResult.getNoRecords

    member this.DeleteAllAsync(table: string, ct: CancellationToken) : Task<RequestResult<unit>> =
        this.DeleteAllResponseAsync(table, ct)
        |> toSimpleResult

    member this.FindResponseAsync<'record>
        (
            table: string,
            id: string,
            ct: CancellationToken
        ) : Task<RestApiSingleResult<'record voption>> =
        keyTableId table id
        |> executeEmptyRequest<'record []> jsonOptions httpClient ct HttpMethod.Get
        |> toTableResponse RestApiSingleResult.getOptionalRecord

    member this.FindAsync<'record>(table: string, id: string, ct: CancellationToken) =
        this.FindResponseAsync<'record>(table, id, ct)
        |> toSimpleResult

    member this.InsertPartialResponseAsync<'data, 'record>
        (
            table: string,
            id: string,
            record: 'data,
            ct: CancellationToken
        ) : Task<RestApiSingleResult<'record>> =
        record
        |> executeRecordRequest<'data, 'record []> jsonOptions httpClient ct HttpMethod.Post (keyTableId table id)
        |> toTableResponse RestApiSingleResult.getRequiredRecord

    member this.InsertPartialAsync<'data, 'record>
        (
            table: string,
            id: string,
            record: 'data,
            ct: CancellationToken
        ) : Task<RequestResult<'record>> =
        this.InsertPartialResponseAsync<'data, 'record>(table, id, record, ct)
        |> toSimpleResult

    member this.InsertResponseAsync<'record>
        (
            table: string,
            id: string,
            record: 'record,
            ct: CancellationToken
        ) : Task<RestApiSingleResult<'record>> =
        this.InsertPartialResponseAsync<'record, 'record>(table, id, record, ct)

    member this.InsertAsync<'record>
        (
            table: string,
            id: string,
            record: 'record,
            ct: CancellationToken
        ) : Task<RequestResult<'record>> =
        this.InsertPartialAsync<'record, 'record>(table, id, record, ct)

    member this.ReplacePartialResponseAsync<'data, 'record>
        (
            table: string,
            id: string,
            record: 'data,
            ct: CancellationToken
        ) : Task<RestApiSingleResult<'record>> =
        record
        |> executeRecordRequest<'data, 'record []> jsonOptions httpClient ct HttpMethod.Put (keyTableId table id)
        |> toTableResponse RestApiSingleResult.getRequiredRecord

    member this.ReplacePartialAsync<'data, 'record>
        (
            table: string,
            id: string,
            record: 'data,
            ct: CancellationToken
        ) : Task<RequestResult<'record>> =
        this.ReplacePartialResponseAsync<'data, 'record>(table, id, record, ct)
        |> toSimpleResult

    member this.ReplaceResponseAsync<'record>
        (
            table: string,
            id: string,
            record: 'record,
            ct: CancellationToken
        ) : Task<RestApiSingleResult<'record>> =
        this.ReplacePartialResponseAsync<'record, 'record>(table, id, record, ct)

    member this.ReplaceAsync<'record>
        (
            table: string,
            id: string,
            record: 'record,
            ct: CancellationToken
        ) : Task<RequestResult<'record>> =
        this.ReplacePartialAsync<'record, 'record>(table, id, record, ct)

    member this.ModifyPartialResponseAsync<'data, 'record>
        (
            table: string,
            id: string,
            record: 'data,
            ct: CancellationToken
        ) : Task<RestApiSingleResult<'record>> =
        record
        |> executeRecordRequest<'data, 'record []> jsonOptions httpClient ct HttpMethod.Patch (keyTableId table id)
        |> toTableResponse RestApiSingleResult.getRequiredRecord

    member this.ModifyPartialAsync<'data, 'record>
        (
            table: string,
            id: string,
            record: 'data,
            ct: CancellationToken
        ) : Task<RequestResult<'record>> =
        this.ModifyPartialResponseAsync<'data, 'record>(table, id, record, ct)
        |> toSimpleResult

    member this.DeleteResponseAsync
        (
            table: string,
            id: string,
            ct: CancellationToken
        ) : Task<RestApiSingleResult<unit>> =
        keyTableId table id
        |> executeEmptyRequest<obj []> jsonOptions httpClient ct HttpMethod.Delete
        |> toTableResponse RestApiSingleResult.getNoRecords

    member this.DeleteAsync(table: string, id: string, ct: CancellationToken) : Task<RequestResult<unit>> =
        this.DeleteResponseAsync(table, id, ct)
        |> toSimpleResult

    member this.Dispose() = httpClient.Dispose()

    interface ISurrealTableClient with
        member this.ListResponseAsync<'record>(table: string, ct: CancellationToken) =
            this.ListResponseAsync<'record>(table, ct)

        member this.ListAsync<'record>(table: string, ct: CancellationToken) = this.ListAsync<'record>(table, ct)

        member this.CreatePartialResponseAsync<'data, 'record>(table: string, record: 'data, ct: CancellationToken) =
            this.CreatePartialResponseAsync<'data, 'record>(table, record, ct)

        member this.CreatePartialAsync<'data, 'record>(table: string, record: 'data, ct: CancellationToken) =
            this.CreatePartialAsync<'data, 'record>(table, record, ct)

        member this.CreateResponseAsync<'record>(table: string, record: 'record, ct: CancellationToken) =
            this.CreateResponseAsync<'record>(table, record, ct)

        member this.CreateAsync<'record>(table: string, record: 'record, ct: CancellationToken) =
            this.CreateAsync<'record>(table, record, ct)

        member this.DeleteAllResponseAsync(table: string, ct: CancellationToken) =
            this.DeleteAllResponseAsync(table, ct)

        member this.DeleteAllAsync(table: string, ct: CancellationToken) = this.DeleteAllAsync(table, ct)

        member this.FindResponseAsync<'record>(table: string, id: string, ct: CancellationToken) =
            this.FindResponseAsync<'record>(table, id, ct)

        member this.FindAsync<'record>(table: string, id: string, ct: CancellationToken) =
            this.FindAsync<'record>(table, id, ct)

        member this.InsertPartialResponseAsync<'data, 'record>
            (
                table: string,
                id: string,
                record: 'data,
                ct: CancellationToken
            ) =
            this.InsertPartialResponseAsync<'data, 'record>(table, id, record, ct)

        member this.InsertPartialAsync<'data, 'record>
            (
                table: string,
                id: string,
                record: 'data,
                ct: CancellationToken
            ) =
            this.InsertPartialAsync<'data, 'record>(table, id, record, ct)

        member this.InsertResponseAsync<'record>(table: string, id: string, record: 'record, ct: CancellationToken) =
            this.InsertResponseAsync<'record>(table, id, record, ct)

        member this.InsertAsync<'record>(table: string, id: string, record: 'record, ct: CancellationToken) =
            this.InsertAsync<'record>(table, id, record, ct)

        member this.ReplacePartialResponseAsync<'data, 'record>
            (
                table: string,
                id: string,
                record: 'data,
                ct: CancellationToken
            ) =
            this.ReplacePartialResponseAsync<'data, 'record>(table, id, record, ct)

        member this.ReplacePartialAsync<'data, 'record>
            (
                table: string,
                id: string,
                record: 'data,
                ct: CancellationToken
            ) =
            this.ReplacePartialAsync<'data, 'record>(table, id, record, ct)

        member this.ReplaceResponseAsync<'record>(table: string, id: string, record: 'record, ct: CancellationToken) =
            this.ReplaceResponseAsync<'record>(table, id, record, ct)

        member this.ReplaceAsync<'record>(table: string, id: string, record: 'record, ct: CancellationToken) =
            this.ReplaceAsync<'record>(table, id, record, ct)

        member this.ModifyPartialResponseAsync<'data, 'record>
            (
                table: string,
                id: string,
                record: 'data,
                ct: CancellationToken
            ) =
            this.ModifyPartialResponseAsync<'data, 'record>(table, id, record, ct)

        member this.ModifyPartialAsync<'data, 'record>
            (
                table: string,
                id: string,
                record: 'data,
                ct: CancellationToken
            ) =
            this.ModifyPartialAsync<'data, 'record>(table, id, record, ct)

        member this.DeleteResponseAsync(table: string, id: string, ct: CancellationToken) =
            this.DeleteResponseAsync(table, id, ct)

        member this.DeleteAsync(table: string, id: string, ct: CancellationToken) = this.DeleteAsync(table, id, ct)

        member this.Dispose() = this.Dispose()
