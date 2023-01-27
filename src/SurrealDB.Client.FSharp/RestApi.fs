module internal SurrealDB.Client.FSharp.RestApi

open System.Net.Http
open System.Text.Json.Nodes

[<AutoOpen>]
module Constants =
    [<Literal>]
    let VERSION_HEADER = "version"

    [<Literal>]
    let SERVER_HEADER = "server"

    [<Literal>]
    let DATE_HEADER = "date"

    [<Literal>]
    let AUTHORIZATION_HEADER = "AUTHORIZATION"

    [<Literal>]
    let BASIC_SCHEME = "Basic"

    [<Literal>]
    let BEARER_SCHEME = "Bearer"

    [<Literal>]
    let NS_HEADER = "NS"

    [<Literal>]
    let DB_HEADER = "DB"

/// <summary>
/// Converts a credential to a list of headers.
/// </summary>
let credentialsAsHeaders credentials =
    seq {
        match credentials with
        | Basic (user, password) ->
            let auth =
                String.toBase64 <| sprintf "%s:%s" user password

            yield AUTHORIZATION_HEADER, sprintf "%s %s" BASIC_SCHEME auth
        | Bearer jwt -> yield AUTHORIZATION_HEADER, sprintf "%s %s" BEARER_SCHEME jwt
    }

/// <summary>
/// Converts a configuration to a list of headers.
/// </summary>
let configAsHeaders (config: SurrealConfig) =
    seq {
        match config.credentials with
        | ValueSome credentials -> yield! credentialsAsHeaders credentials
        | ValueNone -> ()

        match config.ns with
        | ValueSome ns -> yield NS_HEADER, ns
        | ValueNone -> ()

        match config.db with
        | ValueSome db -> yield DB_HEADER, db
        | ValueNone -> ()
    }

/// <summary>
/// Apply a list of headers to a <see cref="HttpClient"/>'s default headers.
/// Headers with the same name will be overwritten.
/// </summary>
/// <param name="headers">The headers to apply.</param>
/// <param name="httpClient">The <see cref="HttpClient"/> to apply the headers to.</param>
let applyDefaultHeaders headers (httpClient: HttpClient) =
    for (key, value) in headers do
        httpClient.DefaultRequestHeaders.Remove(key)
        |> ignore

        httpClient.DefaultRequestHeaders.Add(key, (value: string))

type ErrorDetailsJson =
    { details: string
      code: int
      description: string
      information: string }

type ResponseDetailsJson =
    { time: string
      status: string
      result: JsonNode }

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

let readResponseHeaderInfo (response: HttpResponseMessage) : ResponseInfo =
    let dateString = readHeaderStr DATE_HEADER "" response

    let date =
        lazy (DateTimeOffset.tryParse dateString)

    { ResponseInfo.empty with
        dateString = dateString
        date = date }

let readSuccessResponse ct (response: HttpResponseMessage) =
    task {
        let responseInfo = readResponseHeaderInfo response
        let! json = response.Content.ReadAsStringAsync ct

        let infoJson =
            Json.deserialize<ResponseDetailsJson> json

        let timeString = String.orEmpty infoJson.time
        let time = lazy (TimeSpan.tryParse timeString)
        let status = String.orEmpty infoJson.status
        let server = readServerInfo response

        return
            infoJson.result,
            { responseInfo with
                timeString = timeString
                time = time
                status = status
                server = server }
    }

// let readErrorResponse ct (response: HttpResponseMessage) =
//     task {
//         let responseInfo = readResponseHeaderInfo response
//         let! json = response.Content.ReadAsStringAsync ct
//         let infoJson = Json.deserialize<ErrorDetailsJson> json
//         let status = "ERROR"
//         let server = readServerInfo response

//         let errorDetails =
//             { ErrorDetails.empty with
//                 code = infoJson.code
//                 description = String.orEmpty infoJson.description
//                 information = String.orEmpty infoJson.information
//                 details = String.orEmpty infoJson.details }

//         return
//             infoJson.result,
//             { responseInfo with
//                 timeString = timeString
//                 time = time
//                 status = status
//                 server = server }
//     }
