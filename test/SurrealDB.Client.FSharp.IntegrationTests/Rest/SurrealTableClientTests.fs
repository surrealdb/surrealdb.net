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
module SurrealTableClientTests =
    type Testing =
        { config: SurrealConfig
          httpClient: HttpClient
          jsonOptions: JsonSerializerOptions
          endpoints: ISurrealTableClient
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

    type Person =
        { id: SurrealId
          firstName: string
          lastName: string
          age: int }

    type PersonData =
        { firstName: string
          lastName: string
          age: int }

    type PersonFirstNamePatch = { firstName: string }

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

            let endpoints: ISurrealTableClient =
                new SurrealTableClient(config, httpClient, jsonOptions)

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

    let testResponseHeaders (response: RestApiSingleResult<'result>) expectedStatus =
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

    let testResponseError (response: RestApiSingleResult<'result>) expectedError =
        match response.statement with
        | Ok result -> Assert.Fail $"Expected error, got {result}"
        | Error error -> test <@ error = ResponseError expectedError @>

    let testResponseResultWith
        (response: RestApiSingleResult<'result>)
        expectedStatus
        (expectedResult: 'result -> unit)
        =
        match response.statement with
        | Error error -> Assert.Fail $"Expected success result, got {error}"
        | Ok statement ->
            match statement.timeSpan with
            | ValueSome timeSpan -> Assert.True(timeSpan >= TimeSpan.Zero)
            | ValueNone -> Assert.Fail $"Expected timeSpan to be set, got {statement.time}"

            test <@ statement.status = expectedStatus @>

            match statement.response with
            | Error error -> Assert.Fail $"Expected success result, got {error}"
            | Ok result -> expectedResult result

    let testResponseResult
        (response: RestApiSingleResult<'result>)
        expectedStatus
        (expectedResult: 'result)
        =
        testResponseResultWith
            response
            expectedStatus
            (fun result -> test <@ result = expectedResult @>)

    let testResult (response: RequestResult<'result>) (expectedResult: 'result) =
        match response with
        | Error error -> Assert.Fail $"Expected success result, got {error}"
        | Ok result -> test <@ result = expectedResult @>

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

            let johnId = SurrealId.Create(table, "john")
            let janeId = SurrealId.Create(table, "jane")

            let john =
                { id = johnId
                  firstName = "John"
                  lastName = "Doe"
                  age = 42 }

            let jane =
                { john with
                    id = janeId
                    firstName = "Jane" }

            let johnny = { john with firstName = "Johnny" }


            // Test list of people with empty database
            let expected : Person[] = [||]
            let! response = t.endpoints.ListResponseAsync<Person>(table, t.cancellationToken)
            testResponseHeaders response HttpStatusCode.OK
            testResponseResult response "OK" expected

            // Test insert a person without an id
            let expected = john
            let data = { firstName = "John"; lastName = "Doe"; age = 42 }

            let! response = t.endpoints.CreatePartialResponseAsync<PersonData, Person>(table, data, t.cancellationToken)

            testResponseResultWith response "OK" (fun result ->
                test <@ result.firstName = data.firstName @>
                test <@ result.lastName = data.lastName @>
                test <@ result.age = data.age @>
                test <@ result.id.table = table @>
                test <@ String.length result.id.id > 0 @>
            )

            // Test delete all people
            let! response = t.endpoints.DeleteAllResponseAsync(table, t.cancellationToken)
            testResponseHeaders response HttpStatusCode.OK
            testResponseResult response "OK" ()


            // Test get a non-existent person by id
            let! response = t.endpoints.FindResponseAsync(table, johnId.id, t.cancellationToken)
            testResponseHeaders response HttpStatusCode.OK
            testResponseResult response "OK" ValueNone

            // Test create a person with an id
            let expected = john
            let data = { firstName = john.firstName; lastName = john.lastName; age = john.age }

            let! response = t.endpoints.InsertPartialResponseAsync(table, johnId.id, data, t.cancellationToken)
            testResponseHeaders response HttpStatusCode.OK
            testResponseResult response "OK" expected

            // Test update a non-existent person by id
            let expected = jane
            let data = { firstName = jane.firstName; lastName = jane.lastName; age = jane.age }
            let! response = t.endpoints.ReplacePartialResponseAsync(table, janeId.id, data, t.cancellationToken)
            testResponseHeaders response HttpStatusCode.OK
            testResponseResult response "OK" expected

            // Test patch an existing person by id
            let expected = johnny
            let data = { firstName = johnny.firstName }
            let! response = t.endpoints.ModifyPartialResponseAsync(table, johnId.id, data, t.cancellationToken)
            testResponseHeaders response HttpStatusCode.OK
            testResponseResult response "OK" expected

            // Test get all people
            let expected = [| jane; johnny |]
            let! response = t.endpoints.ListResponseAsync(table, t.cancellationToken)
            testResponseHeaders response HttpStatusCode.OK
            testResponseResult response "OK" expected

            // Test delete a person by id
            let! response = t.endpoints.DeleteResponseAsync(table, johnId.id, t.cancellationToken)
            testResponseHeaders response HttpStatusCode.OK
            testResponseResult response "OK" ()
            let expected = [| jane |]
            let! response = t.endpoints.ListResponseAsync(table, t.cancellationToken)
            testResponseHeaders response HttpStatusCode.OK
            testResponseResult response "OK" expected
        }
