module SurrealDB.Client.FSharp.RestApi

open System
open System.Net
open System.Net.Http
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open System.Threading

[<AutoOpen>]
module Constants =
    [<Literal>]
    let APPLICATION_JSON = "application/json"

    [<Literal>]
    let TEXT_PLAIN = "text/plain"

    [<Literal>]
    let VERSION_HEADER = "version"

    [<Literal>]
    let SERVER_HEADER = "server"

    [<Literal>]
    let DATE_HEADER = "date"

    [<Literal>]
    let AUTHORIZATION_HEADER = "Authorization"

    [<Literal>]
    let ACCEPT_HEADER = "Accept"

    [<Literal>]
    let BASIC_SCHEME = "Basic"

    [<Literal>]
    let BEARER_SCHEME = "Bearer"

    [<Literal>]
    let NS_HEADER = "NS"

    [<Literal>]
    let DB_HEADER = "DB"

    [<Literal>]
    let STATUS_OK = "OK"

    [<Literal>]
    let STATUS_ERR = "ERR"

[<AutoOpen>]
module internal Utilities =
    type ErrorDetailsJson =
        { details: string
          code: int
          description: string
          information: string }

    type ResponseDetailsJson =
        { time: string
          status: string
          detail: string
          result: JsonNode }

    let updateDefaultHeader key value (httpClient: HttpClient) =
        httpClient.DefaultRequestHeaders.Remove(key)
        |> ignore

        httpClient.DefaultRequestHeaders.Add(key, (value: string))

    let applyCredentialHeaders credentials httpClient =
        match credentials with
        | Basic (user, password) ->
            let auth =
                String.toBase64 <| sprintf "%s:%s" user password

            let value = sprintf "%s %s" BASIC_SCHEME auth
            updateDefaultHeader AUTHORIZATION_HEADER value httpClient

        | Bearer jwt ->
            let value = sprintf "%s %s" BEARER_SCHEME jwt
            updateDefaultHeader AUTHORIZATION_HEADER value httpClient

    let readHeaderStrOpt (name: string) (response: HttpResponseMessage) =
        match response.Headers.TryGetValues(name) with
        | false, _ -> ValueNone
        | true, values -> values |> Seq.tryHeadValue

    let readHeaderStr (name: string) (defaultValue: string) (response: HttpResponseMessage) =
        readHeaderStrOpt name response
        |> ValueOption.defaultValue defaultValue

    let readServerInfo (response: HttpResponseMessage) : ServerInfo =
        let version = readHeaderStr VERSION_HEADER "" response
        let name = readHeaderStr SERVER_HEADER "" response

        { version = version; name = name }

    let readResponseHeaderInfo (response: HttpResponseMessage) =
        let dateString = readHeaderStr DATE_HEADER "" response

        let date =
            lazy (DateTimeOffset.tryParse dateString)

        { ResponseInfo.empty with
            dateString = dateString
            date = date }

    let readJsonResponse ct (response: HttpResponseMessage) =
        task {
            try
                let content = response.Content

                if isNull content then
                    return Error(ParseError NoContentResponse)
                elif content.Headers.ContentType.MediaType
                     <> APPLICATION_JSON then
                    return Error(ParseError NoJsonResponse)
                else
                    let! json = content.ReadAsStringAsync ct
                    return Ok json
            with
            | :? JsonException as exn -> return Error(ParseError NoJsonResponse)
        }


    let readSuccessResponse ct (response: HttpResponseMessage) =
        task {
            match! readJsonResponse ct response with
            | Error err -> return Error err
            | Ok json ->
                let json =
                    Json.deserialize<ResponseDetailsJson> json

                let responseInfo = readResponseHeaderInfo response
                let timeString = String.orEmpty json.time
                let time = lazy (TimeSpan.tryParse timeString)
                let status = String.orEmpty json.status
                let server = readServerInfo response

                let responseInfo =
                    { responseInfo with
                        timeString = timeString
                        time = time
                        status = status
                        server = server }

                if status = STATUS_OK then
                    return Ok(struct (json.result, responseInfo))

                elif status = STATUS_ERR then
                    return Error(StatusError json.detail)

                else
                    return Error(StatusError $"Unknown status: '{status}'")
        }

    let readErrorResponse ct (response: HttpResponseMessage) =
        task {
            match! readJsonResponse ct response with
            | Error err -> return Error err
            | Ok json ->
                let json = Json.deserialize<ErrorDetailsJson> json
                let responseInfo = readResponseHeaderInfo response
                let status = STATUS_ERR
                let server = readServerInfo response

                let responseInfo =
                    { responseInfo with
                        status = status
                        server = server }

                let errorDetails =
                    { ErrorDetails.empty with
                        code = json.code
                        description = String.orEmpty json.description
                        information = String.orEmpty json.information
                        details = String.orEmpty json.details
                        response = responseInfo }

                if response.StatusCode
                   >= HttpStatusCode.InternalServerError then
                    return Error <| ServerError errorDetails
                elif response.StatusCode >= HttpStatusCode.BadRequest then
                    return Error <| ClientError errorDetails
                else
                    return Error <| UnknownError errorDetails
        }

    let executeRequest (request: HttpRequestMessage) (ct: CancellationToken) (httpClient: HttpClient) =
        task {
            try
                let! response = httpClient.SendAsync(request, ct)

                if response.IsSuccessStatusCode then
                    return! readSuccessResponse ct response
                else
                    return! readErrorResponse ct response
            with
            | exn -> return Error(ConnectionError exn)
        }

    let executeEmptyRequest method url ct httpClient =
        task {
            let url = Uri(url, UriKind.Relative)

            let request = new HttpRequestMessage(method, url)

            return! executeRequest request ct httpClient
        }

    let executeJsonRequest method url data ct httpClient =
        task {
            let url = Uri(url, UriKind.Relative)

            let request = new HttpRequestMessage(method, url)

            let json = Json.serialize<JsonNode> data

            request.Content <- new StringContent(json, Encoding.UTF8, APPLICATION_JSON)

            return! executeRequest request ct httpClient
        }

    let executeTextRequest method url text ct httpClient =
        task {
            let url = Uri(url, UriKind.Relative)

            let request = new HttpRequestMessage(method, url)

            request.Content <- new StringContent(text, Encoding.UTF8, TEXT_PLAIN)

            return! executeRequest request ct httpClient
        }

