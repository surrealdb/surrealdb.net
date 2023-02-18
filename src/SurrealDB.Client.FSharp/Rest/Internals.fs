namespace SurrealDB.Client.FSharp.Rest

open System
open System.Net.Http
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open System.Threading

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
                              response = Error (StatementError detail) }
                        | None, None ->
                            { time = json.time
                              status = json.status
                              response = Error (StatementError EXPECTED_RESULT_OR_DETAIL) })
                    |> Ok
                else
                    JsonSerializer.Deserialize<ErrorInfo>(content, jsonOptions)
                    |> ResponseError
                    |> Error

            let response: RestApiResult<'result> = { headers = headers; statements = result }

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

    let executeTextRequest<'result> jsonOptions httpClient ct method url text =
        task {
            let url = Uri(url, UriKind.Relative)
            let request = new HttpRequestMessage(method, url)
            request.Content <- new StringContent(text, Encoding.UTF8, TEXT_PLAIN)
            return! executeRequest<'result> jsonOptions httpClient ct request
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
    let applyConfig (config: SurrealConfig) (httpClient: HttpClient) =
        updateDefaultHeader ACCEPT_HEADER APPLICATION_JSON httpClient

        match config.Credentials with
        | ValueSome credentials -> applyCredentialHeaders credentials httpClient
        | ValueNone -> ()

        updateDefaultHeader NS_HEADER config.Namespace httpClient
        updateDefaultHeader DB_HEADER config.Database httpClient

        httpClient.BaseAddress <- Uri(config.BaseUrl, UriKind.Absolute)
