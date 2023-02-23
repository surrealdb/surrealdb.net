namespace SurrealDB.Client.FSharp

open System
open System.Globalization
open System.Text.Json
open System.Text.Json.Nodes

open Xunit
open Swensen.Unquote

open SurrealDB.Client.FSharp

[<Trait(Category, UnitTest)>]
[<Trait(Area, COMMON)>]
module ModelsTests =
    type Person = { id: SurrealId; name: string }

    let john =
        { id = SurrealId.Parse "people:john"
          name = "John" }

    let jane =
        { id = SurrealId.Parse "people:jane"
          name = "Jane" }

    let johnJson =
        JsonSerializer.Serialize(john, Json.defaultOptions)

    let janeJson =
        JsonSerializer.Serialize(jane, Json.defaultOptions)

    let testStatement statement referenceStatement expectedResponse =
        test <@ statement.time = referenceStatement.time @>
        test <@ statement.status = referenceStatement.status @>
        test <@ statement.response = expectedResponse @>

    let getRequiredRecordOkTestCases () =
        seq {
            $"[{johnJson}]", Ok john
            "[]", Error(ProtocolError ExpectedSingleItem)
            $"[{johnJson}, {janeJson}]", Error(ProtocolError ExpectedSingleItem)
            "{}", Error(ProtocolError ExpectedArray)
        }
        |> Seq.map (fun (json, expected) -> [| json :> obj; expected |])

    [<Theory>]
    [<MemberData(nameof (getRequiredRecordOkTestCases))>]
    let ``Statement.getRequiredRecord with success response`` (json: string) expected =
        let statement: Statement =
            { time = "1s"
              status = "OK"
              response = Ok(JsonNode.Parse(json)) }

        let result =
            statement
            |> Statement.getRequiredRecord<Person> Json.defaultOptions

        testStatement result statement expected

    [<Fact>]
    let ``Statement.getRequiredRecord with error response`` () =
        let statement: Statement =
            { time = "1s"
              status = "ERR"
              response = Error (StatementError "Some error") }

        let expected: RequestResult<Person> = Error(StatementError "Some error")

        let result =
            statement
            |> Statement.getRequiredRecord<Person> Json.defaultOptions

        testStatement result statement expected

    let getRequiredRecordOfOkTestCases () =
        seq {
            [| john |], Ok john
            [||], Error(ProtocolError ExpectedSingleItem)
            [| john; jane |], Error(ProtocolError ExpectedSingleItem)
        }
        |> Seq.map (fun (json, expected) -> [| json :> obj; expected |])

    [<Theory>]
    [<MemberData(nameof (getRequiredRecordOfOkTestCases))>]
    let ``Statement.getRequiredRecordOf with success response`` (response: Person []) expected =
        let statement: Statement<Person []> =
            { time = "1s"
              status = "OK"
              response = Ok(response) }

        let result =
            statement
            |> Statement.getRequiredRecordOf<Person>

        testStatement result statement expected


    let getOptionalRecordOkTestCases () =
        seq {
            $"[{johnJson}]", Ok (ValueSome john)
            "[]", Ok ValueNone
            $"[{johnJson}, {janeJson}]", Error(ProtocolError ExpectedOptionalItem)
            "{}", Error(ProtocolError ExpectedArray)
        }
        |> Seq.map (fun (json, expected) -> [| json :> obj; expected |])

    [<Theory>]
    [<MemberData(nameof (getOptionalRecordOkTestCases))>]
    let ``Statement.getOptionalRecord with success response`` (json: string) expected =
        let statement: Statement =
            { time = "1s"
              status = "OK"
              response = Ok(JsonNode.Parse(json)) }

        let result =
            statement
            |> Statement.getOptionalRecord<Person> Json.defaultOptions

        testStatement result statement expected

    [<Fact>]
    let ``Statement.getOptionalRecord with error response`` () =
        let statement: Statement =
            { time = "1s"
              status = "ERR"
              response = Error (StatementError "Some error") }

        let expected: RequestResult<Person voption> = Error(StatementError "Some error")

        let result =
            statement
            |> Statement.getOptionalRecord<Person> Json.defaultOptions

        testStatement result statement expected

    let getOptionalRecordOfOkTestCases () =
        seq {
            [| john |], Ok (ValueSome john)
            [||], Ok ValueNone
            [| john; jane |], Error(ProtocolError ExpectedOptionalItem)
        }
        |> Seq.map (fun (json, expected) -> [| json :> obj; expected |])

    [<Theory>]
    [<MemberData(nameof (getOptionalRecordOfOkTestCases))>]
    let ``Statement.getOptionalRecordOf with success response`` (response: Person []) expected =
        let statement: Statement<Person []> =
            { time = "1s"
              status = "OK"
              response = Ok(response) }

        let result =
            statement
            |> Statement.getOptionalRecordOf<Person>

        testStatement result statement expected


    let getNoRecordsOkTestCases () =
        seq {
            "[]", Ok ()
            $"[{johnJson}]", Error(ProtocolError ExpectedEmptyArray)
            "{}", Error(ProtocolError ExpectedArray)
        }
        |> Seq.map (fun (json, expected) -> [| json :> obj; expected |])

    [<Theory>]
    [<MemberData(nameof (getNoRecordsOkTestCases))>]
    let ``Statement.getNoRecords with success response`` (json: string) expected =
        let statement: Statement =
            { time = "1s"
              status = "OK"
              response = Ok(JsonNode.Parse(json)) }

        let result =
            statement
            |> Statement.getNoRecords

        testStatement result statement expected

    [<Fact>]
    let ``Statement.getNoRecords with error response`` () =
        let statement: Statement =
            { time = "1s"
              status = "ERR"
              response = Error (StatementError "Some error") }

        let expected: RequestResult<unit> = Error(StatementError "Some error")

        let result =
            statement
            |> Statement.getNoRecords

        testStatement result statement expected

    let getNoRecordsOfOkTestCases () =
        seq {
            [||], Ok ()
            [| john |], Error (ProtocolError ExpectedEmptyArray)
        }
        |> Seq.map (fun (json, expected) -> [| json :> obj; expected |])

    [<Theory>]
    [<MemberData(nameof (getNoRecordsOfOkTestCases))>]
    let ``Statement.getNoRecordsOf with success response`` (response: Person []) expected =
        let statement: Statement<Person []> =
            { time = "1s"
              status = "OK"
              response = Ok(response) }

        let result =
            statement
            |> Statement.getNoRecordsOf

        testStatement result statement expected


    let getMultipleRecordsOkTestCases () =
        seq {
            $"[{johnJson}]", Ok [|john|]
            "[]", Ok [||]
            $"[{johnJson}, {janeJson}]", Ok [|john; jane|]
            "{}", Error(ProtocolError ExpectedArray)
        }
        |> Seq.map (fun (json, expected) -> [| json :> obj; expected |])

    [<Theory>]
    [<MemberData(nameof (getMultipleRecordsOkTestCases))>]
    let ``Statement.getMultipleRecords with success response`` (json: string) expected =
        let statement: Statement =
            { time = "1s"
              status = "OK"
              response = Ok(JsonNode.Parse(json)) }

        let result =
            statement
            |> Statement.getMultipleRecords<Person> Json.defaultOptions

        testStatement result statement expected

    [<Fact>]
    let ``Statement.getMultipleRecords with error response`` () =
        let statement: Statement =
            { time = "1s"
              status = "ERR"
              response = Error (StatementError "Some error") }

        let expected: RequestResult<Person[]> = Error(StatementError "Some error")

        let result =
            statement
            |> Statement.getMultipleRecords<Person> Json.defaultOptions

        testStatement result statement expected

    let getMultipleRecordsOfOkTestCases () =
        seq {
            [| john |], (Ok [|john|] : RequestResult<Person[]>)
            [||], Ok [||]
            [| john; jane |], Ok [|john; jane|]
        }
        |> Seq.map (fun (json, expected) -> [| json :> obj; expected |])

    [<Theory>]
    [<MemberData(nameof (getMultipleRecordsOfOkTestCases))>]
    let ``Statement.getMultipleRecordsOf with success response`` (response: Person []) expected =
        let statement: Statement<Person []> =
            { time = "1s"
              status = "OK"
              response = Ok(response) }

        let result =
            statement
            |> Statement.getMultipleRecordsOf<Person>

        testStatement result statement expected
