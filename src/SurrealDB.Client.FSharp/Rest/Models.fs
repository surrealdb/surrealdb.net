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

type RestApiResult<'result> =
    { headers: HeadersInfo
      result: ApiResult<'result> }
type RestApiResult = RestApiResult<JsonNode>

type RestApiSingleResult<'result> =
    { headers: HeadersInfo
      result: ApiSingleResult<'result> }
