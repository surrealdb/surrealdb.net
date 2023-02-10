namespace SurrealDB.Client.FSharp.Rest

open System.Net
open System.Net.Http
open System.Text.Json
open System.Text.Json.Nodes
open System.Text.Json.Serialization
open System.Threading

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

type StatementErrorInfo =
    { time: string
      status: string
      detail: string }

type RequestError =
    | ResponseError of ErrorInfo
    | StatementError of StatementErrorInfo
    | UnexpectedError of string
