module SurrealJsonClient

type Person =
    { id: string
      firstName: string
      lastName: string
      age: int }

open System.Net.Http
open System.Text.Json
open System.Threading

open SurrealDB.Client.FSharp
open SurrealDB.Client.FSharp.Rest

let sample () =
    task {

        let config =
            SurrealConfig
                .Builder()
                .WithBaseUrl("http://localhost:8010")
                .WithBasicCredentials("root", "root")
                .WithNamespace("testns")
                .WithDatabase("testdb")
                .Build()
            |> function
                | Ok c -> c
                | Error err ->
                    printfn "Error: %A" err
                    failwith "Invalid configuration"

        printfn "Config: %A" config

        use httpClient = new HttpClient()
        let jsonOptions = Json.defaultOptions

        let endpoints: ISurrealJsonClient = new SurrealJsonClient(config, httpClient, jsonOptions)

        printfn "HttpClient base url: %A" httpClient.BaseAddress
        printfn "HttpClient headers:\n%O" httpClient.DefaultRequestHeaders

        let ct = CancellationToken.None

        let john =
            { firstName = "John"
              lastName = "Doe"
              age = 24
              id = "" }

        let johnJson =
            JsonSerializer.SerializeToNode(john, jsonOptions)

        let! createJohnResult = endpoints.InsertAsync("people", "john", johnJson, ct)

        printfn "Create John result headers:\n%A" createJohnResult.headers
        match createJohnResult.statements with
        | Ok statements ->
            match statements.[0].response with
            | Ok result ->
                printfn "Create John result:\n%A" (result.ToString())
            | Error err ->
                printfn "Create John statement error:\n%A" err
        | Error err ->
            printfn "Create John request error:\n%A" err

        let! peopleResult = endpoints.ListAsync("people", ct)

        printfn "Get people result:\n%A" peopleResult

        let! infoKv = endpoints.SqlAsync("INFO FOR KV;", ct)

        printfn "INFO FOR KV:\n%A" infoKv
    }
