[<AutoOpen>]
module internal SurrealDB.Client.FSharp.Rest.Utilities

open System
open System.Net.Http
open System.Text
open System.Text.Json.Nodes
open System.Threading

open SurrealDB.Client.FSharp

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

let executeRequest (request: HttpRequestMessage) (ct: CancellationToken) (httpClient: HttpClient) =
    task {
        let! response = httpClient.SendAsync(request, ct)
        let! result = RestApiResult.parse<JsonNode> ct response
        return result
    }

let executeEmptyRequest method url ct httpClient =
    task {
        let url = Uri(url, UriKind.Relative)
        let request = new HttpRequestMessage(method, url)
        return! executeRequest request ct httpClient
    }

let executeJsonRequest method url json ct httpClient =
    task {
        let url = Uri(url, UriKind.Relative)
        let request = new HttpRequestMessage(method, url)
        let json = Json.serialize<JsonNode> json
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

let parseOneItem<'record> (response: RestApiResult<JsonNode>) =
    match response.result with
    | Error err -> Error(ResponseError err)
    | Ok arr ->
        match arr.Length with
        | 1 ->
            match arr.[0] with
            | Error err -> Error(ItemError err)
            | Ok success -> Ok(Json.deserializeNode<'record> success.result)
        | _ -> Error(UnexpectedError "Expected exactly one item in response")

let parseNoItems<'record> (response: RestApiResult<JsonNode>) =
    match response.result with
    | Error err -> Error(ResponseError err)
    | Ok arr ->
        match arr.Length with
        | 0 -> Ok()
        | _ -> Error(UnexpectedError "Expected no items in response")

let parseManyItems<'record> (response: RestApiResult<JsonNode>) =
    match response.result with
    | Error err -> Error(ResponseError err)
    | Ok arr ->
        let items = ResizeArray(arr.Length)
        let rec loop index =
            if index >= arr.Length then
                Ok(items.ToArray())
            else
                match arr.[index] with
                | Error err -> Error(ItemError err)
                | Ok success ->
                    let item = Json.deserializeNode<'record> success.result
                    items.Add(item)
                    loop (index + 1)
        loop 0
