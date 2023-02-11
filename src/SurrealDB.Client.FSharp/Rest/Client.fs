namespace SurrealDB.Client.FSharp.Rest

open System
open SurrealDB.Client.FSharp

type ISurrealRestClient =
    inherit IDisposable
    
    abstract Json : ISurrealRestJsonClient
    //abstract KeyValue : ISurrealRestKeyValueClient

type SurrealRestClient(config, httpClient, ?jsonOptions) =
    do Endpoints.applyConfig config httpClient

    let jsonOptions =
        jsonOptions
        |> Option.defaultValue SurrealConfig.defaultJsonOptions

    let jsonClient : ISurrealRestJsonClient =
        SurrealRestJsonClient(httpClient, jsonOptions)

    //let keyValueClient : ISurrealRestKeyValueClient =
    //    SurrealRestKeyValueClient(jsonClient, jsonOptions)

    member val Json = jsonClient
    member _.Dispose() = httpClient.Dispose()

    interface ISurrealRestClient with
        member this.Json = this.Json
        //member this.KeyValue = this.KeyValue

    interface System.IDisposable with
        member this.Dispose() = this.Dispose()
