module SurrealDB.Client.FSharp.Rest.ModelsTests

open System
open System.Collections.Generic
open System.Globalization
open System.Net
open System.Net.Http
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open System.Threading

open Xunit
open FsCheck.Xunit
open Swensen.Unquote

open SurrealDB.Client.FSharp
open SurrealDB.Client.FSharp.Rest

[<Fact>]
let ``HeadersInfo.empty`` () =
    let expected: HeadersInfo =
        { version = ""
          server = ""
          status = enum 0
          date = "" }

    let actual = HeadersInfo.empty

    test <@ expected = actual @>

[<Fact>]
let ``HeadersInfo.dateTime`` () =
    let info =
        { HeadersInfo.empty with date = "Mon, 06 Feb 2023 16:52:39 GMT" }

    let expectedDateTime =
        DateTime.Parse(info.date, CultureInfo.InvariantCulture, DateTimeStyles.None)

    let dateTime = info.dateTime
    test <@ dateTime = ValueSome expectedDateTime @>

[<Fact>]
let ``HeadersInfo.dateTimeOffset`` () =
    let info =
        { HeadersInfo.empty with date = "Mon, 06 Feb 2023 16:52:39 GMT" }

    let expectedDateTime =
        DateTimeOffset.Parse(info.date, CultureInfo.InvariantCulture, DateTimeStyles.None)

    let dateTimeOffset = info.dateTimeOffset
    test <@ dateTimeOffset = ValueSome expectedDateTime @>

[<Fact>]
let ``HeadersInfo.parse all headers`` () =
    let expected =
        { HeadersInfo.empty with
            version = "surreal-1.0.0-beta.8+20220930.c246533"
            server = "SurrealDB"
            status = HttpStatusCode.Accepted
            date = "Mon, 06 Feb 2023 16:52:39 GMT" }

    use response = new HttpResponseMessage()
    response.Headers.Add(VERSION_HEADER, expected.version)
    response.Headers.Add(SERVER_HEADER, expected.server)
    response.Headers.Add(DATE_HEADER, expected.date)
    response.StatusCode <- expected.status

    let actual = HeadersInfo.parse response

    test <@ actual = expected @>

[<Fact>]
let ``HeadersInfo.parse no headers`` () =
    let expected =
        { HeadersInfo.empty with status = HttpStatusCode.Accepted }

    use response = new HttpResponseMessage()
    response.StatusCode <- expected.status

    let actual = HeadersInfo.parse response

    test <@ actual = expected @>

[<Fact>]
let ``ItemSuccessInfo.empty`` () =
    let expected: ItemSuccessInfo<int> = { status = ""; result = 0; time = "" }

    let actual: ItemSuccessInfo<int> = ItemSuccessInfo.empty ()

    test <@ actual = expected @>

let timeSpanCases () =
    seq {
        "", ValueNone
        "42.1µs", ValueSome(TimeSpan.FromMicroseconds 42.1)
        "3.146ms", ValueSome(TimeSpan.FromMilliseconds 3.146)
        "1.234s", ValueSome(TimeSpan.FromSeconds 1.234)
        "2.3h", ValueNone
        "12345678901234567890s", ValueNone
    }
    |> Seq.map (fun (time, expected) -> [| time :> obj; expected |])

[<Theory>]
[<MemberData(nameof (timeSpanCases))>]
let ``ItemSuccessInfo.timeSpan`` (time: string) (expected: TimeSpan voption) =
    let info =
        { ItemSuccessInfo.empty () with time = time }

    let actual = info.timeSpan

    test <@ actual = expected @>

[<Fact>]
let ``ItemErrorInfo.empty`` () =
    let expected = { status = ""; detail = ""; time = "" }

    let actual = ItemErrorInfo.empty ()

    test <@ actual = expected @>

[<Theory>]
[<MemberData(nameof (timeSpanCases))>]
let ``ItemErrorInfo.timeSpan`` (time: string) (expected: TimeSpan voption) =
    let info =
        { ItemErrorInfo.empty () with time = time }

    let actual = info.timeSpan

    test <@ actual = expected @>

[<Fact>]
let ``ErrorInfo.empty`` () =
    let expected =
        { details = ""
          code = 0
          description = ""
          information = "" }

    let actual = ErrorInfo.empty

    test <@ actual = expected @>

[<Fact>]
let ``RestApiResult.empty`` () =
    let expected: RestApiResult<int> =
        { headers = HeadersInfo.empty
          result = Error ErrorInfo.empty }

    let actual: RestApiResult<int> = RestApiResult.empty ()

    test <@ actual = expected @>

