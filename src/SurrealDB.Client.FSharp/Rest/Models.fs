namespace SurrealDB.Client.FSharp.Rest

open System.Text.Json.Nodes
open System.Net
open System.Net.Http
open System.Threading

open SurrealDB.Client.FSharp

[<Struct>]
type HeadersInfo =
    { version: string
      server: string
      status: HttpStatusCode
      date: string }

    member this.dateTimeOffset = DateTimeOffset.tryParse this.date

    member this.dateTime = DateTime.tryParse this.date

[<RequireQualifiedAccess>]
module HeadersInfo =
    let empty =
        { version = ""
          server = ""
          status = enum 0
          date = "" }

    let parse (response: HttpResponseMessage) =
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

[<Struct>]
type ItemSuccessInfo<'result> =
    { status: string
      result: 'result
      time: string }

    member this.timeSpan = TimeSpan.tryParse this.time

[<RequireQualifiedAccess>]
module ItemSuccessInfo =
    let empty () =
        { status = ""
          result = Unchecked.defaultof<'result>
          time = "" }

    let mapResult f info =
        { status = info.status
          time = info.time
          result = f info.result }

[<Struct>]
type ItemErrorInfo =
    { status: string
      detail: string
      time: string }

    member this.timeSpan = TimeSpan.tryParse this.time

[<RequireQualifiedAccess>]
module ItemErrorInfo =
    let empty () = { status = ""; detail = ""; time = "" }

[<Struct>]
type ErrorInfo =
    { details: string
      code: int
      description: string
      information: string }

[<RequireQualifiedAccess>]
module ErrorInfo =
    let empty =
        { details = ""
          code = 0
          description = ""
          information = "" }

[<Struct>]
type RestApiResult<'result> =
    { headers: HeadersInfo
      result: Result<Result<ItemSuccessInfo<'result>, ItemErrorInfo> [], ErrorInfo> }

[<RequireQualifiedAccess>]
module RestApiResult =
    let empty () =
        { headers = HeadersInfo.empty
          result = Error ErrorInfo.empty }

    let mapResult f info =
        { headers = info.headers
          result =
            info.result
            |> Result.map (Array.map (Result.map (ItemSuccessInfo.mapResult f))) }

    type ItemResultJson =
        { status: string
          detail: string
          result: JsonNode
          time: string }

    let parse<'result> (cancellationToken: CancellationToken) (response: HttpResponseMessage) =
        task {
            let headers = HeadersInfo.parse response
            let! content = response.Content.ReadAsStringAsync(cancellationToken)

            let result =
                if response.IsSuccessStatusCode then
                    Json.deserialize<ItemResultJson []> content
                    |> Array.map (fun item ->
                        if item.status = STATUS_OK then
                            Ok
                                { status = item.status
                                  result = item.result
                                  time = item.time }
                        else
                            Error
                                { status = item.status
                                  detail = item.detail
                                  time = item.time })
                    |> Ok
                else
                    Json.deserialize<ErrorInfo> content |> Error

            return { headers = headers; result = result }
        }

type RequestError =
    | ResponseError of ErrorInfo
    | ItemError of ItemErrorInfo
    | UnexpectedError of string
