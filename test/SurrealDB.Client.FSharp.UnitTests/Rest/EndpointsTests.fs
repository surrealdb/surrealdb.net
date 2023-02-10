module SurrealDB.Client.FSharp.Rest.EndpointsTests

open System
open System.Globalization
open System.Net
open System.Net.Http
open System.Text
open System.Text.Json.Nodes
open System.Threading

open Xunit
open Swensen.Unquote

open SurrealDB.Client.FSharp
open SurrealDB.Client.FSharp.Rest

let resultValue =
    function
    | Ok value -> value
    | Error err -> failwith $"Expected Ok, got Error: %A{err}"

let tryToOption =
    function
    | true, value -> Some value
    | false, _ -> None

let tryGetHeaders name (headers: Headers.HttpRequestHeaders) =
    headers.TryGetValues(name)
    |> tryToOption
    |> Option.map Seq.toList

let applyConfigTestCases () =
    let johnDoeToken =
        "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c"

    seq {
        SurrealConfig
            .Builder()
            .WithBaseUrl("http://localhost:8010")
            .WithNamespace("testns")
            .WithDatabase("testdb")
            .Build()
        |> resultValue,
        "testns",
        "testdb",
        "http://localhost:8010",
        None

        SurrealConfig
            .Builder()
            .WithBaseUrl("http://localhost:8010")
            .WithNamespace("testns")
            .WithDatabase("testdb")
            .WithBasicCredentials("root", "root")
            .Build()
        |> resultValue,
        "testns",
        "testdb",
        "http://localhost:8010",
        Some "Basic cm9vdDpyb290"

        SurrealConfig
            .Builder()
            .WithBaseUrl("http://localhost:8010")
            .WithNamespace("testns")
            .WithDatabase("testdb")
            .WithBearerCredentials(johnDoeToken)
            .Build()
        |> resultValue,
        "testns",
        "testdb",
        "http://localhost:8010",
        Some $"Bearer {johnDoeToken}"
    }
    |> Seq.map (fun (config, ns, db, baseUrl, credentials) ->
        [| config :> obj
           ns
           db
           baseUrl
           credentials |])

[<Theory>]
[<MemberData(nameof (applyConfigTestCases))>]
let ``applyConfig``
    (config: SurrealConfig)
    (expectedNs: string)
    (expectedDb: string)
    (expectedBaseUrl: string)
    (expectedCredentials: string option)
    =
    use httpClient = new HttpClient()

    httpClient |> Endpoints.applyConfig config

    test <@ httpClient.BaseAddress = Uri(expectedBaseUrl) @>

    let acceptHeader =
        httpClient.DefaultRequestHeaders
        |> tryGetHeaders ACCEPT_HEADER

    test <@ acceptHeader = Some [ APPLICATION_JSON ] @>

    let nsHeader =
        httpClient.DefaultRequestHeaders
        |> tryGetHeaders NS_HEADER

    test <@ nsHeader = Some [ expectedNs ] @>

    let dbHeader =
        httpClient.DefaultRequestHeaders
        |> tryGetHeaders DB_HEADER

    test <@ dbHeader = Some [ expectedDb ] @>

    let authHeader =
        httpClient.DefaultRequestHeaders
        |> tryGetHeaders AUTHORIZATION_HEADER

    let expectedCredentials =
        expectedCredentials
        |> Option.map (fun value -> [ value ])

    test <@ authHeader = expectedCredentials @>

let PORT = 8000
let USER = "root"
let PASS = "root"
let NS = "testns"
let DB = "testdb"
let DUMMY_VERSION = "dummy-version"
let DUMMY_SERVER = "dummy-server"
let DUMMY_DATE = "Fri, 10 Feb 2023 20:49:37 GMT"

let DUMMY_DATETIME =
    DateTime.Parse(DUMMY_DATE, CultureInfo.InvariantCulture, DateTimeStyles.None)

let DUMMY_DATETIMEOFFSET =
    DateTimeOffset.Parse(DUMMY_DATE, CultureInfo.InvariantCulture, DateTimeStyles.None)

