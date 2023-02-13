namespace SurrealDB.Client.FSharp

open System.Text.Json
open System.Text.Json.Nodes

open SurrealDB.Client.FSharp

type ErrorInfo =
    { details: string
      code: int
      description: string
      information: string }

type RequestError =
    | ResponseError of ErrorInfo
    | StatementError of string
    | ProtocolError of string

type Statement =
    { time: string
      status: string
      response: Result<JsonNode, string> }

    member this.timeSpan = TimeSpan.tryParse this.time

module Statement =
    [<Literal>]
    let EXPECTED_ARRAY = "EXPECTED_ARRAY"

    [<Literal>]
    let EXPECTED_EMPTY_ARRAY = "EXPECTED_EMPTY_ARRAY"

    [<Literal>]
    let EXPECTED_SINGLE_ITEM = "EXPECTED_SINGLE_ITEM"

    [<Literal>]
    let EXPECTED_OPTIONAL_ITEM = "EXPECTED_OPTIONAL_ITEM"

    let private parseJson<'a> options (json: JsonNode) =
        json.Deserialize<'a>(options: JsonSerializerOptions)

    let private tryGetJsonArray (json: JsonNode) =
        match json with
        | :? JsonArray as arr -> Ok arr
        | _ -> Error (ProtocolError EXPECTED_ARRAY)
        
    let private tryGetSingle (items: JsonArray) =
        match items.Count with
        | 1 -> Ok items.[0]
        | _ -> Error (ProtocolError EXPECTED_SINGLE_ITEM)

    let private tryGetOptional (items: JsonArray) =
        match items.Count with
        | 0 -> Ok ValueNone
        | 1 -> Ok (ValueSome items.[0])
        | _ -> Error (ProtocolError EXPECTED_OPTIONAL_ITEM)
        
    let private tryGetEmpty (items: JsonArray) =
        match items.Count with
        | 0 -> Ok ()
        | _ -> Error (ProtocolError EXPECTED_EMPTY_ARRAY)

    let tryGetRequiredRecord<'record> options statement =
        statement.response
        |> Result.mapError StatementError
        |> Result.bind tryGetJsonArray
        |> Result.bind tryGetSingle
        |> Result.map (parseJson<'record> options)

    let tryGetOptionalRecord<'record> options statement =
        statement.response
        |> Result.mapError StatementError
        |> Result.bind tryGetJsonArray
        |> Result.bind tryGetOptional
        |> Result.map (ValueOption.map (parseJson<'record> options))

    let tryGetNoRecords statement =
        statement.response
        |> Result.mapError StatementError
        |> Result.bind tryGetJsonArray
        |> Result.bind tryGetEmpty

    let tryGetMultipleRecords<'record> options statement =
        statement.response
        |> Result.mapError StatementError
        |> Result.bind tryGetJsonArray
        |> Result.map (parseJson<'record[]> options)

type ApiResult = Result<Statement [], ErrorInfo>

module ApiResult =
    [<Literal>]
    let EXPECTED_SINGLE_STATEMENT = "EXPECTED_SINGLE_STATEMENT"

    let tryGetSingleStatement (response: ApiResult) =
        match response with
        | Error err -> Error(ResponseError err )
        | Ok statements when statements.Length = 1 ->
            Ok statements.[0]
        | _ ->
            Error(ProtocolError EXPECTED_SINGLE_STATEMENT)

    let tryGetRequiredRecord<'record> options response =
        tryGetSingleStatement response
        |> Result.bind (Statement.tryGetRequiredRecord<'record> options)

    let tryGetMultipleRecords<'record> options response =
        tryGetSingleStatement response
        |> Result.bind (Statement.tryGetMultipleRecords<'record> options)

    let tryGetOptionalRecord<'record> options response =
        tryGetSingleStatement response
        |> Result.bind (Statement.tryGetOptionalRecord<'record> options)

    let tryGetNoRecords response =
        tryGetSingleStatement response
        |> Result.bind Statement.tryGetNoRecords
