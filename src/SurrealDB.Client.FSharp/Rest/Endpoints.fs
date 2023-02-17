module SurrealDB.Client.FSharp.Rest.Endpoints

open System
open System.Net.Http
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open System.Threading
open System.Threading.Tasks

open SurrealDB.Client.FSharp

[<AutoOpen>]
module internal Internals =
    [<Literal>]
    let SQL_ENDPOINT = "sql"

    let keyTable table = $"key/%s{table}"
    let keyTableId table id = $"key/%s{table}/%s{id}"

    let parseHeaderInfo (response: HttpResponseMessage) =
        let readHeader name defaultValue =
            match response.Headers.TryGetValues name with
            | true, values -> Seq.tryHeadValue values
            | _ -> ValueNone
            |> ValueOption.defaultValue defaultValue

        let version = readHeader VERSION_HEADER ""
        let server = readHeader SERVER_HEADER ""
        let date = readHeader DATE_HEADER ""
        let status = response.StatusCode

        { version = version
          server = server
          date = date
          status = status }

    let updateDefaultHeader key value (httpClient: HttpClient) =
        httpClient.DefaultRequestHeaders.Remove(key)
        |> ignore

        httpClient.DefaultRequestHeaders.Add(key, (value: string))

    let applyCredentialHeaders credentials httpClient =
        match credentials with
        | SurrealCredentials.Basic (user, password) ->
            let auth =
                String.toBase64 <| sprintf "%s:%s" user password

            let value = sprintf "%s %s" BASIC_SCHEME auth
            updateDefaultHeader AUTHORIZATION_HEADER value httpClient

        | SurrealCredentials.Bearer jwt ->
            let value = sprintf "%s %s" BEARER_SCHEME jwt
            updateDefaultHeader AUTHORIZATION_HEADER value httpClient

    type StatementJson<'result> =
        { status: string
          detail: string option
          result: 'result option
          time: string }

    let parseApiResult<'result>
        (jsonOptions: JsonSerializerOptions)
        (cancellationToken: CancellationToken)
        (response: HttpResponseMessage)
        =
        task {
            let headers = parseHeaderInfo response
            let! content = response.Content.ReadAsStringAsync(cancellationToken)

            let result =
                if response.IsSuccessStatusCode then
                    JsonSerializer.Deserialize<StatementJson<'result> []>(content, jsonOptions)
                    |> Array.map (fun json ->
                        match json.result, json.detail with
                        | Some result, _ ->
                            { time = json.time
                              status = json.status
                              response = Ok result }
                        | None, Some detail ->
                            { time = json.time
                              status = json.status
                              response = Error detail }
                        | None, None ->
                            { time = json.time
                              status = json.status
                              response = Error "Missing result and detail" })
                    |> Ok
                else
                    JsonSerializer.Deserialize<ErrorInfo>(content, jsonOptions)
                    |> Error

            let response: RestApiResult<'result> = { headers = headers; result = result }

            return response
        }

    let executeRequest<'result>
        (jsonOptions: JsonSerializerOptions)
        (httpClient: HttpClient)
        (ct: CancellationToken)
        (request: HttpRequestMessage)
        =
        task {
            let! response = httpClient.SendAsync(request, ct)
            let! result = parseApiResult<'result> jsonOptions ct response
            return result
        }

    let executeEmptyRequest<'result> jsonOptions httpClient ct method url =
        task {
            let url = Uri(url, UriKind.Relative)
            let request = new HttpRequestMessage(method, url)
            return! executeRequest<'result> jsonOptions httpClient ct request
        }

    let executeRecordRequest<'input, 'result> jsonOptions httpClient ct method url input =
        task {
            let url = Uri(url, UriKind.Relative)
            let request = new HttpRequestMessage(method, url)

            let json =
                JsonSerializer.Serialize<'input>(input, (jsonOptions: JsonSerializerOptions))

            request.Content <- new StringContent(json, Encoding.UTF8, APPLICATION_JSON)

            return! executeRequest<'result> jsonOptions httpClient ct request
        }

    let executeJsonRequest<'result> jsonOptions httpClient ct method url json =
        executeRecordRequest<JsonNode, 'result> jsonOptions httpClient ct method url json
    //task {
    //    let url = Uri(url, UriKind.Relative)
    //    let request = new HttpRequestMessage(method, url)

    //    let json =
    //        JsonSerializer.Serialize<JsonNode>(json, (jsonOptions: JsonSerializerOptions))

    //    request.Content <- new StringContent(json, Encoding.UTF8, APPLICATION_JSON)

    //    return! executeRequest<'result> jsonOptions httpClient ct request
    //}

    let executeTextRequest<'result> jsonOptions httpClient ct method url text =
        task {
            let url = Uri(url, UriKind.Relative)
            let request = new HttpRequestMessage(method, url)
            request.Content <- new StringContent(text, Encoding.UTF8, TEXT_PLAIN)
            return! executeRequest<'result> jsonOptions httpClient ct request
        }

    let tryGetSingleStatement (response: ApiResult<'result>) : ApiSingleResult<'result> =
        match response with
        | Error err -> Error(ResponseError err)
        | Ok statements when statements.Length = 1 -> Ok statements.[0]
        | _ -> Error(ProtocolError ExpectedSingleStatement)

    let asSingleApiResult<'result> (response: RestApiResult<'result>) =
        let singleResult =
            response.result |> tryGetSingleStatement

        let singleResult: RestApiSingleResult<'result> =
            { headers = response.headers
              result = singleResult }

        singleResult