let prepareTest statusCode (responseJson: string) =
    let config =
        SurrealConfig
            .Builder()
            .WithBaseUrl($"http://localhost:%d{PORT}")
            .WithBasicCredentials(USER, PASS)
            .WithNamespace(NS)
            .WithDatabase(DB)
            .Build()
        |> resultValue

    let requests = ResizeArray()

    let handler =
        { new HttpMessageHandler() with
            override this.SendAsync(request, ct) =
                task {
                    requests.Add(request)
                    let response = new HttpResponseMessage(statusCode)
                    response.Headers.Add(VERSION_HEADER, DUMMY_VERSION)
                    response.Headers.Add(SERVER_HEADER, DUMMY_SERVER)
                    response.Headers.Add(DATE_HEADER, DUMMY_DATE)

                    let content =
                        new StringContent(responseJson, Encoding.UTF8, APPLICATION_JSON)

                    response.Content <- content
                    return response
                } }

    let httpClient = new HttpClient(handler)

    Endpoints.applyConfig config httpClient

    let jsonOptions = SurrealConfig.defaultJsonOptions

    let cancellationTokenSource = new CancellationTokenSource()

    let disposable =
        { new IDisposable with
            member this.Dispose() =
                cancellationTokenSource.Dispose()
                httpClient.Dispose() }

    {| config = config
       httpClient = httpClient
       jsonOptions = jsonOptions
       requests = requests
       cancellationTokenSource = cancellationTokenSource
       cancellationToken = cancellationTokenSource.Token
       disposable = disposable |}

[<Fact>]
let ``postSql with error response`` () =
    task {
        let expectedStatus = HttpStatusCode.BadRequest

        let testing =
            prepareTest
                expectedStatus
                """{
  "code": 400,
  "details": "Request problems detected",
  "description": "There is a problem with your request. Refer to the documentation for further information.",
  "information": "There was a problem with the database: Parse error on line 1 at character 0 when parsing 'INFO FO R KV;\r\nUSE NS testns DB testdb;'"
}"""

        use _ = testing.disposable

        let query =
            """INFO FO R KV;
USE NS testns DB testdb;"""

        let! response =
            testing.httpClient
            |> Endpoints.postSql testing.jsonOptions query testing.cancellationToken

        test <@ testing.requests.Count = 1 @>

        let request = testing.requests.[0]

        test <@ request.Method = HttpMethod.Post @>
        test <@ request.RequestUri = Uri($"http://localhost:%d{PORT}/sql", UriKind.Absolute) @>

        match request.Content with
        | :? StringContent as content ->
            let contentType = content.Headers.ContentType
            test <@ contentType.MediaType = TEXT_PLAIN @>
            test <@ contentType.CharSet = "utf-8" @>

            let! text = content.ReadAsStringAsync()
            test <@ text = query @>
        | _ -> Assert.Fail "Expected StringContent"

        let expectedHeaders: HeadersInfo =
            { version = DUMMY_VERSION
              server = DUMMY_SERVER
              status = expectedStatus
              date = DUMMY_DATE }

        let expectedResult: ErrorInfo =
            { code = 400
              details = "Request problems detected"
              description = "There is a problem with your request. Refer to the documentation for further information."
              information =
                "There was a problem with the database: Parse error on line 1 at character 0 when parsing 'INFO FO R KV;\r\nUSE NS testns DB testdb;'" }

        test <@ response.headers = expectedHeaders @>
        test <@ response.headers.dateTime = ValueSome(DUMMY_DATETIME) @>
        test <@ response.headers.dateTimeOffset = ValueSome(DUMMY_DATETIMEOFFSET) @>

        match response.result with
        | Ok _ -> Assert.Fail "Expected error response"
        | Error result -> test <@ result = expectedResult @>
    }

[<Fact>]
let ``postSql with empty response`` () =
    task {
        let expectedStatus = HttpStatusCode.OK

        let testing =
            prepareTest
                expectedStatus
                """[
 {
   "time": "16.2ms",
   "status": "OK",
   "result": []
 }
]"""

        use _ = testing.disposable

        let query = "SELECT * FROM person WHERE age >= 18;"

        let! response =
            testing.httpClient
            |> Endpoints.postSql testing.jsonOptions query testing.cancellationToken

        let expectedHeaders: HeadersInfo =
            { version = DUMMY_VERSION
              server = DUMMY_SERVER
              status = expectedStatus
              date = DUMMY_DATE }

        let expectedJson = JsonArray()

        test <@ response.headers = expectedHeaders @>

        match response.result with
        | Error _ -> Assert.Fail "Expected success response"
        | Ok result ->
            test <@ result.Length = 1 @>
            let first = result.[0]
            test <@ first.time = "16.2ms" @>
            test <@ first.timeSpan = ValueSome(TimeSpan.FromMilliseconds(16.2)) @>
            test <@ first.status = "OK" @>

            match first.response with
            | Error _ -> Assert.Fail "Expected success response"
            | Ok json -> test <@ jsonDiff json expectedJson = [] @>
    }

