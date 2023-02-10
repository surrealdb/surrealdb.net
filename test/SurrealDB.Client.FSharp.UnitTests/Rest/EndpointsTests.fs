module SurrealDB.Client.FSharp.Rest.EndpointsTests

open System
open System.Collections.Generic
open System.Globalization
open System.Net
open System.Net.Http
open System.Text
open System.Text.Json
open System.Text.Json.Nodes
open System.Text.Json.Serialization
open System.Threading

open Xunit
open FsCheck.Xunit
open Foq
open Swensen.Unquote

open SurrealDB.Client.FSharp
open SurrealDB.Client.FSharp.Rest
open System.Net.Http.Headers

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

[<Fact>]
let ``postSql`` () =
    task {
        let config =
            SurrealConfig
                .Builder()
                .WithBaseUrl("http://localhost:8010")
                .WithNamespace("testns")
                .WithDatabase("testdb")
                .Build()
            |> resultValue

        let jsonOptions =
            JsonFSharpOptions
                .Default()
                //.WithAllowNullFields(true)
                .WithSkippableOptionFields(true)
                .ToJsonSerializerOptions()

        let query = "INFO FOR KV;"
        let apiResult = RestApiResult.empty ()

        let apiResultJson =
            """[
  {
    "time": "1.2729ms",
    "status": "OK",
    "result": {
      "ns": {}
    }
  }
]"""

        let ct = CancellationToken.None
        let mutable sent = false

        let handler =
            { new HttpMessageHandler() with
                override this.SendAsync(request, cancellationToken) =
                    task {
                        sent <- true
                        test <@ request.Method = HttpMethod.Post @>
                        test <@ request.Content.Headers.ContentType = MediaTypeHeaderValue(TEXT_PLAIN, "utf-8") @>

                        let! content = request.Content.ReadAsStringAsync(cancellationToken)
                        test <@ content = query @>

                        let response =
                            new HttpResponseMessage(HttpStatusCode.OK)

                        response.Content <- new StringContent(apiResultJson, Encoding.UTF8, APPLICATION_JSON)
                        return response
                    } }

        use httpClient = new HttpClient(handler)
        Endpoints.applyConfig config httpClient

        let! response = Endpoints.postSql jsonOptions query ct httpClient

        test <@ sent = true @>
        test <@ response.headers = apiResult.headers @>
    }
