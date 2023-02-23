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
module SurrealTableClientTests =
    type Person =
        { firstName: string
          lastName: string
          id: SurrealId
          age: int }

    type PersonData =
        { firstName: string
          lastName: string
          age: int }

    type PersonFirstNamePatch = { firstName: string }


    let john =
        { firstName = "John"
          lastName = "Doe"
          id = SurrealId.Parse "people:john"
          age = 21 }

    let jane =
        { firstName = "Jane"
          lastName = "Doe"
          id = SurrealId.Parse "people:jane"
          age = 19 }

    type Testing =
        { config: SurrealConfig
          httpClient: HttpClient
          jsonOptions: JsonSerializerOptions
          endpoints: ISurrealTableClient
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

        let endpoints: ISurrealTableClient =
            new SurrealTableClient(config, httpClient, Json.defaultOptions)

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

    let sampleDataResponse status time result =
        let json =
            JsonSerializer.Serialize(result, Json.defaultOptions)

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
            json

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

    let testRequestDataContent t (expectedData: 'data) =
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

            let expectedJson =
                JsonSerializer.SerializeToNode(expectedData, Json.defaultOptions)

            test <@ jsonDiff json expectedJson = [] @>
        }

    let testResponseHeaders (response: RestApiSingleResult<'result>) expectedStatus =
        let expectedHeaders: HeadersInfo =
            { version = DUMMY_VERSION
              server = DUMMY_SERVER
              status = expectedStatus
              date = DUMMY_DATE }

        test <@ response.headers = expectedHeaders @>
        test <@ response.headers.dateTime = ValueSome(DUMMY_DATETIME) @>
        test <@ response.headers.dateTimeOffset = ValueSome(DUMMY_DATETIMEOFFSET) @>

    let testResponseError (response: RestApiSingleResult<'result>) expectedError =
        match response.statement with
        | Ok result -> Assert.Fail $"Expected error, got {result}"
        | Error error -> test <@ error = ResponseError expectedError @>

    let testResponseResult
        (response: RestApiSingleResult<'result>)
        expectedStatus
        expectedTime
        expectedTimeSpan
        (expectedResult: 'result)
        =
        match response.statement with
        | Error error -> Assert.Fail $"Expected success result, got {error}"
        | Ok statement ->
            test <@ statement.time = expectedTime @>
            test <@ statement.timeSpan = ValueSome(expectedTimeSpan) @>
            test <@ statement.status = expectedStatus @>

            match statement.response with
            | Error error -> Assert.Fail $"Expected success result, got {error}"
            | Ok result -> test <@ result = expectedResult @>

    let testResult (response: RequestResult<'result>) (expectedResult: 'result) =
        match response with
        | Error error -> Assert.Fail $"Expected success result, got {error}"
        | Ok result -> test <@ result = expectedResult @>

    let testResponseStatementError
        (response: RestApiSingleResult<'result>)
        expectedStatus
        expectedTime
        expectedTimeSpan
        (expectedError: RequestError)
        =
        match response.statement with
        | Error error -> Assert.Fail $"Expected success result, got {error}"
        | Ok statement ->
            test <@ statement.time = expectedTime @>
            test <@ statement.timeSpan = ValueSome(expectedTimeSpan) @>
            test <@ statement.status = expectedStatus @>

            match statement.response with
            | Error error -> test <@ error = expectedError @>
            | Ok result -> Assert.Fail $"Expected error, got {result}"

    [<Fact>]
    let ``ListResponseAsync with error response`` () =
        task {
            let expectedStatus = HttpStatusCode.BadRequest

            let t =
                prepareTest expectedStatus sampleErrorInfoJson

            use _ = t.disposable

            let table = "people"

            let! response = t.endpoints.ListResponseAsync<Person>(table, t.cancellationToken)

            testRequestHeaders t HttpMethod.Get $"http://localhost:%d{PORT}/key/{table}"
            testRequestNoContent t

            testResponseHeaders response expectedStatus
            testResponseError response sampleErrorInfo
        }

    [<Fact>]
    let ``ListResponseAsync with missing result`` () =
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

            let! response = t.endpoints.ListResponseAsync<Person>(table, t.cancellationToken)

            testRequestHeaders t HttpMethod.Get $"http://localhost:%d{PORT}/key/{table}"
            testRequestNoContent t

            testResponseHeaders response expectedStatus

            testResponseStatementError
                response
                "OK"
                "3.4s"
                (TimeSpan.FromSeconds(3.4))
                (StatementError EXPECTED_RESULT_OR_DETAIL)
        }

    [<Fact>]
    let ``ListResponseAsync with array response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let expectedResult = [| john; jane |]

            let t =
                sampleDataResponse "OK" "151.3µs" expectedResult
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"

            let! response = t.endpoints.ListResponseAsync<Person>(table, t.cancellationToken)

            testRequestHeaders t HttpMethod.Get $"http://localhost:%d{PORT}/key/{table}"
            testRequestNoContent t

            testResponseHeaders response expectedStatus
            testResponseResult response "OK" "151.3µs" (TimeSpan.FromMilliseconds(0.1513)) expectedResult
        }

    [<Fact>]
    let ``ListAsync with array response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let expectedResult = [| john; jane |]

            let t =
                sampleDataResponse "OK" "151.3µs" expectedResult
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"

            let! response = t.endpoints.ListAsync<Person>(table, t.cancellationToken)

            testRequestHeaders t HttpMethod.Get $"http://localhost:%d{PORT}/key/{table}"
            testRequestNoContent t

            testResult response expectedResult
        }

    // Testing CreateAsync[Response]

    [<Fact>]
    let ``CreatePartialResponseAsync with error response`` () =
        task {
            let expectedStatus = HttpStatusCode.BadRequest

            let t =
                prepareTest expectedStatus sampleErrorInfoJson

            use _ = t.disposable

            let table = "people"

            let record: PersonData =
                { firstName = john.firstName
                  lastName = john.lastName
                  age = john.age }

            let! response = t.endpoints.CreatePartialResponseAsync<_, Person>(table, record, t.cancellationToken)

            testRequestHeaders t HttpMethod.Post $"http://localhost:%d{PORT}/key/{table}"
            do! testRequestDataContent t record

            testResponseHeaders response expectedStatus
            testResponseError response sampleErrorInfo
        }

    [<Fact>]
    let ``CreatePartialResponseAsync with record response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let expectedResult = john

            let t =
                sampleDataResponse "OK" "151.3µs" [| expectedResult |]
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"

            let record: PersonData =
                { firstName = john.firstName
                  lastName = john.lastName
                  age = john.age }

            let! response = t.endpoints.CreatePartialResponseAsync<_, Person>(table, record, t.cancellationToken)

            testRequestHeaders t HttpMethod.Post $"http://localhost:%d{PORT}/key/{table}"
            do! testRequestDataContent t record

            testResponseHeaders response expectedStatus
            testResponseResult response "OK" "151.3µs" (TimeSpan.FromMilliseconds(0.1513)) expectedResult
        }

    [<Fact>]
    let ``CreatePartialAsync with record response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let expectedResult = john

            let t =
                sampleDataResponse "OK" "151.3µs" [| expectedResult |]
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"

            let record: PersonData =
                { firstName = john.firstName
                  lastName = john.lastName
                  age = john.age }

            let! response = t.endpoints.CreatePartialAsync<_, Person>(table, record, t.cancellationToken)

            testRequestHeaders t HttpMethod.Post $"http://localhost:%d{PORT}/key/{table}"
            do! testRequestDataContent t record

            testResult response expectedResult
        }

    [<Fact>]
    let ``CreateResponseAsync with error response`` () =
        task {
            let expectedStatus = HttpStatusCode.BadRequest

            let t =
                prepareTest expectedStatus sampleErrorInfoJson

            use _ = t.disposable

            let table = "people"

            let record = john

            let! response = t.endpoints.CreateResponseAsync(table, record, t.cancellationToken)

            testRequestHeaders t HttpMethod.Post $"http://localhost:%d{PORT}/key/{table}"
            do! testRequestDataContent t record

            testResponseHeaders response expectedStatus
            testResponseError response sampleErrorInfo
        }

    [<Fact>]
    let ``CreateResponseAsync with record response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let expectedResult = john

            let t =
                sampleDataResponse "OK" "151.3µs" [| expectedResult |]
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"

            let record = john

            let! response = t.endpoints.CreateResponseAsync(table, record, t.cancellationToken)

            testRequestHeaders t HttpMethod.Post $"http://localhost:%d{PORT}/key/{table}"
            do! testRequestDataContent t record

            testResponseHeaders response expectedStatus
            testResponseResult response "OK" "151.3µs" (TimeSpan.FromMilliseconds(0.1513)) expectedResult
        }

    [<Fact>]
    let ``CreateAsync with record response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let expectedResult = john

            let t =
                sampleDataResponse "OK" "151.3µs" [| expectedResult |]
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"

            let record = john

            let! response = t.endpoints.CreateAsync(table, record, t.cancellationToken)

            testRequestHeaders t HttpMethod.Post $"http://localhost:%d{PORT}/key/{table}"
            do! testRequestDataContent t record

            testResult response expectedResult
        }

    // Testing DeleteAllAsync[Response]

    [<Fact>]
    let ``DeleteAllResponseAsync with error response`` () =
        task {
            let expectedStatus = HttpStatusCode.BadRequest

            let t =
                prepareTest expectedStatus sampleErrorInfoJson

            use _ = t.disposable

            let table = "people"

            let! response = t.endpoints.DeleteAllResponseAsync(table, t.cancellationToken)

            testRequestHeaders t HttpMethod.Delete $"http://localhost:%d{PORT}/key/{table}"
            testRequestNoContent t

            testResponseHeaders response expectedStatus
            testResponseError response sampleErrorInfo
        }

    [<Fact>]
    let ``DeleteAllResponseAsync with no response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let t =
                sampleDataResponse "OK" "151.3µs" [||]
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"

            let! response = t.endpoints.DeleteAllResponseAsync(table, t.cancellationToken)

            testRequestHeaders t HttpMethod.Delete $"http://localhost:%d{PORT}/key/{table}"
            testRequestNoContent t

            testResponseHeaders response expectedStatus
            testResponseResult response "OK" "151.3µs" (TimeSpan.FromMilliseconds(0.1513)) ()
        }

    [<Fact>]
    let ``DeleteAllAsync with no response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let t =
                sampleDataResponse "OK" "151.3µs" [||]
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"

            let! response = t.endpoints.DeleteAllAsync(table, t.cancellationToken)

            testRequestHeaders t HttpMethod.Delete $"http://localhost:%d{PORT}/key/{table}"
            testRequestNoContent t

            testResult response ()
        }

    // Testing FindAsync[Response]

    [<Fact>]
    let ``FindResponseAsync with error response`` () =
        task {
            let expectedStatus = HttpStatusCode.BadRequest

            let t =
                prepareTest expectedStatus sampleErrorInfoJson

            use _ = t.disposable

            let table = "people"
            let id = john.id.id

            let! response = t.endpoints.FindResponseAsync<Person>(table, id, t.cancellationToken)

            testRequestHeaders t HttpMethod.Get $"http://localhost:%d{PORT}/key/{table}/{id}"
            testRequestNoContent t

            testResponseHeaders response expectedStatus
            testResponseError response sampleErrorInfo
        }

    [<Fact>]
    let ``FindResponseAsync with record response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let expectedResult = john

            let t =
                sampleDataResponse "OK" "151.3µs" [| expectedResult |]
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"
            let id = john.id.id

            let! response = t.endpoints.FindResponseAsync<Person>(table, id, t.cancellationToken)

            testRequestHeaders t HttpMethod.Get $"http://localhost:%d{PORT}/key/{table}/{id}"
            testRequestNoContent t

            testResponseHeaders response expectedStatus
            testResponseResult response "OK" "151.3µs" (TimeSpan.FromMilliseconds(0.1513)) (ValueSome expectedResult)
        }

    [<Fact>]
    let ``FindResponseAsync with empty response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let t =
                sampleDataResponse "OK" "151.3µs" [||]
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"
            let id = john.id.id

            let! response = t.endpoints.FindResponseAsync<Person>(table, id, t.cancellationToken)

            testRequestHeaders t HttpMethod.Get $"http://localhost:%d{PORT}/key/{table}/{id}"
            testRequestNoContent t

            testResponseHeaders response expectedStatus
            testResponseResult response "OK" "151.3µs" (TimeSpan.FromMilliseconds(0.1513)) ValueNone
        }

    [<Fact>]
    let ``FindResponseAsync with multiple records response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let t =
                sampleDataResponse "OK" "151.3µs" [| john; jane |]
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"
            let id = john.id.id

            let! response = t.endpoints.FindResponseAsync<Person>(table, id, t.cancellationToken)

            testRequestHeaders t HttpMethod.Get $"http://localhost:%d{PORT}/key/{table}/{id}"
            testRequestNoContent t

            testResponseHeaders response expectedStatus

            testResponseStatementError
                response
                "OK"
                "151.3µs"
                (TimeSpan.FromMilliseconds(0.1513))
                (ProtocolError ExpectedOptionalItem)
        }

    [<Fact>]
    let ``FindAsync with record response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let expectedResult = john

            let t =
                sampleDataResponse "OK" "151.3µs" [| expectedResult |]
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"
            let id = john.id.id

            let! response = t.endpoints.FindAsync<Person>(table, id, t.cancellationToken)

            testRequestHeaders t HttpMethod.Get $"http://localhost:%d{PORT}/key/{table}/{id}"
            testRequestNoContent t

            testResult response (ValueSome expectedResult)
        }

    [<Fact>]
    let ``FindAsync with empty response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let t =
                sampleDataResponse "OK" "151.3µs" [||]
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"
            let id = john.id.id

            let! response = t.endpoints.FindAsync<Person>(table, id, t.cancellationToken)

            testRequestHeaders t HttpMethod.Get $"http://localhost:%d{PORT}/key/{table}/{id}"
            testRequestNoContent t

            testResult response ValueNone
        }

    // Testing InsertAsync[Response]

    [<Fact>]
    let ``InsertPartialResponseAsync with error response`` () =
        task {
            let expectedStatus = HttpStatusCode.BadRequest

            let t =
                prepareTest expectedStatus sampleErrorInfoJson

            use _ = t.disposable

            let table = "people"

            let record: PersonData =
                { firstName = john.firstName
                  lastName = john.lastName
                  age = john.age }

            let id = john.id.id

            let! response = t.endpoints.InsertPartialResponseAsync<_, Person>(table, id, record, t.cancellationToken)

            testRequestHeaders t HttpMethod.Post $"http://localhost:%d{PORT}/key/{table}/{id}"
            do! testRequestDataContent t record

            testResponseHeaders response expectedStatus
            testResponseError response sampleErrorInfo
        }

    [<Fact>]
    let ``InsertPartialResponseAsync with record response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let expectedResult = john

            let t =
                sampleDataResponse "OK" "151.3µs" [| expectedResult |]
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"

            let record: PersonData =
                { firstName = john.firstName
                  lastName = john.lastName
                  age = john.age }

            let id = john.id.id

            let! response = t.endpoints.InsertPartialResponseAsync<_, Person>(table, id, record, t.cancellationToken)

            testRequestHeaders t HttpMethod.Post $"http://localhost:%d{PORT}/key/{table}/{id}"
            do! testRequestDataContent t record

            testResponseHeaders response expectedStatus
            testResponseResult response "OK" "151.3µs" (TimeSpan.FromMilliseconds(0.1513)) expectedResult
        }

    [<Fact>]
    let ``InsertPartialResponseAsync with empty response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let t =
                sampleDataResponse "OK" "151.3µs" [||]
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"

            let record: PersonData =
                { firstName = john.firstName
                  lastName = john.lastName
                  age = john.age }

            let id = john.id.id

            let! response = t.endpoints.InsertPartialResponseAsync<_, Person>(table, id, record, t.cancellationToken)

            testRequestHeaders t HttpMethod.Post $"http://localhost:%d{PORT}/key/{table}/{id}"
            do! testRequestDataContent t record

            testResponseHeaders response expectedStatus

            testResponseStatementError
                response
                "OK"
                "151.3µs"
                (TimeSpan.FromMilliseconds(0.1513))
                (ProtocolError ExpectedSingleItem)
        }

    [<Fact>]
    let ``InsertPartialAsync with record response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let expectedResult = john

            let t =
                sampleDataResponse "OK" "151.3µs" [| expectedResult |]
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"

            let record: PersonData =
                { firstName = john.firstName
                  lastName = john.lastName
                  age = john.age }

            let id = john.id.id

            let! response = t.endpoints.InsertPartialAsync<_, Person>(table, id, record, t.cancellationToken)

            testRequestHeaders t HttpMethod.Post $"http://localhost:%d{PORT}/key/{table}/{id}"
            do! testRequestDataContent t record

            testResult response expectedResult
        }

    [<Fact>]
    let ``InsertResponseAsync with error response`` () =
        task {
            let expectedStatus = HttpStatusCode.BadRequest

            let t =
                prepareTest expectedStatus sampleErrorInfoJson

            use _ = t.disposable

            let table = "people"
            let record = john
            let id = john.id.id

            let! response = t.endpoints.InsertResponseAsync<Person>(table, id, record, t.cancellationToken)

            testRequestHeaders t HttpMethod.Post $"http://localhost:%d{PORT}/key/{table}/{id}"
            do! testRequestDataContent t record

            testResponseHeaders response expectedStatus
            testResponseError response sampleErrorInfo
        }

    [<Fact>]
    let ``InsertResponseAsync with record response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let expectedResult = john

            let t =
                sampleDataResponse "OK" "151.3µs" [| expectedResult |]
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"
            let record = john
            let id = john.id.id

            let! response = t.endpoints.InsertResponseAsync<Person>(table, id, record, t.cancellationToken)

            testRequestHeaders t HttpMethod.Post $"http://localhost:%d{PORT}/key/{table}/{id}"
            do! testRequestDataContent t record

            testResponseHeaders response expectedStatus
            testResponseResult response "OK" "151.3µs" (TimeSpan.FromMilliseconds(0.1513)) expectedResult
        }

    [<Fact>]
    let ``InsertResponseAsync with empty response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let t =
                sampleDataResponse "OK" "151.3µs" [||]
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"
            let record = john
            let id = john.id.id

            let! response = t.endpoints.InsertResponseAsync<Person>(table, id, record, t.cancellationToken)

            testRequestHeaders t HttpMethod.Post $"http://localhost:%d{PORT}/key/{table}/{id}"
            do! testRequestDataContent t record

            testResponseHeaders response expectedStatus

            testResponseStatementError
                response
                "OK"
                "151.3µs"
                (TimeSpan.FromMilliseconds(0.1513))
                (ProtocolError ExpectedSingleItem)
        }

    [<Fact>]
    let ``InsertAsync with record response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let expectedResult = john

            let t =
                sampleDataResponse "OK" "151.3µs" [| expectedResult |]
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"
            let record = john
            let id = john.id.id

            let! response = t.endpoints.InsertAsync<Person>(table, id, record, t.cancellationToken)

            testRequestHeaders t HttpMethod.Post $"http://localhost:%d{PORT}/key/{table}/{id}"
            do! testRequestDataContent t record

            testResult response expectedResult
        }

    // Testing ReplaceAsync[Response]

    [<Fact>]
    let ``ReplacePartialResponseAsync with error response`` () =
        task {
            let expectedStatus = HttpStatusCode.BadRequest

            let t =
                prepareTest expectedStatus sampleErrorInfoJson

            use _ = t.disposable

            let table = "people"

            let record: PersonData =
                { firstName = john.firstName
                  lastName = john.lastName
                  age = john.age }

            let id = john.id.id

            let! response = t.endpoints.ReplacePartialResponseAsync<_, Person>(table, id, record, t.cancellationToken)

            testRequestHeaders t HttpMethod.Put $"http://localhost:%d{PORT}/key/{table}/{id}"
            do! testRequestDataContent t record

            testResponseHeaders response expectedStatus
            testResponseError response sampleErrorInfo
        }

    [<Fact>]
    let ``ReplacePartialResponseAsync with record response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let expectedResult = john

            let t =
                sampleDataResponse "OK" "151.3µs" [| expectedResult |]
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"

            let record: PersonData =
                { firstName = john.firstName
                  lastName = john.lastName
                  age = john.age }

            let id = john.id.id

            let! response = t.endpoints.ReplacePartialResponseAsync<_, Person>(table, id, record, t.cancellationToken)

            testRequestHeaders t HttpMethod.Put $"http://localhost:%d{PORT}/key/{table}/{id}"
            do! testRequestDataContent t record

            testResponseHeaders response expectedStatus
            testResponseResult response "OK" "151.3µs" (TimeSpan.FromMilliseconds(0.1513)) expectedResult
        }

    [<Fact>]
    let ``ReplacePartialResponseAsync with empty response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let t =
                sampleDataResponse "OK" "151.3µs" [||]
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"

            let record: PersonData =
                { firstName = john.firstName
                  lastName = john.lastName
                  age = john.age }

            let id = john.id.id

            let! response = t.endpoints.ReplacePartialResponseAsync<_, Person>(table, id, record, t.cancellationToken)

            testRequestHeaders t HttpMethod.Put $"http://localhost:%d{PORT}/key/{table}/{id}"
            do! testRequestDataContent t record

            testResponseHeaders response expectedStatus

            testResponseStatementError
                response
                "OK"
                "151.3µs"
                (TimeSpan.FromMilliseconds(0.1513))
                (ProtocolError ExpectedSingleItem)
        }

    [<Fact>]
    let ``ReplacePartialAsync with record response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let expectedResult = john

            let t =
                sampleDataResponse "OK" "151.3µs" [| expectedResult |]
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"

            let record: PersonData =
                { firstName = john.firstName
                  lastName = john.lastName
                  age = john.age }

            let id = john.id.id

            let! response = t.endpoints.ReplacePartialAsync<_, Person>(table, id, record, t.cancellationToken)

            testRequestHeaders t HttpMethod.Put $"http://localhost:%d{PORT}/key/{table}/{id}"
            do! testRequestDataContent t record

            testResult response expectedResult
        }

    [<Fact>]
    let ``ReplaceResponseAsync with error response`` () =
        task {
            let expectedStatus = HttpStatusCode.BadRequest

            let t =
                prepareTest expectedStatus sampleErrorInfoJson

            use _ = t.disposable

            let table = "people"
            let record = john
            let id = john.id.id

            let! response = t.endpoints.ReplaceResponseAsync<Person>(table, id, record, t.cancellationToken)

            testRequestHeaders t HttpMethod.Put $"http://localhost:%d{PORT}/key/{table}/{id}"
            do! testRequestDataContent t record

            testResponseHeaders response expectedStatus
            testResponseError response sampleErrorInfo
        }

    [<Fact>]
    let ``ReplaceResponseAsync with record response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let expectedResult = john

            let t =
                sampleDataResponse "OK" "151.3µs" [| expectedResult |]
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"
            let record = john
            let id = john.id.id

            let! response = t.endpoints.ReplaceResponseAsync<Person>(table, id, record, t.cancellationToken)

            testRequestHeaders t HttpMethod.Put $"http://localhost:%d{PORT}/key/{table}/{id}"
            do! testRequestDataContent t record

            testResponseHeaders response expectedStatus
            testResponseResult response "OK" "151.3µs" (TimeSpan.FromMilliseconds(0.1513)) expectedResult
        }

    [<Fact>]
    let ``ReplaceResponseAsync with empty response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let t =
                sampleDataResponse "OK" "151.3µs" [||]
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"
            let record = john
            let id = john.id.id

            let! response = t.endpoints.ReplaceResponseAsync<Person>(table, id, record, t.cancellationToken)

            testRequestHeaders t HttpMethod.Put $"http://localhost:%d{PORT}/key/{table}/{id}"
            do! testRequestDataContent t record

            testResponseHeaders response expectedStatus

            testResponseStatementError
                response
                "OK"
                "151.3µs"
                (TimeSpan.FromMilliseconds(0.1513))
                (ProtocolError ExpectedSingleItem)
        }

    [<Fact>]
    let ``ReplaceAsync with record response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let expectedResult = john

            let t =
                sampleDataResponse "OK" "151.3µs" [| expectedResult |]
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"
            let record = john
            let id = john.id.id

            let! response = t.endpoints.ReplaceAsync<Person>(table, id, record, t.cancellationToken)

            testRequestHeaders t HttpMethod.Put $"http://localhost:%d{PORT}/key/{table}/{id}"
            do! testRequestDataContent t record

            testResult response expectedResult
        }

    // Testing PatchKeyTableIdResponse

    [<Fact>]
    let ``ModifyPartialResponseAsync with error response`` () =
        task {
            let expectedStatus = HttpStatusCode.BadRequest

            let t =
                prepareTest expectedStatus sampleErrorInfoJson

            use _ = t.disposable

            let table = "people"

            let record: PersonFirstNamePatch =
                { firstName = john.firstName }

            let id = john.id.id

            let! response = t.endpoints.ModifyPartialResponseAsync<_, Person>(table, id, record, t.cancellationToken)

            testRequestHeaders t HttpMethod.Patch $"http://localhost:%d{PORT}/key/{table}/{id}"
            do! testRequestDataContent t record

            testResponseHeaders response expectedStatus
            testResponseError response sampleErrorInfo
        }

    [<Fact>]
    let ``ModifyPartialResponseAsync with record response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let expectedResult = john

            let t =
                sampleDataResponse "OK" "151.3µs" [| expectedResult |]
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"

            let record: PersonFirstNamePatch =
                { firstName = john.firstName }

            let id = john.id.id

            let! response = t.endpoints.ModifyPartialResponseAsync<_, Person>(table, id, record, t.cancellationToken)

            testRequestHeaders t HttpMethod.Patch $"http://localhost:%d{PORT}/key/{table}/{id}"
            do! testRequestDataContent t record

            testResponseHeaders response expectedStatus
            testResponseResult response "OK" "151.3µs" (TimeSpan.FromMilliseconds(0.1513)) expectedResult
        }

    [<Fact>]
    let ``ModifyPartialResponseAsync with empty response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let t =
                sampleDataResponse "OK" "151.3µs" [||]
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"

            let record: PersonFirstNamePatch =
                { firstName = john.firstName }

            let id = john.id.id

            let! response = t.endpoints.ModifyPartialResponseAsync<_, Person>(table, id, record, t.cancellationToken)

            testRequestHeaders t HttpMethod.Patch $"http://localhost:%d{PORT}/key/{table}/{id}"
            do! testRequestDataContent t record

            testResponseHeaders response expectedStatus

            testResponseStatementError
                response
                "OK"
                "151.3µs"
                (TimeSpan.FromMilliseconds(0.1513))
                (ProtocolError ExpectedSingleItem)
        }

    [<Fact>]
    let ``ModifyPartialAsync with record response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let expectedResult = john

            let t =
                sampleDataResponse "OK" "151.3µs" [| expectedResult |]
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"

            let record: PersonFirstNamePatch =
                { firstName = john.firstName }

            let id = john.id.id

            let! response = t.endpoints.ModifyPartialAsync<_, Person>(table, id, record, t.cancellationToken)

            testRequestHeaders t HttpMethod.Patch $"http://localhost:%d{PORT}/key/{table}/{id}"
            do! testRequestDataContent t record

            testResult response expectedResult
        }

    // Testing DeleteAsync[Response]

    [<Fact>]
    let ``DeleteResponseAsync with error response`` () =
        task {
            let expectedStatus = HttpStatusCode.BadRequest

            let t =
                prepareTest expectedStatus sampleErrorInfoJson

            use _ = t.disposable

            let table = "people"
            let id = john.id.id

            let! response = t.endpoints.DeleteResponseAsync(table, id, t.cancellationToken)

            testRequestHeaders t HttpMethod.Delete $"http://localhost:%d{PORT}/key/{table}/{id}"

            testResponseHeaders response expectedStatus
            testResponseError response sampleErrorInfo
        }

    [<Fact>]
    let ``DeleteResponseAsync with empty response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let t =
                sampleDataResponse "OK" "151.3µs" [||]
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"
            let id = john.id.id

            let! response = t.endpoints.DeleteResponseAsync(table, id, t.cancellationToken)

            testRequestHeaders t HttpMethod.Delete $"http://localhost:%d{PORT}/key/{table}/{id}"

            testResponseHeaders response expectedStatus
            testResponseResult response "OK" "151.3µs" (TimeSpan.FromMilliseconds(0.1513)) ()
        }

    [<Fact>]
    let ``DeleteResponseAsync with record response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let expectedResult = john

            let t =
                sampleDataResponse "OK" "151.3µs" [| expectedResult |]
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"
            let id = john.id.id

            let! response = t.endpoints.DeleteResponseAsync(table, id, t.cancellationToken)

            testRequestHeaders t HttpMethod.Delete $"http://localhost:%d{PORT}/key/{table}/{id}"

            testResponseHeaders response expectedStatus
            testResponseStatementError
                response
                "OK"
                "151.3µs"
                (TimeSpan.FromMilliseconds(0.1513))
                (ProtocolError ExpectedEmptyArray)
        }

    [<Fact>]
    let ``DeleteAsync with empty response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let t =
                sampleDataResponse "OK" "151.3µs" [||]
                |> prepareTest expectedStatus

            use _ = t.disposable

            let table = "people"
            let id = john.id.id

            let! response = t.endpoints.DeleteAsync(table, id, t.cancellationToken)

            testRequestHeaders t HttpMethod.Delete $"http://localhost:%d{PORT}/key/{table}/{id}"

            testResult response ()
        }