[<Fact>]
let ``postSql with array response`` () =
    task {
        let expectedStatus = HttpStatusCode.OK

        let testing =
            prepareTest
                expectedStatus
                """[
  {
    "time": "151.3µs",
    "status": "OK",
    "result": [
      {
        "age": 19,
        "firstName": "John",
        "id": "person:dr3mc523txrii4cfuczh",
        "lastName": "Doe"
      },
      {
        "age": 17,
        "firstName": "Jane",
        "id": "person:zi1e78q4onfh6wypk5bz",
        "lastName": "Doe"
      }
    ]
  }
]"""

        use _ = testing.disposable

        let query =
            """INSERT INTO person
            (firstName, lastName, age) VALUES
            ("John", "Doe", 19),
            ("Jane", "Doe", 17);"""

        let! response =
            testing.httpClient
            |> Endpoints.postSql testing.jsonOptions query testing.cancellationToken

        let expectedHeaders: HeadersInfo =
            { version = DUMMY_VERSION
              server = DUMMY_SERVER
              status = expectedStatus
              date = DUMMY_DATE }

        let expectedJson =
            JsonArray(
                JsonObject(
                    dict [ "firstName", JsonValue.Create("John") :> JsonNode
                           "lastName", JsonValue.Create("Doe")
                           "age", JsonValue.Create(19)
                           "id", JsonValue.Create("person:dr3mc523txrii4cfuczh") ]
                ),
                JsonObject(
                    dict [ "firstName", JsonValue.Create("Jane") :> JsonNode
                           "lastName", JsonValue.Create("Doe")
                           "age", JsonValue.Create(17)
                           "id", JsonValue.Create("person:zi1e78q4onfh6wypk5bz") ]
                )
            )

        test <@ response.headers = expectedHeaders @>

        match response.result with
        | Error _ -> Assert.Fail "Expected success response"
        | Ok result ->
            test <@ result.Length = 1 @>
            let first = result.[0]
            test <@ first.time = "151.3µs" @>
            test <@ first.timeSpan = ValueSome(TimeSpan.FromMilliseconds(0.1513)) @>
            test <@ first.status = "OK" @>

            match first.response with
            | Error _ -> Assert.Fail "Expected success response"
            | Ok json -> test <@ jsonDiff json expectedJson = [] @>
    }

[<Fact>]
let ``postSql with multiple and mixed responses`` () =
    task {
        let expectedStatus = HttpStatusCode.OK

        let testing =
            prepareTest
                expectedStatus
                """[
  {
    "time": "3.4s",
    "status": "OK",
    "result": {
      "ns": {
        "testns": "DEFINE NAMESPACE testns"
      }
    }
  },
  {
    "time": "4.4µs",
    "status": "OK",
    "result": null
  }
]"""

        use _ = testing.disposable

        let query =
            """INFO FOR KV;
USE NS testns DB testdb;"""

        let! response =
            testing.httpClient
            |> Endpoints.postSql testing.jsonOptions query testing.cancellationToken

        let expectedHeaders: HeadersInfo =
            { version = DUMMY_VERSION
              server = DUMMY_SERVER
              status = expectedStatus
              date = DUMMY_DATE }

        let expectedJson1 =
            JsonObject(
                dict [ "ns", JsonObject(dict [
                    "testns", JsonValue.Create("DEFINE NAMESPACE testns") :> JsonNode
                ]) :> JsonNode ]
            )

        let expectedJson2 : JsonNode = JsonValue.Create(null)

        test <@ response.headers = expectedHeaders @>

        match response.result with
        | Error _ -> Assert.Fail "Expected success response"
        | Ok result ->
            test <@ result.Length = 2 @>

            let statement = result.[0]
            test <@ statement.time = "3.4s" @>
            test <@ statement.timeSpan = ValueSome(TimeSpan.FromSeconds(3.4)) @>
            test <@ statement.status = "OK" @>

            match statement.response with
            | Error _ -> Assert.Fail "Expected success response"
            | Ok json -> test <@ jsonDiff json expectedJson1 = [] @>

            let statement = result.[1]
            test <@ statement.time = "4.4µs" @>
            test <@ statement.timeSpan = ValueSome(TimeSpan.FromMilliseconds(0.0044)) @>
            test <@ statement.status = "OK" @>

            match statement.response with
            | Error _ -> Assert.Fail "Expected success response"
            | Ok json -> test <@ jsonDiff json expectedJson2 = [] @>
    }
