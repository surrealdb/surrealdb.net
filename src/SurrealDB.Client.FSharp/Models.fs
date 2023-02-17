namespace SurrealDB.Client.FSharp

open System.Collections.Generic
open System.Text.Json
open System.Text.Json.Nodes

open SurrealDB.Client.FSharp

type ErrorInfo =
    { details: string
      code: int
      description: string
      information: string }

type ProtocolError =
    | ExpectedSingleStatement
    | ExpectedArray
    | ExpectedSingleItem
    | ExpectedOptionalItem
    | ExpectedEmptyArray

type RequestError =
    | ResponseError of ErrorInfo
    | StatementError of string
    | ProtocolError of ProtocolError

type RequestResult<'result> = Result<'result, RequestError>

type Statement<'result> =
    { time: string
      status: string
      response: Result<'result, string> }

    member this.timeSpan = TimeSpan.tryParse this.time

type Statement = Statement<JsonNode>

module Statement =
    let private parseJson<'a> options (json: JsonNode) =
        json.Deserialize<'a>(options: JsonSerializerOptions)

    let private tryGetJsonArray (json: JsonNode) =
        match json with
        | :? JsonArray as arr -> Ok arr
        | _ -> Error(ProtocolError ExpectedArray)

    let private tryGetSingle (items: IList<'a>) =
        match items.Count with
        | 1 -> Ok items.[0]
        | _ -> Error(ProtocolError ExpectedSingleItem)

    let private tryGetOptional (items: IList<'a>) =
        match items.Count with
        | 0 -> Ok ValueNone
        | 1 -> Ok(ValueSome items.[0])
        | _ -> Error(ProtocolError ExpectedOptionalItem)

    let private tryGetEmpty (items: IList<'a>) =
        match items.Count with
        | 0 -> Ok()
        | _ -> Error(ProtocolError ExpectedEmptyArray)

    let tryGetRequiredRecordOf<'record> (statement: Statement<'record []>) : RequestResult<'record> =
        statement.response
        |> Result.mapError StatementError
        |> Result.bind tryGetSingle

    let tryGetRequiredRecord<'record> (options: JsonSerializerOptions) (statement: Statement) : RequestResult<'record> =
        statement.response
        |> Result.mapError StatementError
        |> Result.bind tryGetJsonArray
        |> Result.bind tryGetSingle
        |> Result.map (parseJson<'record> options)

    let tryGetOptionalRecordOf<'record> (statement: Statement<'record []>) : RequestResult<'record voption> =
        statement.response
        |> Result.mapError StatementError
        |> Result.bind tryGetOptional

    let tryGetOptionalRecord<'record>
        (options: JsonSerializerOptions)
        (statement: Statement)
        : RequestResult<'record voption> =
        statement.response
        |> Result.mapError StatementError
        |> Result.bind tryGetJsonArray
        |> Result.bind tryGetOptional
        |> Result.map (ValueOption.map (parseJson<'record> options))

    let tryGetNoRecordsOf (statement: Statement<_ array>) : RequestResult<unit> =
        statement.response
        |> Result.mapError StatementError
        |> Result.bind tryGetEmpty

    let tryGetNoRecords (statement: Statement) : RequestResult<unit> =
        statement.response
        |> Result.mapError StatementError
        |> Result.bind tryGetJsonArray
        |> Result.bind tryGetEmpty

    let tryGetMultipleRecordsOf<'record> (statement: Statement<'record []>) : RequestResult<'record []> =
        statement.response
        |> Result.mapError StatementError

    let tryGetMultipleRecords<'record>
        (options: JsonSerializerOptions)
        (statement: Statement)
        : RequestResult<'record []> =
        statement.response
        |> Result.mapError StatementError
        |> Result.bind tryGetJsonArray
        |> Result.map (parseJson<'record []> options)

type ApiResult<'result> = Result<Statement<'result> [], ErrorInfo>
type ApiResult = ApiResult<JsonNode>
type ApiSingleResult<'result> = Result<Statement<'result>, RequestError>

module ApiSingleResult =
    let tryGetRequiredRecord<'record> (response: ApiSingleResult<'record []>) : RequestResult<'record> =
        response
        |> Result.bind Statement.tryGetRequiredRecordOf<'record>

    let tryGetMultipleRecords<'record> (response: ApiSingleResult<'record []>) : RequestResult<'record []> =
        response
        |> Result.bind Statement.tryGetMultipleRecordsOf<'record>

    let tryGetOptionalRecord<'record> (response: ApiSingleResult<'record []>) : RequestResult<'record voption> =
        response
        |> Result.bind Statement.tryGetOptionalRecordOf<'record>

    let tryGetNoRecords (response: ApiSingleResult<'record []>) : RequestResult<unit> =
        response
        |> Result.bind Statement.tryGetNoRecordsOf
