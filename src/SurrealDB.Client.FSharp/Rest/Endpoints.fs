module SurrealDB.Client.FSharp.Rest.Endpoints

open System
open System.Net.Http
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open System.Threading

open SurrealDB.Client.FSharp

[<AutoOpen>]
module internal Internals =
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

    type StatementInfoJson =
        { status: string
          detail: string option
          result: JsonNode option
          time: string }

    let parseApiResult
        (jsonOptions: JsonSerializerOptions)
        (cancellationToken: CancellationToken)
        (response: HttpResponseMessage)
        =
        task {
            let headers = parseHeaderInfo response
            let! content = response.Content.ReadAsStringAsync(cancellationToken)

            let result =
                if response.IsSuccessStatusCode then
                    JsonSerializer.Deserialize<StatementInfoJson[]>(content, jsonOptions)
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

            return { headers = headers; result = result }
        }

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

    let executeRequest
        (jsonOptions: JsonSerializerOptions)
        (httpClient: HttpClient)
        (ct: CancellationToken)
        (request: HttpRequestMessage)
        =
        task {
            let! response = httpClient.SendAsync(request, ct)
            let! result = parseApiResult jsonOptions ct response
            return result
        }

    let executeEmptyRequest jsonOptions httpClient ct method url =
        task {
            let url = Uri(url, UriKind.Relative)
            let request = new HttpRequestMessage(method, url)
            return! executeRequest jsonOptions httpClient ct request
        }

    let executeJsonRequest jsonOptions httpClient ct method url json =
        task {
            let url = Uri(url, UriKind.Relative)
            let request = new HttpRequestMessage(method, url)

            let json =
                JsonSerializer.Serialize<JsonNode>(json, (jsonOptions: JsonSerializerOptions))

            request.Content <- new StringContent(json, Encoding.UTF8, APPLICATION_JSON)
            return! executeRequest jsonOptions httpClient ct request
        }

    let executeTextRequest jsonOptions httpClient ct method url text =
        task {
            let url = Uri(url, UriKind.Relative)
            let request = new HttpRequestMessage(method, url)
            request.Content <- new StringContent(text, Encoding.UTF8, TEXT_PLAIN)
            return! executeRequest jsonOptions httpClient ct request
        }

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
let applyConfig (config: SurrealConfig) httpClient =
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
let postSql jsonOptions httpClient ct query =
    executeTextRequest jsonOptions httpClient ct HttpMethod.Post "sql" query

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
let getKeyTable jsonOptions httpClient ct table =
    executeEmptyRequest jsonOptions httpClient ct HttpMethod.Get $"key/%s{table}"

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
let postKeyTable jsonOptions httpClient ct table record =
    executeJsonRequest jsonOptions httpClient ct HttpMethod.Post $"key/%s{table}" record

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
let deleteKeyTable jsonOptions httpClient ct table =
    executeEmptyRequest jsonOptions httpClient ct HttpMethod.Delete $"key/%s{table}"

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
let getKeyTableId jsonOptions httpClient ct table id =
    executeEmptyRequest jsonOptions httpClient ct HttpMethod.Get $"key/%s{table}/%s{id}"

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
let postKeyTableId jsonOptions httpClient ct table id record =
    executeJsonRequest jsonOptions httpClient ct HttpMethod.Post $"key/%s{table}/%s{id}" record

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
let putKeyTableId jsonOptions httpClient ct table id =
    executeJsonRequest jsonOptions httpClient ct HttpMethod.Put $"key/%s{table}/%s{id}"

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
let patchKeyTableId jsonOptions httpClient ct table id record =
    executeJsonRequest jsonOptions httpClient ct HttpMethod.Patch $"key/%s{table}/%s{id}" record

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
let deleteKeyTableId jsonOptions httpClient ct table id =
    executeEmptyRequest jsonOptions httpClient ct HttpMethod.Delete $"key/%s{table}/%s{id}"
