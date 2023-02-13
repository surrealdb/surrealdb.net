namespace SurrealDB.Client.FSharp.Rest

open System
open System.Globalization
open System.Text.Json.Nodes

open Xunit
open Swensen.Unquote

open SurrealDB.Client.FSharp
open SurrealDB.Client.FSharp.Rest

[<Trait(Category, UnitTest)>]
[<Trait(Area, REST)>]
module ModelsTests =

    let emptyHeaders =
        { version = ""
          server = ""
          status = enum 0
          date = "" }

    let emptyStatement =
        { time = ""
          status = ""
          response = Ok(JsonValue.Create(null)) }

    [<Fact>]
    let ``HeadersInfo.dateTime`` () =
        let info =
            { emptyHeaders with date = "Mon, 06 Feb 2023 16:52:39 GMT" }

        let expectedDateTime =
            DateTime.Parse(info.date, CultureInfo.InvariantCulture, DateTimeStyles.None)

        let dateTime = info.dateTime
        test <@ dateTime = ValueSome expectedDateTime @>

    [<Fact>]
    let ``HeadersInfo.dateTimeOffset`` () =
        let info =
            { emptyHeaders with date = "Mon, 06 Feb 2023 16:52:39 GMT" }

        let expectedDateTime =
            DateTimeOffset.Parse(info.date, CultureInfo.InvariantCulture, DateTimeStyles.None)

        let dateTimeOffset = info.dateTimeOffset
        test <@ dateTimeOffset = ValueSome expectedDateTime @>
