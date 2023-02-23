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
      response: RequestResult<'result> }

    member this.timeSpan = TimeSpan.tryParse this.time

type Statement = Statement<JsonNode>

module Statement =
    let private parseJson<'a> options (json: JsonNode) =
        json.Deserialize<'a>(options: JsonSerializerOptions)

    let private tryGetJsonArray (json: JsonNode) =
        match json with
        | :? JsonArray as arr -> Ok arr
        | _ -> Error(ProtocolError ExpectedArray)

    let internal tryGetSingle (items: IList<'a>) =
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

    let private toStatement statement response =
        { response = response; status = statement.status; time = statement.time }

    let getRequiredRecordOf<'record> (statement: Statement<'record []>) : Statement<'record> =
        statement.response
        |> Result.bind tryGetSingle
        |> toStatement statement

    let getRequiredRecord<'record> (options: JsonSerializerOptions) (statement: Statement) : Statement<'record> =
        statement.response
        |> Result.bind tryGetJsonArray
        |> Result.bind tryGetSingle
        |> Result.map (parseJson<'record> options)
        |> toStatement statement

    let getOptionalRecordOf<'record> (statement: Statement<'record []>) : Statement<'record voption> =
        statement.response
        |> Result.bind tryGetOptional
        |> toStatement statement

    let getOptionalRecord<'record>
        (options: JsonSerializerOptions)
        (statement: Statement)
        : Statement<'record voption> =
        statement.response
        |> Result.bind tryGetJsonArray
        |> Result.bind tryGetOptional
        |> Result.map (ValueOption.map (parseJson<'record> options))
        |> toStatement statement

    let getNoRecordsOf (statement: Statement<_ array>) : Statement<unit> =
        statement.response
        |> Result.bind tryGetEmpty
        |> toStatement statement

    let getNoRecords (statement: Statement) : Statement<unit> =
        statement.response
        |> Result.bind tryGetJsonArray
        |> Result.bind tryGetEmpty
        |> toStatement statement

    let getMultipleRecordsOf<'record> (statement: Statement<'record []>) : Statement<'record []> =
        statement

    let getMultipleRecords<'record>
        (options: JsonSerializerOptions)
        (statement: Statement)
        : Statement<'record []> =
        statement.response
        |> Result.bind tryGetJsonArray
        |> Result.map (parseJson<'record []> options)
        |> toStatement statement

type StatementsResult<'result> = RequestResult<Statement<'result> []>
type StatementResult<'result> = RequestResult<Statement<'result>>

module StatementResult =
    let ofStatementsResult<'result> (response: StatementsResult<'result>) : StatementResult<'result> =
        response
        |> Result.bind Statement.tryGetSingle

    let toSimpleResult (response: StatementResult<'result>) : RequestResult<'result> =
        response
        |> Result.bind (fun statement -> statement.response)