/// <summary>
/// Applies the configuration to the <see cref="HttpClient"/>.
/// </summary>
/// <param name="config">The configuration to apply.</param>
/// <param name="httpClient">The HTTP client to update.</param>
/// <remarks>
/// Assigns BaseAddress to <see cref="SurrealConfig.BaseUrl"/>
/// Affects the following headers:
/// - Accept: application/json
/// - NS: <see cref="SurrealConfig.Namespace"/>
/// - DB: <see cref="SurrealConfig.Database"/>
/// - Authorization: <see cref="SurrealConfig.Credentials"/>
/// </remarks>
let applyConfig (config: SurrealConfig) (httpClient: HttpClient) =
    updateDefaultHeader ACCEPT_HEADER APPLICATION_JSON httpClient

    match config.Credentials with
    | ValueSome credentials -> applyCredentialHeaders credentials httpClient
    | ValueNone -> ()

    updateDefaultHeader NS_HEADER config.Namespace httpClient
    updateDefaultHeader DB_HEADER config.Database httpClient

    httpClient.BaseAddress <- Uri(config.BaseUrl, UriKind.Absolute)

/// <summary>
/// The SQL endpoint enables advanced SurrealQL queries.
/// </summary>
/// <see href="https://surrealdb.com/docs/integration/http#sql"/>
/// <param name="jsonOptions">The JSON options to use.</param>
/// <param name="httpClient">The HTTP client to use.</param>
/// <param name="ct">The cancellation token.</param>
/// <param name="query">The query to execute.</param>
/// <returns>The records as a JSON Node.</returns>
/// <remarks>
/// Assumes the HTTP client has the correct headers already set.
/// </remarks>
let postSql
    (jsonOptions: JsonSerializerOptions)
    (httpClient: HttpClient)
    (ct: CancellationToken)
    (query: string)
    : Task<RestApiResult<JsonNode>> =
    executeTextRequest<JsonNode> jsonOptions httpClient ct HttpMethod.Post SQL_ENDPOINT query

/// <summary>
/// This HTTP RESTful endpoint selects all records in a specific table in the database.
/// </summary>
/// <see href="https://surrealdb.com/docs/integration/http#select-all"/>
/// <param name="jsonOptions">The JSON options to use.</param>
/// <param name="httpClient">The HTTP client to use.</param>
/// <param name="ct">The cancellation token.</param>
/// <param name="table">The table to select from.</param>
/// <returns>The records as a JSON Node.</returns>
/// <remarks>
/// Assumes the HTTP client has the correct headers already set.
/// </remarks>
let getKeyTable
    (jsonOptions: JsonSerializerOptions)
    (httpClient: HttpClient)
    (ct: CancellationToken)
    (table: string)
    : Task<RestApiResult<JsonNode>> =
    executeEmptyRequest<JsonNode> jsonOptions httpClient ct HttpMethod.Get (keyTable table)

/// <summary>
/// This HTTP RESTful endpoint creates a record in a specific table in the database.
/// </summary>
/// <see href="https://surrealdb.com/docs/integration/http#create-all"/>
/// <param name="jsonOptions">The JSON options to use.</param>
/// <param name="httpClient">The HTTP client to use.</param>
/// <param name="ct">The cancellation token.</param>
/// <param name="table">The table to create the record in.</param>
/// <param name="record">The record to create as a JSON Node.</param>
/// <returns>The created record as a JSON Node.</returns>
/// <remarks>
/// Assumes the HTTP client has the correct headers already set.
/// </remarks>
let postKeyTable
    (jsonOptions: JsonSerializerOptions)
    (httpClient: HttpClient)
    (ct: CancellationToken)
    (table: string)
    (record: JsonNode)
    : Task<RestApiResult<JsonNode>> =
    executeJsonRequest<JsonNode> jsonOptions httpClient ct HttpMethod.Post (keyTable table) record

