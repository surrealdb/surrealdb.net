namespace SurrealDB.Client.FSharp.Rest

open System
open System.Collections.Generic
open System.Globalization
open System.Net
open System.Net.Http
open System.Net.Http.Headers
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open System.Text.Json.Serialization
open System.Threading
open System.Threading.Tasks

open DotNet.Testcontainers.Builders
open DotNet.Testcontainers.Containers
open Swensen.Unquote
open Xunit

open SurrealDB.Client.FSharp
open SurrealDB.Client.FSharp.Rest

[<Trait(Category, IntegrationTest)>]
[<Trait(Area, REST)>]
module SurrealJsonClientTests =
    type Testing =
        { config: SurrealConfig
          httpClient: HttpClient
          jsonOptions: JsonSerializerOptions
          endpoints: ISurrealJsonClient
          container: IContainer
          cancellationTokenSource: CancellationTokenSource
          cancellationToken: CancellationToken
          disposable: IAsyncDisposable }

    let resultValue =
        function
        | Ok value -> value
        | Error err -> failwith $"Expected Ok, got Error: %A{err}"

    let DockerImage = "surrealdb/surrealdb:latest"
    let PORT = 8000
    let USER = "root"
    let PASS = "root"
    let NS = "testns"
    let DB = "testdb"

    let prepareTest () =
        task {
            let container =
                ContainerBuilder()
                    .WithImage(DockerImage)
                    .WithEnvironment("USER", USER)
                    .WithEnvironment("PASS", PASS)
                    .WithEnvironment("STRICT", "false")
                    .WithPortBinding(PORT, true)
                    .WithCommand("start", "memory")
                    .Build()

            do! container.StartAsync().ConfigureAwait(false)

            let httpPort = container.GetMappedPublicPort(PORT)

            let config =
                SurrealConfig
                    .Builder()
                    .WithBaseUrl($"http://localhost:%d{httpPort}")
                    .WithBasicCredentials(USER, PASS)
                    .WithNamespace(NS)
                    .WithDatabase(DB)
                    .Build()
                |> resultValue

            let httpClient = new HttpClient()

            let jsonOptions = Json.defaultOptions

            let endpoints: ISurrealJsonClient =
                new SurrealJsonClient(config, httpClient, jsonOptions)

            let cancellationTokenSource = new CancellationTokenSource()

            let disposable =
                { new IAsyncDisposable with
                    member this.DisposeAsync() =
                        task {
                            cancellationTokenSource.Dispose()
                            httpClient.Dispose()
                            do! container.StopAsync()
                        }
                        |> ValueTask }

            return
                { container = container
                  endpoints = endpoints
                  config = config
                  httpClient = httpClient
                  jsonOptions = jsonOptions
                  cancellationTokenSource = cancellationTokenSource
                  cancellationToken = cancellationTokenSource.Token
                  disposable = disposable }
        }

    let testResponseHeaders (response: RestApiResult<JsonNode>) expectedStatus =
        Assert.Matches("^surreal\-(\d+(\.\d+)*)(\-.*)?$", response.headers.version)
        test <@ response.headers.server = "SurrealDB" @>
        test <@ response.headers.status = expectedStatus @>

        match response.headers.dateTime with
        | ValueSome dateTime -> Assert.InRange(dateTime, DateTime.Now.AddMinutes(-1.0), DateTime.Now.AddMinutes(1.0))
        | ValueNone -> Assert.Fail $"Expected dateTime to be set, got {response.headers.date}"

        match response.headers.dateTimeOffset with
        | ValueSome dateTimeOffset ->
            Assert.InRange(dateTimeOffset, DateTimeOffset.Now.AddMinutes(-1.0), DateTimeOffset.Now.AddMinutes(1.0))
        | ValueNone -> Assert.Fail $"Expected dateTimeOffset to be set, got {response.headers.date}"

    let testResponseError (response: RestApiResult<JsonNode>) expectedError =
        match response.statements with
        | Ok result -> Assert.Fail $"Expected error, got {result}"
        | Error error -> test <@ error = ResponseError expectedError @>

    let testResponseJsonWith expectedFn (response: RestApiResult<JsonNode>) expectedStatus (expectedJson: string) =
        match response.statements with
        | Error error -> Assert.Fail $"Expected success result, got {error}"
        | Ok result ->
            let expectedJson = JsonNode.Parse(expectedJson)
            test <@ result.Length = 1 @>
            let first = result.[0]
            Assert.NotNull(first.time)

            match first.timeSpan with
            | ValueSome timeSpan -> Assert.True(timeSpan >= TimeSpan.Zero)
            | ValueNone -> Assert.Fail $"Expected timeSpan to be set, got {first.time}"

            test <@ first.status = expectedStatus @>

            match first.response with
            | Error error -> Assert.Fail $"Expected success result, got {error}"
            | Ok json -> test <@ jsonDiff expectedFn json expectedJson = [] @>

    let testResponseJson (response: RestApiResult<JsonNode>) expectedStatus (expectedJson: string) =
        testResponseJsonWith (fun _ -> false) response expectedStatus expectedJson

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

    [<Fact>]
    let ``Combined operations`` () =
        task {
            let! t = prepareTest ()
            use _ = t.disposable
            let table = "people"

            let johnId = "john"
            let janeId = "jane"

            let johnJson =
                sprintf
                    """{
                    "id": "%s:%s",
                    "firstName": "John",
                    "lastName": "Doe",
                    "age": 42
                }"""
                    table
                    johnId

            let janeJson =
                sprintf
                    """{
                    "id": "%s:%s",
                    "firstName": "Jane",
                    "lastName": "Doe",
                    "age": 42
                }"""
                    table
                    janeId

            let johnnyJson =
                sprintf
                    """{
                    "id": "%s:%s",
                    "firstName": "Johnny",
                    "lastName": "Doe",
                    "age": 42
                }"""
                    table
                    johnId


            // Test list of people with empty database
            let expectedJson = "[]"
            let! response = t.endpoints.ListAsync(table, t.cancellationToken)
            testResponseHeaders response HttpStatusCode.OK
            testResponseJson response "OK" expectedJson

            // Test insert a person without an id
            let expectedJson =
                """[
                {
                    "firstName": "John",
                    "lastName": "Doe",
                    "age": 42
                }
            ]"""

            let record =
                JsonNode.Parse
                    """{
                "firstName": "John",
                "lastName": "Doe",
                "age": 42
            }"""

            let! response = t.endpoints.CreateAsync(table, record, t.cancellationToken)

            testResponseJsonWith
                (fun diff ->
                    match diff.path, diff.diff with
                    | JsonIndex 0 :: [], MissingRightKey ("id", node) ->
                        match SurrealId.TryParse(node.ToString()) with
                        | Ok id -> id.table = table && id.id <> ""
                        | Error _ -> false
                    | _ -> false)
                response
                "OK"
                expectedJson

            // Test delete all people
            let expectedJson = "[]"
            let! response = t.endpoints.DeleteAllAsync(table, t.cancellationToken)
            testResponseHeaders response HttpStatusCode.OK
            testResponseJson response "OK" expectedJson


            // Test a SQL query
            let expectedJson =
                """{
                "ns": {
                    "testns": "DEFINE NAMESPACE testns"
                }
            }"""

            let! response = t.endpoints.SqlAsync("INFO FOR KV;", t.cancellationToken)
            testResponseHeaders response HttpStatusCode.OK
            testResponseJson response "OK" expectedJson


            // Test get a non-existent person by id
            let expectedJson = "[]"
            let! response = t.endpoints.FindAsync(table, johnId, t.cancellationToken)
            testResponseHeaders response HttpStatusCode.OK
            testResponseJson response "OK" expectedJson

            // Test create a person with an id
            let expectedJson = sprintf "[ %s ]" johnJson

            let record =
                JsonNode.Parse
                    """{
                "firstName": "John",
                "lastName": "Doe",
                "age": 42
            }"""

            let! response = t.endpoints.InsertAsync(table, johnId, record, t.cancellationToken)
            testResponseHeaders response HttpStatusCode.OK
            testResponseJson response "OK" expectedJson

            // Test update a non-existent person by id
            let expectedJson = sprintf "[ %s ]" janeJson

            let record =
                JsonNode.Parse
                    """{
                "firstName": "Jane",
                "lastName": "Doe",
                "age": 42
            }"""

            let! response = t.endpoints.ReplaceAsync(table, janeId, record, t.cancellationToken)
            testResponseHeaders response HttpStatusCode.OK
            testResponseJson response "OK" expectedJson

            // Test patch an existing person by id
            let expectedJson = sprintf "[ %s ]" johnnyJson

            let record =
                JsonNode.Parse
                    """{
                "firstName": "Johnny"
            }"""

            let! response = t.endpoints.ModifyAsync(table, johnId, record, t.cancellationToken)
            testResponseHeaders response HttpStatusCode.OK
            testResponseJson response "OK" expectedJson

            // Test get all people
            let expectedJson = sprintf "[ %s, %s ]" janeJson johnnyJson
            let! response = t.endpoints.ListAsync(table, t.cancellationToken)
            testResponseHeaders response HttpStatusCode.OK
            testResponseJson response "OK" expectedJson

            // Test delete a person by id
            let expectedJson = """[]"""
            let! response = t.endpoints.DeleteAsync(table, johnId, t.cancellationToken)
            testResponseHeaders response HttpStatusCode.OK
            testResponseJson response "OK" expectedJson
            let expectedJson = sprintf "[ %s ]" janeJson
            let! response = t.endpoints.ListAsync(table, t.cancellationToken)
            testResponseHeaders response HttpStatusCode.OK
            testResponseJson response "OK" expectedJson
        }