/// <summary>
/// Updates the default headers of a given HTTP client, to math the given configuration.
let applyConfig config httpClient =
    updateDefaultHeader ACCEPT_HEADER APPLICATION_JSON httpClient

    match config.credentials with
    | ValueSome credentials -> applyCredentialHeaders credentials httpClient
    | ValueNone -> ()

    match config.ns with
    | ValueSome ns -> updateDefaultHeader NS_HEADER ns httpClient
    | ValueNone -> ()

    match config.db with
    | ValueSome db -> updateDefaultHeader DB_HEADER db httpClient
    | ValueNone -> ()

    httpClient.BaseAddress <- Uri(config.baseUrl, UriKind.Absolute)

/// <summary>
/// This module contains calls to SurrealDB REST endpoints where the results and inputs are treated as JsonNode.
///
/// This module is low level and should not be used directly. Use the higher level modules instead.
/// </summary>
/// <see href="https://surrealdb.com/docs/integration/http"/>
/// <remarks>
/// This module is low level and should not be used directly. Use the higher level modules instead.
/// </remarks>
module Json =
    /// <summary>
    /// The SQL endpoint enables advanced SurrealQL queries.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#sql"/>
    /// <param name="query">The query to execute.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="httpClient">The HTTP client to use.</param>
    /// <returns>The records as a JSON Node.</returns>
    /// <remarks>
    /// Assumes the HTTP client has the correct headers already set.
    /// </remarks>
    let postSql query ct httpClient =
        executeTextRequest HttpMethod.Post "sql" query ct httpClient

    /// <summary>
    /// This HTTP RESTful endpoint selects all records in a specific table in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#select-all"/>
    /// <param name="table">The table to create the record in.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="httpClient">The HTTP client to use.</param>
    /// <returns>The records as a JSON Node.</returns>
    /// <remarks>
    /// Assumes the HTTP client has the correct headers already set.
    /// </remarks>
    let getKeyTable table ct httpClient =
        executeEmptyRequest HttpMethod.Get $"key/%s{table}" ct httpClient

    /// <summary>
    /// This HTTP RESTful endpoint creates a record in a specific table in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#create-all"/>
    /// <param name="table">The table to create the record in.</param>
    /// <param name="record">The record to create as a JSON Node.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="httpClient">The HTTP client to use.</param>
    /// <returns>The created record as a JSON Node.</returns>
    /// <remarks>
    /// Assumes the HTTP client has the correct headers already set.
    /// </remarks>
    let postKeyTable table record ct httpClient =
        executeJsonRequest HttpMethod.Post $"key/%s{table}" record ct httpClient

    /// <summary>
    /// This HTTP RESTful endpoint deletes all records from the specified table in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#delete-all"/>
    /// <param name="table">The table to delete all records from.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="httpClient">The HTTP client to use.</param>
    /// <returns>An empty array as a JSON Node.</returns>
    /// <remarks>
    /// Assumes the HTTP client has the correct headers already set.
    /// </remarks>
    let deleteKeyTable table ct httpClient =
        executeEmptyRequest HttpMethod.Delete $"key/%s{table}" ct httpClient

    /// <summary>
    /// This HTTP RESTful endpoint selects a specific record from the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#select-one"/>
    /// <param name="table">The table to select the record from.</param>
    /// <param name="id">The id of the record to select.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="httpClient">The HTTP client to use.</param>
    /// <returns>An array with the record (or empty) as a JSON Node.</returns>
    /// <remarks>
    /// Assumes the HTTP client has the correct headers already set.
    /// </remarks>
    let getKeyTableId table id ct httpClient =
        executeEmptyRequest HttpMethod.Get $"key/%s{table}/%s{id}" ct httpClient

    /// <summary>
    /// This HTTP RESTful endpoint creates a single specific record into the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#create-one"/>
    /// <param name="table">The table to create the record in.</param>
    /// <param name="id">The id of the record to create.</param>
    /// <param name="record">The record to create as a JSON Node.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="httpClient">The HTTP client to use.</param>
    /// <returns>The created record as a JSON Node.</returns>
    /// <remarks>
    /// Assumes the HTTP client has the correct headers already set.
    /// </remarks>
    let postKeyTableId table id record ct httpClient =
        executeJsonRequest HttpMethod.Post $"key/%s{table}/%s{id}" record ct httpClient

    /// <summary>
    /// This HTTP RESTful endpoint creates or updates a single specific record in the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#update-one"/>
    /// <param name="table">The table to create or update the record in.</param>
    /// <param name="id">The id of the record to create or update.</param>
    /// <param name="record">The record to create or update as a JSON Node.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="httpClient">The HTTP client to use.</param>
    /// <returns>The created or updated record as a JSON Node.</returns>
    /// <remarks>
    /// Assumes the HTTP client has the correct headers already set.
    /// </remarks>
    let putKeyTableId table id =
        executeJsonRequest HttpMethod.Put $"key/%s{table}/%s{id}"

    /// <summary>
    /// This HTTP RESTful endpoint creates or updates a single specific record in the database.
    /// If the record already exists, then only the specified fields will be updated.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#modify-one"/>
    /// <param name="table">The table to update the record in.</param>
    /// <param name="id">The id of the record to update.</param>
    /// <param name="record">The partial record to update as a JSON Node.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="httpClient">The HTTP client to use.</param>
    /// <returns>The updated record as a JSON Node.</returns>
    /// <remarks>
    /// Assumes the HTTP client has the correct headers already set.
    /// </remarks>
    let patchKeyTableId table id record ct httpClient =
        executeJsonRequest HttpMethod.Patch $"key/%s{table}/%s{id}" record ct httpClient

    /// <summary>
    /// This HTTP RESTful endpoint deletes a single specific record from the database.
    /// </summary>
    /// <see href="https://surrealdb.com/docs/integration/http#delete-one"/>
    /// <param name="table">The table to delete the record from.</param>
    /// <param name="id">The id of the record to delete.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <param name="httpClient">The HTTP client to use.</param>
    /// <returns>An empty array as a JSON Node.</returns>
    /// <remarks>
    /// Assumes the HTTP client has the correct headers already set.
    /// </remarks>
    let deleteKeyTableId table id ct httpClient =
        executeEmptyRequest HttpMethod.Delete $"key/%s{table}/%s{id}" ct httpClient