/// <summary>
/// This HTTP RESTful endpoint deletes all records from the specified table in the database.
/// </summary>
/// <see href="https://surrealdb.com/docs/integration/http#delete-all"/>
/// <param name="jsonOptions">The JSON options to use.</param>
/// <param name="httpClient">The HTTP client to use.</param>
/// <param name="ct">The cancellation token.</param>
/// <param name="table">The table to delete all records from.</param>
/// <returns>An empty array as a JSON Node.</returns>
/// <remarks>
/// Assumes the HTTP client has the correct headers already set.
/// </remarks>
let deleteKeyTable
    (jsonOptions: JsonSerializerOptions)
    (httpClient: HttpClient)
    (ct: CancellationToken)
    (table: string)
    : Task<RestApiResult<JsonNode>> =
    keyTable table
    |> executeEmptyRequest<JsonNode> jsonOptions httpClient ct HttpMethod.Delete

/// <summary>
/// This HTTP RESTful endpoint selects a specific record from the database.
/// </summary>
/// <see href="https://surrealdb.com/docs/integration/http#select-one"/>
/// <param name="jsonOptions">The JSON options to use.</param>
/// <param name="httpClient">The HTTP client to use.</param>
/// <param name="ct">The cancellation token.</param>
/// <param name="table">The table to select the record from.</param>
/// <param name="id">The id of the record to select.</param>
/// <returns>An array with the record (or empty) as a JSON Node.</returns>
/// <remarks>
/// Assumes the HTTP client has the correct headers already set.
/// </remarks>
let getKeyTableId
    (jsonOptions: JsonSerializerOptions)
    (httpClient: HttpClient)
    (ct: CancellationToken)
    (table: string)
    (id: string)
    : Task<RestApiResult<JsonNode>> =
    keyTableId table id
    |> executeEmptyRequest<JsonNode> jsonOptions httpClient ct HttpMethod.Get

/// <summary>
/// This HTTP RESTful endpoint creates a single specific record into the database.
/// </summary>
/// <see href="https://surrealdb.com/docs/integration/http#create-one"/>
/// <param name="jsonOptions">The JSON options to use.</param>
/// <param name="httpClient">The HTTP client to use.</param>
/// <param name="ct">The cancellation token.</param>
/// <param name="table">The table to create the record in.</param>
/// <param name="id">The id of the record to create.</param>
/// <param name="record">The record to create as a JSON Node.</param>
/// <returns>The created record as a JSON Node.</returns>
/// <remarks>
/// Assumes the HTTP client has the correct headers already set.
/// </remarks>
let postKeyTableId
    (jsonOptions: JsonSerializerOptions)
    (httpClient: HttpClient)
    (ct: CancellationToken)
    (table: string)
    (id: string)
    (record: JsonNode)
    : Task<RestApiResult<JsonNode>> =
    executeJsonRequest<JsonNode> jsonOptions httpClient ct HttpMethod.Post (keyTableId table id) record

/// <summary>
/// This HTTP RESTful endpoint creates or updates a single specific record in the database.
/// </summary>
/// <see href="https://surrealdb.com/docs/integration/http#update-one"/>
/// <param name="jsonOptions">The JSON options to use.</param>
/// <param name="httpClient">The HTTP client to use.</param>
/// <param name="ct">The cancellation token.</param>
/// <param name="table">The table to create or update the record in.</param>
/// <param name="id">The id of the record to create or update.</param>
/// <param name="record">The record to create or update as a JSON Node.</param>
/// <returns>The created or updated record as a JSON Node.</returns>
/// <remarks>
/// Assumes the HTTP client has the correct headers already set.
/// </remarks>
let putKeyTableId
    (jsonOptions: JsonSerializerOptions)
    (httpClient: HttpClient)
    (ct: CancellationToken)
    (table: string)
    (id: string)
    (record: JsonNode)
    : Task<RestApiResult<JsonNode>> =
    executeJsonRequest<JsonNode> jsonOptions httpClient ct HttpMethod.Put (keyTableId table id) record