[<Fact>]
let ``RestApiResult.parse with error response`` () =
    task {
        let headersInfo =
            { HeadersInfo.empty with
                version = "surreal-1.0.0-beta.8+20220930.c246533"
                server = "SurrealDB"
                status = HttpStatusCode.BadRequest
                date = "Mon, 06 Feb 2023 16:52:39 GMT" }

        let errorInfo =
            { details = "Request problems detected"
              code = 400
              description = "There is a problem with your request. Refer to the documentation for further information."
              information =
                "There was a problem with the database: Parse error on line 1 at character 0 when parsing 'INFO FO R NS;'" }

        let contentJson =
            """{
  "code": 400,
  "details": "Request problems detected",
  "description": "There is a problem with your request. Refer to the documentation for further information.",
  "information": "There was a problem with the database: Parse error on line 1 at character 0 when parsing 'INFO FO R NS;'"
}"""

        let expected: RestApiResult<JsonNode> =
            { headers = headersInfo
              result = Error errorInfo }

        let cToken = CancellationToken.None
        let jOptions = JsonSerializerOptions()

        use response = new HttpResponseMessage()
        response.Headers.Add(VERSION_HEADER, headersInfo.version)
        response.Headers.Add(SERVER_HEADER, headersInfo.server)
        response.Headers.Add(DATE_HEADER, headersInfo.date)
        response.StatusCode <- headersInfo.status
        response.Content <- new StringContent(contentJson, Encoding.UTF8, "application/json")

        let! actual = RestApiResult.parse jOptions cToken response

        test <@ actual = expected @>
    }

[<Fact>]
let ``RestApiResult.parse with array result`` () =
    task {
        let headersInfo =
            { HeadersInfo.empty with
                version = "surreal-1.0.0-beta.8+20220930.c246533"
                server = "SurrealDB"
                status = HttpStatusCode.OK
                date = "Mon, 06 Feb 2023 16:52:39 GMT" }

        let contentJson =
            """[
  {
    "time": "3.474ms",
    "status": "OK",
    "result": [
      {
        "age": 20,
        "firstName": "John",
        "id": "people:j8cbt874jgipk2y9czth",
        "lastName": "Doe"
      }
    ]
  }
]"""

        let cToken = CancellationToken.None
        let jOptions = JsonSerializerOptions()

        use response = new HttpResponseMessage()
        response.Headers.Add(VERSION_HEADER, headersInfo.version)
        response.Headers.Add(SERVER_HEADER, headersInfo.server)
        response.Headers.Add(DATE_HEADER, headersInfo.date)
        response.StatusCode <- headersInfo.status
        response.Content <- new StringContent(contentJson, Encoding.UTF8, "application/json")

        let! actual = RestApiResult.parse jOptions cToken response
        let actualHeaders = actual.headers

        test <@ actualHeaders = headersInfo @>

        match actual.result with
        | Ok [| Ok actualItemResult |] ->
            Assert.Equal(actualItemResult.status, "OK")
            Assert.Equal(actualItemResult.time, "3.474ms")

            let jArray =
                Assert.IsType<JsonArray>(actualItemResult.result)

            Assert.Equal(1, jArray.Count)
            let elem = Assert.IsType<JsonObject>(jArray.[0])
            Assert.Equal(4, elem.Count)

            match elem.TryGetPropertyValue("age") with
            | true, node ->
                let node = Assert.IsAssignableFrom<JsonValue>(node)
                Assert.Equal((true, 20), node.TryGetValue())
            | false, _ -> Assert.Fail "Expected property age"

            match elem.TryGetPropertyValue("firstName") with
            | true, node ->
                let node = Assert.IsAssignableFrom<JsonValue>(node)
                Assert.Equal((true, "John"), node.TryGetValue())
            | false, _ -> Assert.Fail "Expected property firstName"

            match elem.TryGetPropertyValue("lastName") with
            | true, node ->
                let node = Assert.IsAssignableFrom<JsonValue>(node)
                Assert.Equal((true, "Doe"), node.TryGetValue())
            | false, _ -> Assert.Fail "Expected property lastName"

            match elem.TryGetPropertyValue("id") with
            | true, node ->
                let node = Assert.IsAssignableFrom<JsonValue>(node)
                Assert.Equal((true, "people:j8cbt874jgipk2y9czth"), node.TryGetValue())
            | false, _ -> Assert.Fail "Expected property id"

        | _ -> Assert.Fail "Unexpected result"
    }

[<Fact>]
let ``RestApiResult.parse with array error`` () =
    task {
        let headersInfo =
            { HeadersInfo.empty with
                version = "surreal-1.0.0-beta.8+20220930.c246533"
                server = "SurrealDB"
                status = HttpStatusCode.OK
                date = "Mon, 06 Feb 2023 16:52:39 GMT" }

        let contentJson =
            """[
  {
    "time": "3.474ms",
    "status": "ERR",
    "detail": "Database record `people:john` already exists"
  }
]"""

        let cToken = CancellationToken.None
        let jOptions = JsonSerializerOptions()

        use response = new HttpResponseMessage()
        response.Headers.Add(VERSION_HEADER, headersInfo.version)
        response.Headers.Add(SERVER_HEADER, headersInfo.server)
        response.Headers.Add(DATE_HEADER, headersInfo.date)
        response.StatusCode <- headersInfo.status
        response.Content <- new StringContent(contentJson, Encoding.UTF8, "application/json")

        let! actual = RestApiResult.parse jOptions cToken response
        let actualHeaders = actual.headers

        test <@ actualHeaders = headersInfo @>

        match actual.result with
        | Ok [| Error actualItemResult |] ->
            Assert.Equal(actualItemResult.status, "ERR")
            Assert.Equal(actualItemResult.time, "3.474ms")
            Assert.Equal(actualItemResult.detail, "Database record `people:john` already exists")

        | _ -> Assert.Fail "Unexpected result"
    }
