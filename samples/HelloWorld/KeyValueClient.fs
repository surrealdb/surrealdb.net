module KeyValueClient

type Person =
    { id: string
      firstName: string
      lastName: string
      age: int }

open System.Net.Http
open System.Text.Json
open System.Text.Json.Serialization
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

        let jsonOptions =
            JsonFSharpOptions
                .Default()
                .WithAllowNullFields(true)
                .WithSkippableOptionFields(true)
                .ToJsonSerializerOptions()

        let httpClient = new HttpClient()

        use client =
            new SurrealRestClient(config, httpClient, jsonOptions)

        let client = client :> ISurrealRestClient

        printfn "HttpClient base url: %A" httpClient.BaseAddress
        printfn "HttpClient headers:\n%O" httpClient.DefaultRequestHeaders

        let ct = CancellationToken.None

        let john =
            { firstName = "John"
              lastName = "Doe"
              age = 24
              id = "" }

        let! createJohnResult = client.KeyValue.CreateAsync("people", "john", john, ct)

        printfn "Create John result:\n%A" createJohnResult

        let! peopleResult = client.KeyValue.GetAllAsync<Person>("people", ct)

        printfn "Get people result:\n%A" peopleResult
    }