/// <summary>
/// This HTTP RESTful endpoint creates or updates a single specific record in the database.
/// If the record already exists, then only the specified fields will be updated.
/// </summary>
/// <see href="https://surrealdb.com/docs/integration/http#modify-one"/>
/// <param name="jsonOptions">The JSON options to use.</param>
/// <param name="httpClient">The HTTP client to use.</param>
/// <param name="ct">The cancellation token.</param>
/// <param name="table">The table to update the record in.</param>
/// <param name="id">The id of the record to update.</param>
/// <param name="record">The partial record to update as a JSON Node.</param>
/// <returns>The updated record as a JSON Node.</returns>
/// <remarks>
/// Assumes the HTTP client has the correct headers already set.
/// </remarks>
let patchKeyTableId
    (jsonOptions: JsonSerializerOptions)
    (httpClient: HttpClient)
    (ct: CancellationToken)
    (table: string)
    (id: string)
    (record: JsonNode)
    : Task<RestApiResult<JsonNode>> =
    executeJsonRequest<JsonNode> jsonOptions httpClient ct HttpMethod.Patch (keyTableId table id) record

/// <summary>
/// This HTTP RESTful endpoint deletes a single specific record from the database.
/// </summary>
/// <see href="https://surrealdb.com/docs/integration/http#delete-one"/>
/// <param name="jsonOptions">The JSON options to use.</param>
/// <param name="httpClient">The HTTP client to use.</param>
/// <param name="ct">The cancellation token.</param>
/// <param name="table">The table to delete the record from.</param>
/// <param name="id">The id of the record to delete.</param>
/// <returns>An empty array as a JSON Node.</returns>
/// <remarks>
/// Assumes the HTTP client has the correct headers already set.
/// </remarks>
let deleteKeyTableId
    (jsonOptions: JsonSerializerOptions)
    (httpClient: HttpClient)
    (ct: CancellationToken)
    (table: string)
    (id: string)
    : Task<RestApiResult<JsonNode>> =
    keyTableId table id
    |> executeEmptyRequest<JsonNode> jsonOptions httpClient ct HttpMethod.Delete

