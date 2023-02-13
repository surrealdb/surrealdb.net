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

open DotNet.Testcontainers
open DotNet.Testcontainers.Builders
open Swensen.Unquote
open Xunit

open SurrealDB.Client.FSharp
open SurrealDB.Client.FSharp.Rest

[<Xunit.Trait(Category, IntegrationTest)>]
module EndpointsTests =

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

    let startContainer () =
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

            Endpoints.applyConfig config httpClient

            let jsonOptions = Json.defaultOptions

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
                {| container = container
                   config = config
                   httpClient = httpClient
                   jsonOptions = jsonOptions
                   cancellationTokenSource = cancellationTokenSource
                   cancellationToken = cancellationTokenSource.Token
                   disposable = disposable |}
        }

    //[<Fact>]
    let ``Endpoints.postSql`` () =
        task {
            let! testing = startContainer ()
            use _ = testing.disposable

            let table = "people"
        
            let! response = Endpoints.getKeyTable testing.jsonOptions testing.httpClient testing.cancellationToken table

            match response.headers.dateTime with
            | ValueSome dateTime ->
                test <@ dateTime <= DateTime.Now @>
                test <@ dateTime > DateTime.Now.AddMinutes(-1.0) @>
            | ValueNone -> ()
        }
