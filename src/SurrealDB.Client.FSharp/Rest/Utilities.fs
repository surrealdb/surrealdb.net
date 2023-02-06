[<AutoOpen>]
module internal SurrealDB.Client.FSharp.Rest.Utilities

open System
open System.Net.Http
open System.Text
open System.Text.Json
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

let executeRequest (jsonOptions: JsonSerializerOptions) (request: HttpRequestMessage) (ct: CancellationToken) (httpClient: HttpClient) =
    task {
        let! response = httpClient.SendAsync(request, ct)
        let! result = RestApiResult.parse jsonOptions ct response
        return result
    }

let executeEmptyRequest jsonOptions method url ct httpClient =
    task {
        let url = Uri(url, UriKind.Relative)
        let request = new HttpRequestMessage(method, url)
        return! executeRequest jsonOptions request ct httpClient
    }

let executeJsonRequest jsonOptions method url json ct httpClient =
    task {
        let url = Uri(url, UriKind.Relative)
        let request = new HttpRequestMessage(method, url)
        let json = Json.serialize<JsonNode> jsonOptions json
        request.Content <- new StringContent(json, Encoding.UTF8, APPLICATION_JSON)
        return! executeRequest jsonOptions request ct httpClient
    }

let executeTextRequest jsonOptions method url text ct httpClient =
    task {
        let url = Uri(url, UriKind.Relative)
        let request = new HttpRequestMessage(method, url)
        request.Content <- new StringContent(text, Encoding.UTF8, TEXT_PLAIN)
        return! executeRequest jsonOptions request ct httpClient
    }

let parseFirstResponse (response: RestApiResult<JsonNode>) =
    match response.result with
    | Error err -> Error(ResponseError err)
    | Ok arr when arr.Length = 1 ->
        match arr.[0] with
        | Error err -> Error(ItemError err)
        | Ok nodes -> nodes.result.AsArray() |> Seq.toArray |> Ok
    | _ -> Error(UnexpectedError "EXPECTED_ONE_RESPONSE")

let parseManyItems<'record> jsonOptions response =
    match parseFirstResponse response with
    | Error err -> Error err
    | Ok nodes ->
        nodes
        |> Array.map (Json.deserializeNode<'record> jsonOptions)
        |> Ok

let parseOneItem<'record> jsonOptions (response: RestApiResult<JsonNode>) =
    match parseFirstResponse response with
    | Error err -> Error err
    | Ok nodes when nodes.Length = 1 ->
        nodes.[0]
        |> Json.deserializeNode<'record> jsonOptions
        |> Ok
    | _ -> Error(UnexpectedError "EXPECTED_SINGLE_RESPONSE")

let parseNoItems<'record> (response: RestApiResult<JsonNode>) =
    match response.result with
    | Error err -> Error(ResponseError err)
    | Ok arr when arr.Length = 1 -> Ok()
    | _ -> Error(UnexpectedError "EXPECTED_NO_RESPONSE")
