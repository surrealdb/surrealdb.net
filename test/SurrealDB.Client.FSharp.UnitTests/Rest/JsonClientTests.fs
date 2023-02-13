namespace SurrealDB.Client.FSharp.Rest

open System
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
module JsonClientTests =
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

        let surrealClient: ISurrealRestClient =
            new SurrealRestClient(config, httpClient, jsonOptions)

        let disposable =
            { new IDisposable with
                member this.Dispose() =
                    cancellationTokenSource.Dispose()
                    surrealClient.Dispose() }

        {| jsonOptions = jsonOptions
           surrealClient = surrealClient
           requests = requests
           cancellationTokenSource = cancellationTokenSource
           cancellationToken = cancellationTokenSource.Token
           disposable = disposable |}

    [<Fact>]
    let ``SqlAsync with error response`` () =
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
                """
                INFO FO R KV;
                USE NS testns DB testdb;
                """

            let! response = testing.surrealClient.Json.SqlAsync(query, testing.cancellationToken)

            test <@ testing.requests.Count = 1 @>

            let request = testing.requests.[0]

            test <@ request.Method = HttpMethod.Post @>
            test <@ request.RequestUri = Uri($"http://localhost:%d{PORT}/sql", UriKind.Absolute) @>

            let content = request.Content
            test <@ not (isNull content) @>

            let contentType = content.Headers.ContentType
            test <@ contentType.MediaType = TEXT_PLAIN @>
            test <@ contentType.CharSet = "utf-8" @>

            let! text = content.ReadAsStringAsync()
            test <@ text = query @>

            let expectedHeaders: HeadersInfo =
                { version = DUMMY_VERSION
                  server = DUMMY_SERVER
                  status = expectedStatus
                  date = DUMMY_DATE }

            let expectedResult: ErrorInfo =
                { code = 400
                  details = "Request problems detected"
                  description =
                    "There is a problem with your request. Refer to the documentation for further information."
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
    let ``SqlAsync with empty response`` () =
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

            let query = "SELECT * FROM people WHERE age >= 18;"

            let! response = testing.surrealClient.Json.SqlAsync(query, testing.cancellationToken)

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
    let ``SqlAsync with array response`` () =
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
                            "id": "people:dr3mc523txrii4cfuczh",
                            "lastName": "Doe"
                        },
                        {
                            "age": 17,
                            "firstName": "Jane",
                            "id": "people:zi1e78q4onfh6wypk5bz",
                            "lastName": "Doe"
                        }
                        ]
                    }
                    ]"""

            use _ = testing.disposable

            let query =
                """
                INSERT INTO people
                (firstName, lastName, age) VALUES
                ("John", "Doe", 19),
                ("Jane", "Doe", 17);
                """

            let! response = testing.surrealClient.Json.SqlAsync(query, testing.cancellationToken)

            let expectedHeaders: HeadersInfo =
                { version = DUMMY_VERSION
                  server = DUMMY_SERVER
                  status = expectedStatus
                  date = DUMMY_DATE }

            let expectedJson =
                JsonSerializer.Deserialize<JsonNode>(
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
                ]""",
                    testing.jsonOptions
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
    let ``SqlAsync with multiple and mixed responses`` () =
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
                """
                INFO FOR KV;
                USE NS testns DB testdb;
                """

            let! response = testing.surrealClient.Json.SqlAsync(query, testing.cancellationToken)

            let expectedHeaders: HeadersInfo =
                { version = DUMMY_VERSION
                  server = DUMMY_SERVER
                  status = expectedStatus
                  date = DUMMY_DATE }

            let expectedJson1 =
                JsonSerializer.Deserialize<JsonNode>(
                    """{
                        "ns": {
                            "testns": "DEFINE NAMESPACE testns"
                        }
                    }""",
                    testing.jsonOptions
                )

            let expectedJson2: JsonNode =
                JsonSerializer.Deserialize<JsonNode>("""null""", testing.jsonOptions)

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

    [<Fact>]
    let ``GetAllAsync with array response`` () =
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
                                    "id": "people:dr3mc523txrii4cfuczh",
                                    "lastName": "Doe"
                                },
                                {
                                    "age": 17,
                                    "firstName": "Jane",
                                    "id": "people:zi1e78q4onfh6wypk5bz",
                                    "lastName": "Doe"
                                }
                            ]
                        }
                    ]"""

            use _ = testing.disposable

            let table = "people"

            let! response = testing.surrealClient.Json.GetAllAsync(table, testing.cancellationToken)

            test <@ testing.requests.Count = 1 @>

            let request = testing.requests.[0]

            test <@ request.Method = HttpMethod.Get @>
            test <@ request.RequestUri = Uri($"http://localhost:%d{PORT}/key/%s{table}", UriKind.Absolute) @>

            let content = request.Content
            test <@ isNull content @>

            let expectedHeaders: HeadersInfo =
                { version = DUMMY_VERSION
                  server = DUMMY_SERVER
                  status = expectedStatus
                  date = DUMMY_DATE }

            let expectedJson =
                JsonSerializer.Deserialize<JsonNode>(
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
                    ]""",
                    testing.jsonOptions
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
    let ``GetAllAsync with missing result`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let testing =
                prepareTest
                    expectedStatus
                    """[
                        {
                            "time": "151.3µs",
                            "status": "OK"
                        }
                    ]"""

            use _ = testing.disposable

            let table = "people"

            let! response = testing.surrealClient.Json.GetAllAsync(table, testing.cancellationToken)

            let expectedHeaders: HeadersInfo =
                { version = DUMMY_VERSION
                  server = DUMMY_SERVER
                  status = expectedStatus
                  date = DUMMY_DATE }

            let expectedJson =
                JsonSerializer.Deserialize<JsonNode>(
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
                    ]""",
                    testing.jsonOptions
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
                | Error error -> test <@ error = "Missing result and detail" @>
                | Ok _ -> Assert.Fail "Expected error response"
        }

    [<Fact>]
    let ``CreateAsync with array response`` () =
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
                                    "id": "people:dr3mc523txrii4cfuczh",
                                    "lastName": "Doe"
                                }
                            ]
                        }
                    ]"""

            use _ = testing.disposable

            let table = "people"

            let record =
                JsonSerializer.Deserialize<JsonNode>(
                    """{
                        "age": 19,
                        "firstName": "John",
                        "lastName": "Doe"
                    }""",
                    testing.jsonOptions
                )

            let! response = testing.surrealClient.Json.CreateAsync(table, record, testing.cancellationToken)

            test <@ testing.requests.Count = 1 @>

            let request = testing.requests.[0]

            test <@ request.Method = HttpMethod.Post @>
            test <@ request.RequestUri = Uri($"http://localhost:%d{PORT}/key/%s{table}", UriKind.Absolute) @>

            let content = request.Content
            test <@ not (isNull content) @>

            let contentType = content.Headers.ContentType
            test <@ contentType.MediaType = APPLICATION_JSON @>
            test <@ contentType.CharSet = "utf-8" @>

            let! text = content.ReadAsStringAsync()

            let expectedRequestBody =
                JsonSerializer.Serialize(record, testing.jsonOptions)

            test <@ text = expectedRequestBody @>

            let expectedHeaders: HeadersInfo =
                { version = DUMMY_VERSION
                  server = DUMMY_SERVER
                  status = expectedStatus
                  date = DUMMY_DATE }

            let expectedJson =
                JsonSerializer.Deserialize<JsonNode>(
                    """[
                        {
                            "id": "people:dr3mc523txrii4cfuczh",
                            "firstName": "John",
                            "lastName": "Doe",
                            "age": 19
                        }
                    ]""",
                    testing.jsonOptions
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
    let ``DeleteAllAsync with array response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let testing =
                prepareTest
                    expectedStatus
                    """[
                        {
                            "time": "151.3µs",
                            "status": "OK",
                            "result": []
                        }
                    ]"""

            use _ = testing.disposable

            let table = "people"

            let! response = testing.surrealClient.Json.DeleteAllAsync(table, testing.cancellationToken)

            test <@ testing.requests.Count = 1 @>

            let request = testing.requests.[0]

            test <@ request.Method = HttpMethod.Delete @>
            test <@ request.RequestUri = Uri($"http://localhost:%d{PORT}/key/%s{table}", UriKind.Absolute) @>

            let content = request.Content
            test <@ isNull content @>

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
                test <@ first.time = "151.3µs" @>
                test <@ first.timeSpan = ValueSome(TimeSpan.FromMilliseconds(0.1513)) @>
                test <@ first.status = "OK" @>

                match first.response with
                | Error _ -> Assert.Fail "Expected success response"
                | Ok json -> test <@ jsonDiff json expectedJson = [] @>
        }

    [<Fact>]
    let ``GetAsync with array response`` () =
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
                                    "id": "people:dr3mc523txrii4cfuczh",
                                    "lastName": "Doe"
                                }
                            ]
                        }
                    ]"""

            use _ = testing.disposable

            let table = "people"
            let recordId = "dr3mc523txrii4cfuczh"

            let! response = testing.surrealClient.Json.GetAsync(table, recordId, testing.cancellationToken)

            test <@ testing.requests.Count = 1 @>

            let request = testing.requests.[0]

            test <@ request.Method = HttpMethod.Get @>

            test
                <@ request.RequestUri = Uri($"http://localhost:%d{PORT}/key/%s{table}/%s{recordId}", UriKind.Absolute) @>

            let content = request.Content
            test <@ isNull content @>

            let expectedHeaders: HeadersInfo =
                { version = DUMMY_VERSION
                  server = DUMMY_SERVER
                  status = expectedStatus
                  date = DUMMY_DATE }

            let expectedJson =
                JsonSerializer.Deserialize<JsonNode>(
                    """[
                        {
                            "age": 19,
                            "firstName": "John",
                            "id": "people:dr3mc523txrii4cfuczh",
                            "lastName": "Doe"
                        }
                    ]""",
                    testing.jsonOptions
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
    let ``CreateAsync one with array response`` () =
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
                                    "id": "people:john",
                                    "lastName": "Doe"
                                }
                            ]
                        }
                    ]"""

            use _ = testing.disposable

            let table = "people"

            let record =
                JsonSerializer.Deserialize<JsonNode>(
                    """{
                        "age": 19,
                        "firstName": "John",
                        "lastName": "Doe"
                    }""",
                    testing.jsonOptions
                )

            let recordId = "john"

            let! response = testing.surrealClient.Json.CreateAsync(table, recordId, record, testing.cancellationToken)

            test <@ testing.requests.Count = 1 @>

            let request = testing.requests.[0]

            test <@ request.Method = HttpMethod.Post @>

            test
                <@ request.RequestUri = Uri($"http://localhost:%d{PORT}/key/%s{table}/%s{recordId}", UriKind.Absolute) @>

            let content = request.Content
            test <@ not (isNull content) @>

            let contentType = content.Headers.ContentType
            test <@ contentType.MediaType = APPLICATION_JSON @>
            test <@ contentType.CharSet = "utf-8" @>

            let! text = content.ReadAsStringAsync()

            let expectedRequestBody =
                JsonSerializer.Serialize(record, testing.jsonOptions)

            test <@ text = expectedRequestBody @>

            let expectedHeaders: HeadersInfo =
                { version = DUMMY_VERSION
                  server = DUMMY_SERVER
                  status = expectedStatus
                  date = DUMMY_DATE }

            let expectedJson =
                JsonSerializer.Deserialize<JsonNode>(
                    """[
                        {
                            "id": "people:john",
                            "firstName": "John",
                            "lastName": "Doe",
                            "age": 19
                        }
                    ]""",
                    testing.jsonOptions
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
    let ``CreateAsync with error response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let testing =
                prepareTest
                    expectedStatus
                    """[
                        {
                            "time": "151.3µs",
                            "status": "ERR",
                            "detail": "Database record `people:john` already exists"
                        }
                    ]"""

            use _ = testing.disposable

            let table = "people"

            let record =
                JsonSerializer.Deserialize<JsonNode>(
                    """{
                        "age": 19,
                        "firstName": "John",
                        "lastName": "Doe"
                    }""",
                    testing.jsonOptions
                )

            let recordId = "john"

            let! response = testing.surrealClient.Json.CreateAsync(table, recordId, record, testing.cancellationToken)

            let expectedHeaders: HeadersInfo =
                { version = DUMMY_VERSION
                  server = DUMMY_SERVER
                  status = expectedStatus
                  date = DUMMY_DATE }

            test <@ response.headers = expectedHeaders @>

            match response.result with
            | Error _ -> Assert.Fail "Expected success response"
            | Ok result ->
                test <@ result.Length = 1 @>
                let first = result.[0]
                test <@ first.time = "151.3µs" @>
                test <@ first.timeSpan = ValueSome(TimeSpan.FromMilliseconds(0.1513)) @>
                test <@ first.status = "ERR" @>

                match first.response with
                | Error error -> test <@ error = "Database record `people:john` already exists" @>
                | Ok _ -> Assert.Fail "Expected error response"
        }

    [<Fact>]
    let ``InsertOrUpdateAsync with array response`` () =
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
                                    "age": 24,
                                    "firstName": "John",
                                    "id": "people:john",
                                    "lastName": "Doe"
                                }
                            ]
                        }
                    ]"""

            use _ = testing.disposable

            let table = "people"

            let record =
                JsonSerializer.Deserialize<JsonNode>(
                    """{
                        "age": 24,
                        "firstName": "John",
                        "lastName": "Doe"
                    }""",
                    testing.jsonOptions
                )

            let recordId = "john"

            let! response =
                testing.surrealClient.Json.InsertOrUpdateAsync(table, recordId, record, testing.cancellationToken)

            test <@ testing.requests.Count = 1 @>

            let request = testing.requests.[0]

            test <@ request.Method = HttpMethod.Put @>

            test
                <@ request.RequestUri = Uri($"http://localhost:%d{PORT}/key/%s{table}/%s{recordId}", UriKind.Absolute) @>

            let content = request.Content
            test <@ not (isNull content) @>

            let contentType = content.Headers.ContentType
            test <@ contentType.MediaType = APPLICATION_JSON @>
            test <@ contentType.CharSet = "utf-8" @>

            let! text = content.ReadAsStringAsync()

            let expectedRequestBody =
                JsonSerializer.Serialize(record, testing.jsonOptions)

            test <@ text = expectedRequestBody @>

            let expectedHeaders: HeadersInfo =
                { version = DUMMY_VERSION
                  server = DUMMY_SERVER
                  status = expectedStatus
                  date = DUMMY_DATE }

            let expectedJson =
                JsonSerializer.Deserialize<JsonNode>(
                    """[
                        {
                            "id": "people:john",
                            "firstName": "John",
                            "lastName": "Doe",
                            "age": 24
                        }
                    ]""",
                    testing.jsonOptions
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
    let ``PatchAsync with array response`` () =
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
                                    "age": 24,
                                    "firstName": "Johnny",
                                    "id": "people:john",
                                    "lastName": "Doe"
                                }
                            ]
                        }
                    ]"""

            use _ = testing.disposable

            let table = "people"

            let record =
                JsonSerializer.Deserialize<JsonNode>(
                    """{
                        "firstName": "Johnny"
                    }""",
                    testing.jsonOptions
                )

            let recordId = "john"

            let! response = testing.surrealClient.Json.PatchAsync(table, recordId, record, testing.cancellationToken)

            test <@ testing.requests.Count = 1 @>

            let request = testing.requests.[0]

            test <@ request.Method = HttpMethod.Patch @>

            test
                <@ request.RequestUri = Uri($"http://localhost:%d{PORT}/key/%s{table}/%s{recordId}", UriKind.Absolute) @>

            let content = request.Content
            test <@ not (isNull content) @>

            let contentType = content.Headers.ContentType
            test <@ contentType.MediaType = APPLICATION_JSON @>
            test <@ contentType.CharSet = "utf-8" @>

            let! text = content.ReadAsStringAsync()

            let expectedRequestBody =
                JsonSerializer.Serialize(record, testing.jsonOptions)

            test <@ text = expectedRequestBody @>

            let expectedHeaders: HeadersInfo =
                { version = DUMMY_VERSION
                  server = DUMMY_SERVER
                  status = expectedStatus
                  date = DUMMY_DATE }

            let expectedJson =
                JsonSerializer.Deserialize<JsonNode>(
                    """[
                        {
                            "id": "people:john",
                            "firstName": "Johnny",
                            "lastName": "Doe",
                            "age": 24
                        }
                    ]""",
                    testing.jsonOptions
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
    let ``DeleteAsync with empty response`` () =
        task {
            let expectedStatus = HttpStatusCode.OK

            let testing =
                prepareTest
                    expectedStatus
                    """[
                        {
                            "time": "151.3µs",
                            "status": "OK",
                            "result": []
                        }
                    ]"""

            use _ = testing.disposable

            let table = "people"
            let recordId = "dr3mc523txrii4cfuczh"

            let! response = testing.surrealClient.Json.DeleteAsync(table, recordId, testing.cancellationToken)

            test <@ testing.requests.Count = 1 @>

            let request = testing.requests.[0]

            test <@ request.Method = HttpMethod.Delete @>

            test
                <@ request.RequestUri = Uri($"http://localhost:%d{PORT}/key/%s{table}/%s{recordId}", UriKind.Absolute) @>

            let content = request.Content
            test <@ isNull content @>

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
                test <@ first.time = "151.3µs" @>
                test <@ first.timeSpan = ValueSome(TimeSpan.FromMilliseconds(0.1513)) @>
                test <@ first.status = "OK" @>

                match first.response with
                | Error _ -> Assert.Fail "Expected success response"
                | Ok json -> test <@ jsonDiff json expectedJson = [] @>
        }
