module internal SurrealDB.Client.FSharp.RestApi

open System
open System.Globalization
open System.Net.Http
open System.Text.Json

[<AutoOpen>]
module Constants =
    [<Literal>]
    let VERSION_HEADER = "version"

    [<Literal>]
    let SERVER_HEADER = "server"

    [<Literal>]
    let DATE_HEADER = "date"

let applyDefaultHeaders headers (httpClient: HttpClient) =
    for (key, value) in headers do
        httpClient.DefaultRequestHeaders.Remove(key)
        |> ignore

        httpClient.DefaultRequestHeaders.Add(key, (value: string))

let jsonSerializerOptions =
    let o = JsonSerializerOptions()
    // o.PropertyNamingPolicy <- JsonNamingPolicy.CamelCase
    o

type ErrorDetailsJson =
    { details: string
      code: int
      description: string
      information: string }

let stringOrEmpty s = if isNull s then "" else s

let readHeaderStrOpt (name: string) (response: HttpResponseMessage) =
    match response.Headers.TryGetValues(name) with
    | false, _ -> ValueNone
    | true, values -> values |> Seq.tryHeadValue

let readServerInfo (response: HttpResponseMessage) : ServerInfo =
    let version = readHeaderStrOpt VERSION_HEADER response
    let name = readHeaderStrOpt SERVER_HEADER response

    { version = version; name = name }

let readResponseHeaderInfo (response: HttpResponseMessage) : ResponseInfo =
    let dateString = readHeaderStrOpt DATE_HEADER response

    let date =
        lazy
            (dateString
             |> ValueOption.bind (fun s ->
                 match DateTimeOffset.TryParse s with
                 | true, dto -> ValueSome dto
                 | false, _ -> ValueNone))

    { timeString = ValueNone
      time = lazy (ValueNone)
      dateString = dateString
      date = date }

// let parseErrorDetails (response: HttpResponseMessage) =
//     task {  }
//     JsonSerializer.Deserialize<ErrorDetailsJson>(json, jsonSerializerOptions)


// let toSurrealErrorDetails (json: ErrorDetailsJson) =
//     { SurrealErrorDetails.details = stringOrEmpty json.details
//       code = json.code
//       description = stringOrEmpty json.description
//       information = stringOrEmpty json.information }
