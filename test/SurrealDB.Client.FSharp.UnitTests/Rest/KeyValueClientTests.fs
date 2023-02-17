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
module KeyValueClientTests =
    type Person =
        { id: string
          firstName: string
          lastName: string
          age: int }

    type PersonData =
        { firstName: string
          lastName: string
          age: int }

    type PersonPatchFirstName = { firstName: string }

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

        do Endpoints.applyConfig config httpClient

        let jsonOptions = Json.defaultOptions

        let cancellationTokenSource = new CancellationTokenSource()

        let surrealClient: ISurrealRestClient =
            new SurrealRestClient(httpClient, jsonOptions)

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
    let ``CreateDataAsync without id`` () =
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

            let recordData: PersonData =
                { age = 19
                  firstName = "John"
                  lastName = "Doe" }

            let expectedRecord: Person =
                { id = "people:dr3mc523txrii4cfuczh"
                  age = recordData.age
                  firstName = recordData.firstName
                  lastName = recordData.lastName }

            let! response =
                testing.surrealClient.KeyValue.CreateDataAsync<PersonData, Person>(
                    table,
                    recordData,
                    testing.cancellationToken
                )

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
            let requestJson = JsonNode.Parse(text)

            let expectedRequestJson =
                JsonSerializer.SerializeToNode(recordData, testing.jsonOptions)

            test <@ jsonDiff requestJson expectedRequestJson = [] @>

            match response with
            | Error _ -> Assert.Fail "Expected success response"
            | Ok result ->
                test <@ result = expectedRecord @>
        }

    [<Fact>]
    let ``CreateAsync without id`` () =
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

            let record: Person =
                { id = "people:dr3mc523txrii4cfuczh"
                  age = 19
                  firstName = "John"
                  lastName = "Doe" }

            let! response = testing.surrealClient.KeyValue.CreateAsync(table, record, testing.cancellationToken)

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
            let requestJson = JsonNode.Parse(text)

            let expectedRequestJson =
                JsonSerializer.SerializeToNode(record, testing.jsonOptions)

            test <@ jsonDiff requestJson expectedRequestJson = [] @>

            match response with
            | Error _ -> Assert.Fail "Expected success response"
            | Ok result ->
                test <@ result = record @>
        }

    [<Fact>]
    let ``CreateDataAsync with id`` () =
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

            let recordData: PersonData =
                { age = 19
                  firstName = "John"
                  lastName = "Doe" }

            let recordId = "john"

            let expectedRecord: Person =
                { id = $"%s{table}:%s{recordId}"
                  age = recordData.age
                  firstName = recordData.firstName
                  lastName = recordData.lastName }

            let! response =
                testing.surrealClient.KeyValue.CreateDataAsync<PersonData, Person>(
                    table,
                    recordId,
                    recordData,
                    testing.cancellationToken
                )

            match response with
            | Error _ -> Assert.Fail "Expected success response"
            | Ok result ->
                test <@ result = expectedRecord @>
        }

    [<Fact>]
    let ``CreateAsync with id`` () =
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

            let record: Person =
                { id = "people:john"
                  age = 19
                  firstName = "John"
                  lastName = "Doe" }

            let recordId = "john"

            let! response =
                testing.surrealClient.KeyValue.CreateAsync(table, recordId, record, testing.cancellationToken)

            match response with
            | Error _ -> Assert.Fail "Expected success response"
            | Ok result ->
                test <@ result = record @>
        }

    [<Fact>]
    let ``InsertOrUpdateDataAsync`` () =
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

            let recordData: PersonData =
                { age = 24
                  firstName = "John"
                  lastName = "Doe" }

            let recordId = "john"

            let expectedRecord: Person =
                { id = $"%s{table}:%s{recordId}"
                  age = recordData.age
                  firstName = recordData.firstName
                  lastName = recordData.lastName }

            let! response =
                testing.surrealClient.KeyValue.InsertOrUpdateDataAsync<PersonData, Person>(
                    table,
                    recordId,
                    recordData,
                    testing.cancellationToken
                )

            match response with
            | Error _ -> Assert.Fail "Expected success response"
            | Ok result ->
                test <@ result = expectedRecord @>
        }

    [<Fact>]
    let ``InsertOrUpdateAsync`` () =
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

            let record: Person =
                { id = "people:john"
                  age = 24
                  firstName = "John"
                  lastName = "Doe" }

            let recordId = "john"

            let! response =
                testing.surrealClient.KeyValue.InsertOrUpdateAsync(table, recordId, record, testing.cancellationToken)

            match response with
            | Error _ -> Assert.Fail "Expected success response"
            | Ok result ->
                test <@ result = record @>
        }

    [<Fact>]
    let ``PatchDataAsync`` () =
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
                                    "firstName": "Johnny",
                                    "id": "people:john",
                                    "lastName": "Doe"
                                }
                            ]
                        }
                    ]"""

            use _ = testing.disposable

            let table = "people"

            let recordData: PersonPatchFirstName = { firstName = "Johnny" }

            let recordId = "john"

            let expectedRecord: Person =
                { id = $"%s{table}:%s{recordId}"
                  age = 19
                  firstName = recordData.firstName
                  lastName = "Doe" }

            let! response =
                testing.surrealClient.KeyValue.PatchDataAsync<PersonPatchFirstName, Person>(
                    table,
                    recordId,
                    recordData,
                    testing.cancellationToken
                )

            match response with
            | Error _ -> Assert.Fail "Expected success response"
            | Ok result ->
                test <@ result = expectedRecord @>
        }

    [<Fact>]
    let ``GetAllAsync`` () =
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
                                },
                                {
                                    "age": 21,
                                    "firstName": "Jane",
                                    "id": "people:jane",
                                    "lastName": "Doe"
                                }
                            ]
                        }
                    ]"""

            use _ = testing.disposable

            let table = "people"

            let expectedRecords: Person [] =
                [| { id = "people:john"
                     age = 19
                     firstName = "John"
                     lastName = "Doe" }
                   { id = "people:jane"
                     age = 21
                     firstName = "Jane"
                     lastName = "Doe" } |]

            let! response = testing.surrealClient.KeyValue.GetAllAsync<Person>(table, testing.cancellationToken)

            match response with
            | Error _ -> Assert.Fail "Expected success response"
            | Ok result ->
                test <@ result = expectedRecords @>
        }

    [<Fact>]
    let ``GetAsync of existing record`` () =
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
            let recordId = "john"

            let expectedRecord: Person =
                { id = "people:john"
                  age = 19
                  firstName = "John"
                  lastName = "Doe" }

            let! response = testing.surrealClient.KeyValue.GetAsync<Person>(table, recordId, testing.cancellationToken)

            match response with
            | Error _ -> Assert.Fail "Expected success response"
            | Ok result ->
                test <@ result = ValueSome expectedRecord @>
        }

    [<Fact>]
    let ``GetAsync of missing record`` () =
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
            let recordId = "john"

            let! response = testing.surrealClient.KeyValue.GetAsync<Person>(table, recordId, testing.cancellationToken)

            match response with
            | Error _ -> Assert.Fail "Expected success response"
            | Ok result ->
                test <@ result = ValueNone @>
        }

    [<Fact>]
    let ``DeleteAllAsync`` () =
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

            let! response = testing.surrealClient.KeyValue.DeleteAllAsync(table, testing.cancellationToken)

            match response with
            | Error _ -> Assert.Fail "Expected success response"
            | Ok result ->
                test <@ result = () @>
        }

    [<Fact>]
    let ``DeleteAsync`` () =
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
            let recordId = "john"

            let! response = testing.surrealClient.KeyValue.DeleteAsync(table, recordId, testing.cancellationToken)

            match response with
            | Error _ -> Assert.Fail "Expected success response"
            | Ok result ->
                test <@ result = () @>
        }
