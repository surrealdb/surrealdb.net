namespace SurrealDB.Client.FSharp

open System
open System.Text.Json
open Xunit
open Swensen.Unquote

open SurrealDB.Client.FSharp

[<Trait(Category, UnitTest)>]
[<Trait(Area, COMMON)>]
module SurrealIdTests =

    [<Theory>]
    [<InlineData("people", "john")>]
    let ``TryCreate valid`` table id =
        let actual = SurrealId.TryCreate(table, id)
        let expected = { table = table; id = id }
        match actual with
        | Ok actual -> test <@ actual = expected @>
        | Error error -> Assert.Fail (sprintf "Expected Ok, but got Error: %s" error)

    [<Theory>]
    [<InlineData("", "john", EMPTY_TABLE_NAME)>]
    [<InlineData("people", "", EMPTY_ID)>]
    [<InlineData("peo:ple", "john", INVALID_TABLE_NAME)>]
    [<InlineData("", "", EMPTY_TABLE_NAME)>]
    let ``TryCreate invalid`` table id expectedError =
        let actual = SurrealId.TryCreate(table, id)
        match actual with
        | Ok _ -> Assert.Fail "Expected Error, but got Ok"
        | Error error -> test <@ error = expectedError @>

    [<Theory>]
    [<InlineData("people", "john")>]
    let ``Create valid`` table id =
        let actual = SurrealId.Create(table, id)
        let expected = { table = table; id = id }
        test <@ actual = expected @>

    [<Theory>]
    [<InlineData("", "john", EMPTY_TABLE_NAME)>]
    [<InlineData("people", "", EMPTY_ID)>]
    [<InlineData("", "", EMPTY_TABLE_NAME)>]
    let ``Create invalid`` table id expectedError =
        let action = fun () -> SurrealId.Create(table, id) |> ignore
        let actual = Assert.Throws<FormatException>(action)
        test <@ actual.Message = expectedError @>

    [<Theory>]
    [<InlineData("people", "john", "people:john")>]
    let ``ToString`` table id expected =
        let actual = SurrealId.Create(table, id).ToString()
        test <@ actual = expected @>

    [<Theory>]
    [<InlineData("people:john", "people", "john")>]
    let ``TryParse valid`` identifier expectedTable expectedId =
        let actual = SurrealId.TryParse(identifier)
        let expected = { table = expectedTable; id = expectedId }
        match actual with
        | Ok actual -> test <@ actual = expected @>
        | Error error -> Assert.Fail (sprintf "Expected Ok, but got Error: %s" error)

    [<Theory>]
    [<InlineData("people", MISSING_COLON)>]
    [<InlineData("people:", EMPTY_ID)>]
    [<InlineData(":john", EMPTY_TABLE_NAME)>]
    [<InlineData("", MISSING_COLON)>]
    let ``TryParse invalid`` identifier expectedError =
        let actual = SurrealId.TryParse(identifier)
        match actual with
        | Ok _ -> Assert.Fail $"Expected {expectedError}, but got Ok: {identifier}"
        | Error error -> test <@ error = expectedError @>

    [<Theory>]
    [<InlineData("people:john", "people", "john")>]
    let ``Parse valid`` identifier expectedTable expectedId =
        let actual = SurrealId.Parse(identifier)
        let expected = { table = expectedTable; id = expectedId }
        test <@ actual = expected @>

    [<Theory>]
    [<InlineData("people", MISSING_COLON)>]
    [<InlineData("people:", EMPTY_ID)>]
    [<InlineData(":john", EMPTY_TABLE_NAME)>]
    [<InlineData("", MISSING_COLON)>]
    let ``Parse invalid`` identifier expectedError =
        let action = fun () -> SurrealId.Parse(identifier) |> ignore
        let actual = Assert.Throws<FormatException>(action)
        test <@ actual.Message = expectedError @>

    [<Fact>]
    let ``Serialize as JSON``() =
        let data = [| SurrealId.Parse("people:john") |]
        let actual = JsonSerializer.Serialize(data, SurrealConfig.defaultJsonOptions)
        let expected = "[\"people:john\"]"
        test <@ actual = expected @>

    [<Fact>]
    let ``Deserialize from JSON``() =
        let json = "[\"people:john\"]"
        let actual = JsonSerializer.Deserialize<SurrealId[]>(json, SurrealConfig.defaultJsonOptions)
        let expected = [| SurrealId.Parse("people:john") |]
        test <@ actual = expected @>