module Typed =
    /// <summary>
    /// This HTTP RESTful endpoint selects all records in a specific table in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#select-all"/>
    /// <typeparam name="record">The type of the records to return.</typeparam>
    /// <param name="jsonOptions">The JSON options to use.</param>
    /// <param name="httpClient">The HTTP client to use.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="table">The table to select from.</param>
    /// <returns>The records as a JSON Node.</returns>
    /// <remarks>
    /// Assumes the HTTP client has the correct headers already set.
    /// </remarks>
    let getKeyTable<'record>
        (jsonOptions: JsonSerializerOptions)
        (httpClient: HttpClient)
        (ct: CancellationToken)
        (table: string)
        : Task<RequestResult<'record []>> =
        task {
            let! response =
                keyTable table
                |> executeEmptyRequest<'record []> jsonOptions httpClient ct HttpMethod.Get

            let response = asSingleApiResult response

            return ApiSingleResult.tryGetMultipleRecords response.result
        }

    /// <summary>
    /// This HTTP RESTful endpoint creates a record in a specific table in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#create-all"/>
    /// <typeparam name="recordData">The type of the input data.</typeparam>
    /// <typeparam name="record">The type of the records to return.</typeparam>
    /// <param name="jsonOptions">The JSON options to use.</param>
    /// <param name="httpClient">The HTTP client to use.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="table">The table to create the record in.</param>
    /// <param name="record">The record to create as a JSON Node.</param>
    /// <returns>The created record as a JSON Node.</returns>
    /// <remarks>
    /// Assumes the HTTP client has the correct headers already set.
    /// </remarks>
    let postKeyTableData<'recordData, 'record>
        (jsonOptions: JsonSerializerOptions)
        (httpClient: HttpClient)
        (ct: CancellationToken)
        (table: string)
        (record: 'recordData)
        : Task<RequestResult<'record>> =
        task {
            let! response =
                executeRecordRequest<'recordData, 'record []>
                    jsonOptions
                    httpClient
                    ct
                    HttpMethod.Post
                    (keyTable table)
                    record

            let response = asSingleApiResult response

            return ApiSingleResult.tryGetRequiredRecord response.result
        }

    /// <summary>
    /// This HTTP RESTful endpoint creates a record in a specific table in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#create-all"/>
    /// <typeparam name="recordData">The type of the input data.</typeparam>
    /// <typeparam name="record">The type of the records to return.</typeparam>
    /// <param name="jsonOptions">The JSON options to use.</param>
    /// <param name="httpClient">The HTTP client to use.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="table">The table to create the record in.</param>
    /// <param name="record">The record to create as a JSON Node.</param>
    /// <returns>The created record as a JSON Node.</returns>
    /// <remarks>
    /// Assumes the HTTP client has the correct headers already set.
    /// </remarks>
    let postKeyTable<'record>
        (jsonOptions: JsonSerializerOptions)
        (httpClient: HttpClient)
        (ct: CancellationToken)
        (table: string)
        (record: 'record)
        : Task<RequestResult<'record>> =
        postKeyTableData<'record, 'record> jsonOptions httpClient ct table record

    /// <summary>
    /// This HTTP RESTful endpoint deletes all records from the specified table in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#delete-all"/>
    /// <param name="jsonOptions">The JSON options to use.</param>
    /// <param name="httpClient">The HTTP client to use.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="table">The table to delete all records from.</param>
    /// <returns>An empty array as a JSON Node.</returns>
    /// <remarks>
    /// Assumes the HTTP client has the correct headers already set.
    /// </remarks>
    let deleteKeyTable
        (jsonOptions: JsonSerializerOptions)
        (httpClient: HttpClient)
        (ct: CancellationToken)
        (table: string)
        : Task<RequestResult<unit>> =
        task {
            let! response =
                keyTable table
                |> executeEmptyRequest<unit []> jsonOptions httpClient ct HttpMethod.Delete

            let response = asSingleApiResult response

            return ApiSingleResult.tryGetNoRecords response.result
        }

    /// <summary>
    /// This HTTP RESTful endpoint selects a specific record from the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#select-one"/>
    /// <param name="jsonOptions">The JSON options to use.</param>
    /// <param name="httpClient">The HTTP client to use.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="table">The table to select the record from.</param>
    /// <param name="id">The id of the record to select.</param>
    /// <returns>An array with the record (or empty) as a JSON Node.</returns>
    /// <remarks>
    /// Assumes the HTTP client has the correct headers already set.
    /// </remarks>
    let getKeyTableId<'record>
        (jsonOptions: JsonSerializerOptions)
        (httpClient: HttpClient)
        (ct: CancellationToken)
        (table: string)
        (id: string)
        : Task<RequestResult<'record voption>> =
        task {
            let! response =
                keyTableId table id
                |> executeEmptyRequest<'record []> jsonOptions httpClient ct HttpMethod.Get

            let response = asSingleApiResult response

            return ApiSingleResult.tryGetOptionalRecord response.result
        }

    /// <summary>
    /// This HTTP RESTful endpoint creates a single specific record into the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#create-one"/>
    /// <param name="jsonOptions">The JSON options to use.</param>
    /// <param name="httpClient">The HTTP client to use.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="table">The table to create the record in.</param>
    /// <param name="id">The id of the record to create.</param>
    /// <param name="record">The record to create as a JSON Node.</param>
    /// <returns>The created record as a JSON Node.</returns>
    /// <remarks>
    /// Assumes the HTTP client has the correct headers already set.
    /// </remarks>
    let postKeyTableIdData<'recordData, 'record>
        (jsonOptions: JsonSerializerOptions)
        (httpClient: HttpClient)
        (ct: CancellationToken)
        (table: string)
        (id: string)
        (record: 'recordData)
        : Task<RequestResult<'record>> =
        task {
            let! response =
                executeRecordRequest<'recordData, 'record []>
                    jsonOptions
                    httpClient
                    ct
                    HttpMethod.Post
                    (keyTableId table id)
                    record

            let response = asSingleApiResult response

            return ApiSingleResult.tryGetRequiredRecord response.result
        }

    /// <summary>
    /// This HTTP RESTful endpoint creates a single specific record into the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#create-one"/>
    /// <param name="jsonOptions">The JSON options to use.</param>
    /// <param name="httpClient">The HTTP client to use.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="table">The table to create the record in.</param>
    /// <param name="id">The id of the record to create.</param>
    /// <param name="record">The record to create as a JSON Node.</param>
    /// <returns>The created record as a JSON Node.</returns>
    /// <remarks>
    /// Assumes the HTTP client has the correct headers already set.
    /// </remarks>
    let postKeyTableId<'record>
        (jsonOptions: JsonSerializerOptions)
        (httpClient: HttpClient)
        (ct: CancellationToken)
        (table: string)
        (id: string)
        (record: 'record)
        : Task<RequestResult<'record>> =
        postKeyTableIdData<'record, 'record> jsonOptions httpClient ct table id record

    /// <summary>
    /// This HTTP RESTful endpoint creates or updates a single specific record in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#update-one"/>
    /// <param name="jsonOptions">The JSON options to use.</param>
    /// <param name="httpClient">The HTTP client to use.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="table">The table to create or update the record in.</param>
    /// <param name="id">The id of the record to create or update.</param>
    /// <param name="record">The record to create or update as a JSON Node.</param>
    /// <returns>The created or updated record as a JSON Node.</returns>
    /// <remarks>
    /// Assumes the HTTP client has the correct headers already set.
    /// </remarks>
    let putKeyTableIdData<'recordData, 'record>
        (jsonOptions: JsonSerializerOptions)
        (httpClient: HttpClient)
        (ct: CancellationToken)
        (table: string)
        (id: string)
        (record: 'recordData)
        : Task<RequestResult<'record>> =
        task {
            let! response =
                executeRecordRequest<'recordData, 'record []>
                    jsonOptions
                    httpClient
                    ct
                    HttpMethod.Put
                    (keyTableId table id)
                    record

            let response = asSingleApiResult response

            return ApiSingleResult.tryGetRequiredRecord response.result
        }

    /// <summary>
    /// This HTTP RESTful endpoint creates or updates a single specific record in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#update-one"/>
    /// <param name="jsonOptions">The JSON options to use.</param>
    /// <param name="httpClient">The HTTP client to use.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="table">The table to create or update the record in.</param>
    /// <param name="id">The id of the record to create or update.</param>
    /// <param name="record">The record to create or update as a JSON Node.</param>
    /// <returns>The created or updated record as a JSON Node.</returns>
    /// <remarks>
    /// Assumes the HTTP client has the correct headers already set.
    /// </remarks>
    let putKeyTableId<'record>
        (jsonOptions: JsonSerializerOptions)
        (httpClient: HttpClient)
        (ct: CancellationToken)
        (table: string)
        (id: string)
        (record: 'record)
        : Task<RequestResult<'record>> =
        putKeyTableIdData<'record, 'record> jsonOptions httpClient ct table id record

    /// <summary>
    /// This HTTP RESTful endpoint creates or updates a single specific record in the database.
    /// If the record already exists, then only the specified fields will be updated.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#modify-one"/>
    /// <param name="jsonOptions">The JSON options to use.</param>
    /// <param name="httpClient">The HTTP client to use.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="table">The table to update the record in.</param>
    /// <param name="id">The id of the record to update.</param>
    /// <param name="record">The partial record to update as a JSON Node.</param>
    /// <returns>The updated record as a JSON Node.</returns>
    /// <remarks>
    /// Assumes the HTTP client has the correct headers already set.
    /// </remarks>
    let patchKeyTableIdData<'recordData, 'record>
        (jsonOptions: JsonSerializerOptions)
        (httpClient: HttpClient)
        (ct: CancellationToken)
        (table: string)
        (id: string)
        (record: 'recordData)
        : Task<RequestResult<'record>> =
        task {
            let! response =
                executeRecordRequest<'recordData, 'record []>
                    jsonOptions
                    httpClient
                    ct
                    HttpMethod.Patch
                    (keyTableId table id)
                    record

            let response = asSingleApiResult response

            return ApiSingleResult.tryGetRequiredRecord response.result
        }

    /// <summary>
    /// This HTTP RESTful endpoint deletes a single specific record from the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#delete-one"/>
    /// <param name="jsonOptions">The JSON options to use.</param>
    /// <param name="httpClient">The HTTP client to use.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="table">The table to delete the record from.</param>
    /// <param name="id">The id of the record to delete.</param>
    /// <returns>An empty array as a JSON Node.</returns>
    /// <remarks>
    /// Assumes the HTTP client has the correct headers already set.
    /// </remarks>
    let deleteKeyTableId
        (jsonOptions: JsonSerializerOptions)
        (httpClient: HttpClient)
        (ct: CancellationToken)
        (table: string)
        (id: string)
        : Task<RequestResult<unit>> =
        task {
            let! response =
                keyTableId table id
                |> executeEmptyRequest<unit []> jsonOptions httpClient ct HttpMethod.Delete

            let response = asSingleApiResult response

            return ApiSingleResult.tryGetNoRecords response.result
        }
