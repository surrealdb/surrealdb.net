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
      statements: StatementsResult<'result> }

type RestApiSingleResult<'result> =
    { headers: HeadersInfo
      statement: StatementResult<'result> }

module RestApiSingleResult =
    let private toResult result statement =
        { headers = result.headers
          statement = statement }

    let ofRestApiResult<'result> (response: RestApiResult<'result>) : RestApiSingleResult<'result> =
        response.statements
        |> StatementResult.ofStatementsResult
        |> fun statement -> { headers = response.headers; statement = statement }

    let getRequiredRecord<'record> (response: RestApiSingleResult<'record []>) : RestApiSingleResult<'record> =
        response.statement
        |> Result.map Statement.getRequiredRecordOf
        |> toResult response

    let getMultipleRecords<'record> (response: RestApiSingleResult<'record []>) : RestApiSingleResult<'record []> =
        response

    let getOptionalRecord<'record>
        (response: RestApiSingleResult<'record []>)
        : RestApiSingleResult<'record voption> =
        response.statement
        |> Result.map Statement.getOptionalRecordOf<'record>
        |> toResult response

    let getNoRecords (response: RestApiSingleResult<'record []>) : RestApiSingleResult<unit> =
        response.statement
        |> Result.map Statement.getNoRecordsOf
        |> toResult response
