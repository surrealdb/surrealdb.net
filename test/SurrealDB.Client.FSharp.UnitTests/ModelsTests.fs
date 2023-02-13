namespace SurrealDB.Client.FSharp

open System
open System.Globalization
open System.Text.Json.Nodes

open Xunit
open Swensen.Unquote

open SurrealDB.Client.FSharp

[<Trait(Category, UnitTest)>]
[<Trait(Area, COMMON)>]
module ModelsTests =
    type Person = { id: SurrealId; name: string }

    let tryGetRequiredRecordOkTestCases () =
        seq {
            """[{"id": "people:john", "name": "John"}]""",
            Ok
                { id = SurrealId.Parse "people:john"
                  name = "John" }

            """[]""", Error(ProtocolError Statement.EXPECTED_SINGLE_ITEM)

            """[
                {"id": "people:john", "name": "John"},
                {"id": "people:jane", "name": "Jane"}
            ]""",
            Error(ProtocolError Statement.EXPECTED_SINGLE_ITEM)

            """{}""", Error(ProtocolError Statement.EXPECTED_ARRAY)
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

        let expected : Result<Person, _> = Error (StatementError "Some error")

        let result =
            statement
            |> Statement.tryGetRequiredRecord<Person> Json.defaultOptions

        test <@ result = expected @>
