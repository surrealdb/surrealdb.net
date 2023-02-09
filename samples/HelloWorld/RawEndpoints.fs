module RawEndpoints

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

        let httpClient = new HttpClient()
        let jsonOptions = JsonSerializerOptions()

        Endpoints.applyConfig config httpClient

        printfn "HttpClient base url: %A" httpClient.BaseAddress
        printfn "HttpClient headers:\n%O" httpClient.DefaultRequestHeaders

        let ct = CancellationToken.None

        let john =
            { firstName = "John"
              lastName = "Doe"
              age = 24
              id = "" }

        let johnJson =
            JsonSerializer.SerializeToNode(john, JsonSerializerOptions.Default)

        let! createJohnResult = Endpoints.postKeyTableId jsonOptions "people" "john" johnJson ct httpClient

        printfn "Create John result:\n%A" createJohnResult

        let! peopleResult = Endpoints.getKeyTable jsonOptions "people" ct httpClient

        printfn "Get people result:\n%A" peopleResult

        let! infoKv = Endpoints.postSql jsonOptions "INFO FOR KV;" ct httpClient

        printfn "INFO FOR KV:\n%A" infoKv
    }
