namespace SurrealDB.Client.FSharp.Rest

open System
open System.Globalization
open System.Net
open System.Net.Http
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open System.Threading

open Xunit
open Swensen.Unquote

open SurrealDB.Client.FSharp
open SurrealDB.Client.FSharp.Rest

[<Trait(Category, UnitTest)>]
[<Trait(Area, REST)>]
module SurrealEndpointsTests =
    type Testing =
        { config: SurrealConfig
          httpClient: HttpClient
          jsonOptions: JsonSerializerOptions
          endpoints: ISurrealEndpoints
          requests: ResizeArray<HttpRequestMessage>
          cancellationTokenSource: CancellationTokenSource
          cancellationToken: CancellationToken
          disposable: IDisposable }

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

    let prepareTestWith (config: SurrealConfig) statusCode (responseJson: string) =
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

        let jsonOptions = Json.defaultOptions

        let endpoints: ISurrealEndpoints =
            new SurrealEndpoints(config, httpClient, jsonOptions)

        let cancellationTokenSource = new CancellationTokenSource()

        let disposable =
            { new IDisposable with
                member this.Dispose() =
                    cancellationTokenSource.Dispose()
                    endpoints.Dispose() }

        { config = config
          httpClient = httpClient
          jsonOptions = jsonOptions
          endpoints = endpoints
          requests = requests
          cancellationTokenSource = cancellationTokenSource
          cancellationToken = cancellationTokenSource.Token
          disposable = disposable }

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

        prepareTestWith config statusCode responseJson

    let applyConfigTestCases () =
        let baseUrl = "http://localhost:8010"
        let ns = "testns"
        let db = "testdb"
        let user = "root"
        let pass = "root"
        let basicToken = "cm9vdDpyb290"

        let johnDoeToken =
            "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxMjM0NTY3ODkwIiwibmFtZSI6IkpvaG4gRG9lIiwiaWF0IjoxNTE2MjM5MDIyfQ.SflKxwRJSMeKKF2QT4fwpMeJf36POk6yJV_adQssw5c"

        let makeConfig (fn: SurrealConfigBuilder -> SurrealConfigBuilder) =
            fn(
                SurrealConfig
                    .Builder()
                    .WithBaseUrl(baseUrl)
                    .WithNamespace(ns)
                    .WithDatabase(db)
            )
                .Build()
            |> resultValue

        let withBasic (config: SurrealConfigBuilder) = config.WithBasicCredentials(user, pass)

        let withBearer (config: SurrealConfigBuilder) =
            config.WithBearerCredentials(johnDoeToken)

        seq {
            makeConfig id, ns, db, baseUrl, None
            makeConfig withBasic, ns, db, baseUrl, Some $"Basic {basicToken}"
            makeConfig withBearer, ns, db, baseUrl, Some $"Bearer {johnDoeToken}"
        }
        |> Seq.map (fun (config, ns, db, baseUrl, credentials) ->
            [| config :> obj
               ns
               db
               baseUrl
               credentials |])

    let sampleErrorInfo: ErrorInfo =
        { code = 400
          details = "Request problems detected"
          description = "Some description."
          information = "Some information." }

    let sampleErrorInfoJson =
        """{
            "code": 400,
            "details": "Request problems detected",
            "description": "Some description.",
            "information": "Some information."
        }"""

    let sampleJsonResponse status time result =
        sprintf
            """[
    {
        "status": "%s",
        "time": "%s",
        "result": %s
    }
]"""
            status
            time
            result

    [<Theory>]
    [<MemberData(nameof (applyConfigTestCases))>]
    let ``applyConfig when creating``
        (config: SurrealConfig)
        (expectedNs: string)
        (expectedDb: string)
        (expectedBaseUrl: string)
        (expectedCredentials: string option)
        =
        let t =
            prepareTestWith config HttpStatusCode.OK "{}"

        test <@ t.httpClient.BaseAddress = Uri(expectedBaseUrl) @>

        let acceptHeader =
            t.httpClient.DefaultRequestHeaders
            |> tryGetHeaders ACCEPT_HEADER

        test <@ acceptHeader = Some [ APPLICATION_JSON ] @>

        let nsHeader =
            t.httpClient.DefaultRequestHeaders
            |> tryGetHeaders NS_HEADER

        test <@ nsHeader = Some [ expectedNs ] @>

        let dbHeader =
            t.httpClient.DefaultRequestHeaders
            |> tryGetHeaders DB_HEADER

        test <@ dbHeader = Some [ expectedDb ] @>

        let authHeader =
            t.httpClient.DefaultRequestHeaders
            |> tryGetHeaders AUTHORIZATION_HEADER

        let expectedCredentials =
            expectedCredentials
            |> Option.map (fun value -> [ value ])

        test <@ authHeader = expectedCredentials @>

    let testRequestHeaders t expectedMethod expectedUrl =
        test <@ t.requests.Count = 1 @>
        let request = t.requests.[0]
        test <@ request.Method = expectedMethod @>
        test <@ request.RequestUri = Uri(expectedUrl, UriKind.Absolute) @>

    let testRequestNoContent t =
        test <@ t.requests.Count = 1 @>
        let request = t.requests.[0]
        let content = request.Content
        test <@ isNull content @>

    let testRequestTextContent t expectedContent =
        task {
            test <@ t.requests.Count = 1 @>
            let request = t.requests.[0]
            let content = request.Content
            test <@ not (isNull content) @>
            let contentType = content.Headers.ContentType
            test <@ contentType.MediaType = TEXT_PLAIN @>
            test <@ contentType.CharSet = "utf-8" @>
            let! text = content.ReadAsStringAsync()
            test <@ text = expectedContent @>
        }

    let testRequestJsonContent t expectedJson =
        task {
            test <@ t.requests.Count = 1 @>
            let request = t.requests.[0]
            let content = request.Content
            test <@ not (isNull content) @>
            let contentType = content.Headers.ContentType
            test <@ contentType.MediaType = APPLICATION_JSON @>
            test <@ contentType.CharSet = "utf-8" @>
            let! text = content.ReadAsStringAsync()
            let json = JsonNode.Parse(text)
            test <@ jsonDiff json expectedJson = [] @>
        }

    let testResponseHeaders (response: RestApiResult<JsonNode>) expectedStatus =
        let expectedHeaders: HeadersInfo =
            { version = DUMMY_VERSION
              server = DUMMY_SERVER
              status = expectedStatus
              date = DUMMY_DATE }

        test <@ response.headers = expectedHeaders @>
        test <@ response.headers.dateTime = ValueSome(DUMMY_DATETIME) @>
        test <@ response.headers.dateTimeOffset = ValueSome(DUMMY_DATETIMEOFFSET) @>

    let testResponseError (response: RestApiResult<JsonNode>) expectedError =
        match response.statements with
        | Ok result -> Assert.Fail $"Expected error, got {result}"
        | Error error -> test <@ error = ResponseError expectedError @>

    let testResponseJson
        (response: RestApiResult<JsonNode>)
        expectedStatus
        expectedTime
        expectedTimeSpan
        (expectedJson: string)
        =
        match response.statements with
        | Error error -> Assert.Fail $"Expected success result, got {error}"
        | Ok result ->
            let expectedJson = JsonNode.Parse(expectedJson)
            test <@ result.Length = 1 @>
            let first = result.[0]
            test <@ first.time = expectedTime @>
            test <@ first.timeSpan = ValueSome(expectedTimeSpan) @>
            test <@ first.status = expectedStatus @>

            match first.response with
            | Error error -> Assert.Fail $"Expected success result, got {error}"
            | Ok json -> test <@ jsonDiff json expectedJson = [] @>

    let testResponseStatementError
        (response: RestApiResult<JsonNode>)
        expectedStatus
        expectedTime
        expectedTimeSpan
        (expectedError: string)
        =
        match response.statements with
        | Error error -> Assert.Fail $"Expected success result, got {error}"
        | Ok result ->
            test <@ result.Length = 1 @>
            let first = result.[0]
            test <@ first.time = expectedTime @>
            test <@ first.timeSpan = ValueSome(expectedTimeSpan) @>
            test <@ first.status = expectedStatus @>

            match first.response with
            | Error error -> test <@ error = StatementError expectedError @>
            | Ok json -> Assert.Fail $"Expected error, got {json}"

    let testResponseMultipleJson
        (response: RestApiResult<JsonNode>)
        expectedStatus
        expectedTime
        expectedTimeSpan
        (expectedJsons: string [])
        =
        match response.statements with
        | Error error -> Assert.Fail $"Expected success result, got {error}"
        | Ok result ->
            test <@ result.Length = expectedJsons.Length @>

            let expectedJsons =
                expectedJsons |> Array.map JsonNode.Parse

            for i in 0 .. result.Length - 1 do
                let result = result.[i]
                let expectedJson = expectedJsons.[i]
                test <@ result.time = expectedTime @>
                test <@ result.timeSpan = ValueSome(expectedTimeSpan) @>
                test <@ result.status = expectedStatus @>

                match result.response with
                | Error error -> Assert.Fail $"Expected success result, got {error}"
                | Ok json -> test <@ jsonDiff json expectedJson = [] @>

    // Testing PostSql

    [<Fact>]
    let ``PostSql with error response`` () =
        task {
            let expectedStatus = HttpStatusCode.BadRequest

            let t =
                sampleErrorInfoJson |> prepareTest expectedStatus

            use _ = t.disposable

            let query =
                """
               INFO FO R KV;
               USE NS testns DB testdb;
               """

            let! response = t.endpoints.PostSql(query, t.cancellationToken)

            testRequestHeaders t HttpMethod.Post $"http://localhost:%d{PORT}/sql"
            do! testRequestTextContent t query

            testResponseHeaders response expectedStatus
            testResponseError response sampleErrorInfo
        }

    [<Fact>]
    let ``PostSql with empty response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let expectedJson = "[]"

            let t =
                sampleJsonResponse "OK" "16.2ms" expectedJson
                |> prepareTest expectedStatus

            use _ = t.disposable

            let query = "SELECT * FROM people WHERE age >= 18;"

            let! response = t.endpoints.PostSql(query, t.cancellationToken)

            testRequestHeaders t HttpMethod.Post $"http://localhost:%d{PORT}/sql"
            do! testRequestTextContent t query

            testResponseHeaders response expectedStatus
            testResponseJson response "OK" "16.2ms" (TimeSpan.FromMilliseconds(16.2)) expectedJson
        }

    [<Fact>]
    let ``PostSql with array response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let expectedJson =
                """[
                    {
                        "age": 19,
                        "firstName": "John",
                        "id": "people:dr3mc523txrii4cfuczh",
                        "lastName": "Doe"
                    },
                    {
                        "age": 17,
                        "firstName": "Jane",
                        "id": "people:zi1e78q4onfh6wypk5bz",
                        "lastName": "Doe"
                    }
                ]"""

            let t =
                sampleJsonResponse "OK" "151.3µs" expectedJson
                |> prepareTest expectedStatus

            use _ = t.disposable

            let query =
                """
                INSERT INTO people
                    (firstName, lastName, age) VALUES
                    ("John", "Doe", 19),
                    ("Jane", "Doe", 17);
                """

            let! response = t.endpoints.PostSql(query, t.cancellationToken)

            testRequestHeaders t HttpMethod.Post $"http://localhost:%d{PORT}/sql"
            do! testRequestTextContent t query

            testResponseHeaders response expectedStatus
            testResponseJson response "OK" "151.3µs" (TimeSpan.FromMilliseconds(0.1513)) expectedJson
        }

    [<Fact>]
    let ``PostSql with multiple and mixed responses`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let expectedJsons =
                [| """{
                    "ns": {
                        "testns": "DEFINE NAMESPACE testns"
                    }
                }"""
                   "null" |]

            let t =
                sprintf
                    """[
                    {
                        "time": "3.4s",
                        "status": "OK",
                        "result": %s
                    },
                    {
                        "time": "3.4s",
                        "status": "OK",
                        "result": %s
                    }
                ]""" expectedJsons.[0] expectedJsons.[1]
                |> prepareTest expectedStatus

            use _ = t.disposable

            let query =
                """
                INFO FOR KV;
                USE NS testns DB testdb;
                """

            let! response = t.endpoints.PostSql(query, t.cancellationToken)

            testRequestHeaders t HttpMethod.Post $"http://localhost:%d{PORT}/sql"
            do! testRequestTextContent t query

            testResponseHeaders response expectedStatus
            testResponseMultipleJson response "OK" "3.4s" (TimeSpan.FromSeconds(3.4)) expectedJsons
        }

    // Testing GetKeyTable

    [<Fact>]
    let ``GetKeyTable with error response`` () =
        task {
            let expectedStatus = HttpStatusCode.BadRequest

            let t = prepareTest expectedStatus sampleErrorInfoJson

            use _ = t.disposable

            let table = "people"

            let! response = t.endpoints.GetKeyTable(table, t.cancellationToken)

            testRequestHeaders t HttpMethod.Get $"http://localhost:%d{PORT}/key/{table}"
            testRequestNoContent t

            testResponseHeaders response expectedStatus
            testResponseError response sampleErrorInfo
        }

    [<Fact>]
    let ``GetKeyTable with missing result`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let t =
                """[{
                    "time": "3.4s",
                    "status": "OK"
                }]"""
                |> prepareTest expectedStatus
            use _ = t.disposable

            let table = "people"

            let! response = t.endpoints.GetKeyTable(table, t.cancellationToken)

            testRequestHeaders t HttpMethod.Get $"http://localhost:%d{PORT}/key/{table}"
            testRequestNoContent t

            testResponseHeaders response expectedStatus
            testResponseStatementError response "OK" "3.4s" (TimeSpan.FromSeconds(3.4)) EXPECTED_RESULT_OR_DETAIL
        }

    [<Fact>]
    let ``GetKeyTable with array response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let expectedJson =
                """[
                    {
                        "age": 19,
                        "firstName": "John",
                        "id": "people:dr3mc523txrii4cfuczh",
                        "lastName": "Doe"
                    },
                    {
                        "age": 17,
                        "firstName": "Jane",
                        "id": "people:zi1e78q4onfh6wypk5bz",
                        "lastName": "Doe"
                    }
                ]"""

            let t =
                sampleJsonResponse "OK" "151.3µs" expectedJson
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"

            let! response = t.endpoints.GetKeyTable(table, t.cancellationToken)

            testRequestHeaders t HttpMethod.Get $"http://localhost:%d{PORT}/key/{table}"
            testRequestNoContent t

            testResponseHeaders response expectedStatus
            testResponseJson response "OK" "151.3µs" (TimeSpan.FromMilliseconds(0.1513)) expectedJson
        }

    [<Fact>]
    let ``GetKeyTable with statement error`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let expectedDetail = "Some error details"

            let t =
                sprintf
                    """[
                        {
                            "status": "%s",
                            "time": "%s",
                            "detail": "%s"
                        }
                    ]""" "OK" "151.3µs" expectedDetail
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"

            let! response = t.endpoints.GetKeyTable(table, t.cancellationToken)

            testRequestHeaders t HttpMethod.Get $"http://localhost:%d{PORT}/key/{table}"
            testRequestNoContent t

            testResponseHeaders response expectedStatus
            testResponseStatementError response "OK" "151.3µs" (TimeSpan.FromMilliseconds(0.1513)) expectedDetail
        }

    // Testing PostKeyTable

    [<Fact>]
    let ``PostKeyTable with error response`` () =
        task {
            let expectedStatus = HttpStatusCode.BadRequest

            let t = prepareTest expectedStatus sampleErrorInfoJson

            use _ = t.disposable

            let table = "people"

            let record = JsonNode.Parse """[{
                "firstName": "John",
                "lastName": "Doe",
                "age": 19
            }]"""

            let! response = t.endpoints.PostKeyTable(table, record, t.cancellationToken)

            testRequestHeaders t HttpMethod.Post $"http://localhost:%d{PORT}/key/{table}"
            do! testRequestJsonContent t record

            testResponseHeaders response expectedStatus
            testResponseError response sampleErrorInfo
        }

    [<Fact>]
    let ``PostKeyTable with single response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let expectedJson =
                """[{
                    "age": 19,
                    "firstName": "John",
                    "id": "people:dr3mc523txrii4cfuczh",
                    "lastName": "Doe"
                }]"""

            let t =
                sampleJsonResponse "OK" "16.2ms" expectedJson
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"

            let record = JsonNode.Parse """{
                "firstName": "John",
                "lastName": "Doe",
                "age": 19
            }"""

            let! response = t.endpoints.PostKeyTable(table, record, t.cancellationToken)

            testRequestHeaders t HttpMethod.Post $"http://localhost:%d{PORT}/key/{table}"
            do! testRequestJsonContent t record

            testResponseHeaders response expectedStatus
            testResponseJson response "OK" "16.2ms" (TimeSpan.FromMilliseconds(16.2)) expectedJson
        }

    // Testing DeleteKeyTable

    [<Fact>]
    let ``DeleteKeyTable with error response`` () =
        task {
            let expectedStatus = HttpStatusCode.BadRequest

            let t = prepareTest expectedStatus sampleErrorInfoJson

            use _ = t.disposable

            let table = "people"

            let! response = t.endpoints.DeleteKeyTable(table, t.cancellationToken)

            testRequestHeaders t HttpMethod.Delete $"http://localhost:%d{PORT}/key/{table}"
            testRequestNoContent t

            testResponseHeaders response expectedStatus
            testResponseError response sampleErrorInfo
        }

    [<Fact>]
    let ``DeleteKeyTable with empty response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let expectedJson = "[]"

            let t =
                sampleJsonResponse "OK" "16.2ms" expectedJson
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"

            let! response = t.endpoints.DeleteKeyTable(table, t.cancellationToken)

            testRequestHeaders t HttpMethod.Delete $"http://localhost:%d{PORT}/key/{table}"

            testResponseHeaders response expectedStatus
            testResponseJson response "OK" "16.2ms" (TimeSpan.FromMilliseconds(16.2)) expectedJson
        }

    // Testing GetKeyTableId

    [<Fact>]
    let ``GetKeyTableId with error response`` () =
        task {
            let expectedStatus = HttpStatusCode.BadRequest

            let t = prepareTest expectedStatus sampleErrorInfoJson

            use _ = t.disposable

            let table = "people"
            let id = "dr3mc523txrii4cfuczh"

            let! response = t.endpoints.GetKeyTableId(table, id, t.cancellationToken)

            testRequestHeaders t HttpMethod.Get $"http://localhost:%d{PORT}/key/{table}/{id}"
            testRequestNoContent t

            testResponseHeaders response expectedStatus
            testResponseError response sampleErrorInfo
        }

    [<Fact>]
    let ``GetKeyTableId with single response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let expectedJson =
                """[{
                    "age": 19,
                    "firstName": "John",
                    "id": "people:dr3mc523txrii4cfuczh",
                    "lastName": "Doe"
                }]"""

            let t =
                sampleJsonResponse "OK" "16.2ms" expectedJson
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"
            let id = "dr3mc523txrii4cfuczh"

            let! response = t.endpoints.GetKeyTableId(table, id, t.cancellationToken)

            testRequestHeaders t HttpMethod.Get $"http://localhost:%d{PORT}/key/{table}/{id}"
            testRequestNoContent t

            testResponseHeaders response expectedStatus
            testResponseJson response "OK" "16.2ms" (TimeSpan.FromMilliseconds(16.2)) expectedJson
        }

    // Testing PostKeyTableId

    [<Fact>]
    let ``PostKeyTableId with error response`` () =
        task {
            let expectedStatus = HttpStatusCode.BadRequest

            let t = prepareTest expectedStatus sampleErrorInfoJson

            use _ = t.disposable

            let table = "people"
            let id = "dr3mc523txrii4cfuczh"

            let record = JsonNode.Parse """{
                "firstName": "John",
                "lastName": "Doe",
                "age": 19
            }"""

            let! response = t.endpoints.PostKeyTableId(table, id, record, t.cancellationToken)

            testRequestHeaders t HttpMethod.Post $"http://localhost:%d{PORT}/key/{table}/{id}"
            do! testRequestJsonContent t record

            testResponseHeaders response expectedStatus
            testResponseError response sampleErrorInfo
        }

    [<Fact>]
    let ``PostKeyTableId with single response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let expectedJson =
                """[{
                    "age": 19,
                    "firstName": "John",
                    "id": "people:dr3mc523txrii4cfuczh",
                    "lastName": "Doe"
                }]"""

            let t =
                sampleJsonResponse "OK" "16.2ms" expectedJson
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"
            let id = "dr3mc523txrii4cfuczh"

            let record = JsonNode.Parse """{
                "firstName": "John",
                "lastName": "Doe",
                "age": 19
            }"""

            let! response = t.endpoints.PostKeyTableId(table, id, record, t.cancellationToken)

            testRequestHeaders t HttpMethod.Post $"http://localhost:%d{PORT}/key/{table}/{id}"
            do! testRequestJsonContent t record

            testResponseHeaders response expectedStatus
            testResponseJson response "OK" "16.2ms" (TimeSpan.FromMilliseconds(16.2)) expectedJson
        }

    // Testing PutKeyTableId

    [<Fact>]
    let ``PutKeyTableId with error response`` () =
        task {
            let expectedStatus = HttpStatusCode.BadRequest

            let t = prepareTest expectedStatus sampleErrorInfoJson

            use _ = t.disposable

            let table = "people"
            let id = "dr3mc523txrii4cfuczh"

            let record = JsonNode.Parse """{
                "firstName": "John",
                "lastName": "Doe",
                "age": 19
            }"""

            let! response = t.endpoints.PutKeyTableId(table, id, record, t.cancellationToken)

            testRequestHeaders t HttpMethod.Put $"http://localhost:%d{PORT}/key/{table}/{id}"
            do! testRequestJsonContent t record

            testResponseHeaders response expectedStatus
            testResponseError response sampleErrorInfo
        }

    [<Fact>]
    let ``PutKeyTableId with single response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let expectedJson =
                """[{
                    "age": 19,
                    "firstName": "John",
                    "id": "people:dr3mc523txrii4cfuczh",
                    "lastName": "Doe"
                }]"""

            let t =
                sampleJsonResponse "OK" "16.2ms" expectedJson
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"
            let id = "dr3mc523txrii4cfuczh"

            let record = JsonNode.Parse """{
                "firstName": "John",
                "lastName": "Doe",
                "age": 19
            }"""

            let! response = t.endpoints.PutKeyTableId(table, id, record, t.cancellationToken)

            testRequestHeaders t HttpMethod.Put $"http://localhost:%d{PORT}/key/{table}/{id}"
            do! testRequestJsonContent t record

            testResponseHeaders response expectedStatus
            testResponseJson response "OK" "16.2ms" (TimeSpan.FromMilliseconds(16.2)) expectedJson
        }

    // Testing PatchKeyTableId

    [<Fact>]
    let ``PatchKeyTableId with error response`` () =
        task {
            let expectedStatus = HttpStatusCode.BadRequest

            let t = prepareTest expectedStatus sampleErrorInfoJson

            use _ = t.disposable

            let table = "people"
            let id = "dr3mc523txrii4cfuczh"

            let record = JsonNode.Parse """{
                "firstName": "John",
                "lastName": "Doe",
                "age": 19
            }"""

            let! response = t.endpoints.PatchKeyTableId(table, id, record, t.cancellationToken)

            testRequestHeaders t HttpMethod.Patch $"http://localhost:%d{PORT}/key/{table}/{id}"
            do! testRequestJsonContent t record

            testResponseHeaders response expectedStatus
            testResponseError response sampleErrorInfo
        }

    [<Fact>]
    let ``PatchKeyTableId with single response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let expectedJson =
                """[{
                    "age": 19,
                    "firstName": "John",
                    "id": "people:dr3mc523txrii4cfuczh",
                    "lastName": "Doe"
                }]"""

            let t =
                sampleJsonResponse "OK" "16.2ms" expectedJson
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"
            let id = "dr3mc523txrii4cfuczh"

            let record = JsonNode.Parse """{
                "firstName": "Johnny"
            }"""

            let! response = t.endpoints.PatchKeyTableId(table, id, record, t.cancellationToken)

            testRequestHeaders t HttpMethod.Patch $"http://localhost:%d{PORT}/key/{table}/{id}"
            do! testRequestJsonContent t record

            testResponseHeaders response expectedStatus
            testResponseJson response "OK" "16.2ms" (TimeSpan.FromMilliseconds(16.2)) expectedJson
        }

    // Testing DeleteKeyTableId

    [<Fact>]
    let ``DeleteKeyTableId with error response`` () =
        task {
            let expectedStatus = HttpStatusCode.BadRequest

            let t = prepareTest expectedStatus sampleErrorInfoJson

            use _ = t.disposable

            let table = "people"
            let id = "dr3mc523txrii4cfuczh"

            let! response = t.endpoints.DeleteKeyTableId(table, id, t.cancellationToken)

            testRequestHeaders t HttpMethod.Delete $"http://localhost:%d{PORT}/key/{table}/{id}"

            testResponseHeaders response expectedStatus
            testResponseError response sampleErrorInfo
        }

    [<Fact>]
    let ``DeleteKeyTableId with empty response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let expectedJson = "[]"

            let t =
                sampleJsonResponse "OK" "16.2ms" expectedJson
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"
            let id = "dr3mc523txrii4cfuczh"

            let! response = t.endpoints.DeleteKeyTableId(table, id, t.cancellationToken)

            testRequestHeaders t HttpMethod.Delete $"http://localhost:%d{PORT}/key/{table}/{id}"

            testResponseHeaders response expectedStatus
            testResponseJson response "OK" "16.2ms" (TimeSpan.FromMilliseconds(16.2)) expectedJson
        }
