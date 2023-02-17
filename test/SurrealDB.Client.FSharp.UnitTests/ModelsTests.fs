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


    let tryGetRequiredRecordOkTestCases () =
        seq {
            $"[{johnJson}]", Ok john
            "[]", Error(ProtocolError ExpectedSingleItem)
            $"[{johnJson}, {janeJson}]", Error(ProtocolError ExpectedSingleItem)
            "{}", Error(ProtocolError ExpectedArray)
        }
        |> Seq.map (fun (json, expected) -> [| json :> obj; expected |])

    [<Theory>]
    [<MemberData(nameof (tryGetRequiredRecordOkTestCases))>]
    let ``Statement.tryGetRequiredRecord with success response`` (json: string) expected =
        let statement: Statement =
            { time = "1s"
              status = "OK"
              response = Ok(JsonNode.Parse(json)) }

        let result =
            statement
            |> Statement.tryGetRequiredRecord<Person> Json.defaultOptions

        test <@ result = expected @>

    [<Fact>]
    let ``Statement.tryGetRequiredRecord with error response`` () =
        let statement: Statement =
            { time = "1s"
              status = "ERR"
              response = Error "Some error" }

        let expected: Result<Person, _> = Error(StatementError "Some error")

        let result =
            statement
            |> Statement.tryGetRequiredRecord<Person> Json.defaultOptions

        test <@ result = expected @>

    let tryGetRequiredRecordOfOkTestCases () =
        seq {
            [| john |], Ok john
            [||], Error(ProtocolError ExpectedSingleItem)
            [| john; jane |], Error(ProtocolError ExpectedSingleItem)
        }
        |> Seq.map (fun (json, expected) -> [| json :> obj; expected |])

    [<Theory>]
    [<MemberData(nameof (tryGetRequiredRecordOfOkTestCases))>]
    let ``Statement.tryGetRequiredRecordOf with success response`` (response: Person []) expected =
        let statement: Statement<Person []> =
            { time = "1s"
              status = "OK"
              response = Ok(response) }

        let result =
            statement
            |> Statement.tryGetRequiredRecordOf<Person>

        test <@ result = expected @>


    let tryGetOptionalRecordOkTestCases () =
        seq {
            $"[{johnJson}]", Ok (ValueSome john)
            "[]", Ok ValueNone
            $"[{johnJson}, {janeJson}]", Error(ProtocolError ExpectedOptionalItem)
            "{}", Error(ProtocolError ExpectedArray)
        }
        |> Seq.map (fun (json, expected) -> [| json :> obj; expected |])

    [<Theory>]
    [<MemberData(nameof (tryGetOptionalRecordOkTestCases))>]
    let ``Statement.tryGetOptionalRecord with success response`` (json: string) expected =
        let statement: Statement =
            { time = "1s"
              status = "OK"
              response = Ok(JsonNode.Parse(json)) }

        let result =
            statement
            |> Statement.tryGetOptionalRecord<Person> Json.defaultOptions

        test <@ result = expected @>

    [<Fact>]
    let ``Statement.tryGetOptionalRecord with error response`` () =
        let statement: Statement =
            { time = "1s"
              status = "ERR"
              response = Error "Some error" }

        let expected: Result<Person voption, _> = Error(StatementError "Some error")

        let result =
            statement
            |> Statement.tryGetOptionalRecord<Person> Json.defaultOptions

        test <@ result = expected @>

    let tryGetOptionalRecordOfOkTestCases () =
        seq {
            [| john |], Ok (ValueSome john)
            [||], Ok ValueNone
            [| john; jane |], Error(ProtocolError ExpectedOptionalItem)
        }
        |> Seq.map (fun (json, expected) -> [| json :> obj; expected |])

    [<Theory>]
    [<MemberData(nameof (tryGetOptionalRecordOfOkTestCases))>]
    let ``Statement.tryGetOptionalRecordOf with success response`` (response: Person []) expected =
        let statement: Statement<Person []> =
            { time = "1s"
              status = "OK"
              response = Ok(response) }

        let result =
            statement
            |> Statement.tryGetOptionalRecordOf<Person>

        test <@ result = expected @>


    let tryGetNoRecordsOkTestCases () =
        seq {
            "[]", Ok ()
            $"[{johnJson}]", Error(ProtocolError ExpectedEmptyArray)
            "{}", Error(ProtocolError ExpectedArray)
        }
        |> Seq.map (fun (json, expected) -> [| json :> obj; expected |])

    [<Theory>]
    [<MemberData(nameof (tryGetNoRecordsOkTestCases))>]
    let ``Statement.tryGetNoRecords with success response`` (json: string) expected =
        let statement: Statement =
            { time = "1s"
              status = "OK"
              response = Ok(JsonNode.Parse(json)) }

        let result =
            statement
            |> Statement.tryGetNoRecords

        test <@ result = expected @>

    [<Fact>]
    let ``Statement.tryGetNoRecords with error response`` () =
        let statement: Statement =
            { time = "1s"
              status = "ERR"
              response = Error "Some error" }

        let expected: Result<unit, _> = Error(StatementError "Some error")

        let result =
            statement
            |> Statement.tryGetNoRecords

        test <@ result = expected @>

    let tryGetNoRecordsOfOkTestCases () =
        seq {
            [||], Ok ()
            [| john |], Error (ProtocolError ExpectedEmptyArray)
        }
        |> Seq.map (fun (json, expected) -> [| json :> obj; expected |])

    [<Theory>]
    [<MemberData(nameof (tryGetNoRecordsOfOkTestCases))>]
    let ``Statement.tryGetNoRecordsOf with success response`` (response: Person []) expected =
        let statement: Statement<Person []> =
            { time = "1s"
              status = "OK"
              response = Ok(response) }

        let result =
            statement
            |> Statement.tryGetNoRecordsOf

        test <@ result = expected @>


    let tryGetMultipleRecordsOkTestCases () =
        seq {
            $"[{johnJson}]", Ok [|john|]
            "[]", Ok [||]
            $"[{johnJson}, {janeJson}]", Ok [|john; jane|]
            "{}", Error(ProtocolError ExpectedArray)
        }
        |> Seq.map (fun (json, expected) -> [| json :> obj; expected |])

    [<Theory>]
    [<MemberData(nameof (tryGetMultipleRecordsOkTestCases))>]
    let ``Statement.tryGetMultipleRecords with success response`` (json: string) expected =
        let statement: Statement =
            { time = "1s"
              status = "OK"
              response = Ok(JsonNode.Parse(json)) }

        let result =
            statement
            |> Statement.tryGetMultipleRecords<Person> Json.defaultOptions

        test <@ result = expected @>

    [<Fact>]
    let ``Statement.tryGetMultipleRecords with error response`` () =
        let statement: Statement =
            { time = "1s"
              status = "ERR"
              response = Error "Some error" }

        let expected: Result<Person[], _> = Error(StatementError "Some error")

        let result =
            statement
            |> Statement.tryGetMultipleRecords<Person> Json.defaultOptions

        test <@ result = expected @>

    let tryGetMultipleRecordsOfOkTestCases () =
        seq {
            [| john |], (Ok [|john|] : Result<Person[], RequestError>)
            [||], Ok [||]
            [| john; jane |], Ok [|john; jane|]
        }
        |> Seq.map (fun (json, expected) -> [| json :> obj; expected |])

    [<Theory>]
    [<MemberData(nameof (tryGetMultipleRecordsOfOkTestCases))>]
    let ``Statement.tryGetMultipleRecordsOf with success response`` (response: Person []) expected =
        let statement: Statement<Person []> =
            { time = "1s"
              status = "OK"
              response = Ok(response) }

        let result =
            statement
            |> Statement.tryGetMultipleRecordsOf<Person>

        test <@ result = expected @>
