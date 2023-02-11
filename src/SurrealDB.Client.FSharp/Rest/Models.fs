namespace SurrealDB.Client.FSharp.Rest

open System.Net
open System.Text.Json.Nodes

open SurrealDB.Client.FSharp

type HeadersInfo =
    { version: string
      server: string
      status: HttpStatusCode
      date: string }

    member this.dateTimeOffset = DateTimeOffset.tryParse this.date

    member this.dateTime = DateTime.tryParse this.date

type StatementInfo =
    { time: string
      status: string
      response: Result<JsonNode, string> }

    member this.timeSpan = TimeSpan.tryParse this.time

type ErrorInfo =
    { details: string
      code: int
      description: string
      information: string }

type RestApiResult =
    { headers: HeadersInfo
      result: Result<StatementInfo [], ErrorInfo> }

type ResponseErrorInfo =
    { headers: HeadersInfo
      error: ErrorInfo }

type ProtocolErrorInfo =
    { headers: HeadersInfo
      error: string }

type StatementErrorInfo =
    { headers: HeadersInfo
      time: string
      status: string
      detail: string }

    member this.timeSpan = TimeSpan.tryParse this.time

type StatementResultInfo<'result> =
    { headers: HeadersInfo
      time: string
      status: string
      result: 'result }

    member this.timeSpan = TimeSpan.tryParse this.time


type RequestError =
    | ResponseError of ResponseErrorInfo
    | StatementError of StatementErrorInfo
    | ProtocolError of ProtocolErrorInfo
