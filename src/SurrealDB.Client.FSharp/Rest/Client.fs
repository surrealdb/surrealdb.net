namespace SurrealDB.Client.FSharp.Rest

type ISurrealRestClient =
    abstract Json : ISurrealRestJsonClient
    abstract KeyValue : ISurrealRestKeyValueClient

type internal SurrealRestClient(jsonClient: ISurrealRestJsonClient) =
    let keyValueClient = SurrealRestKeyValueClient jsonClient

    interface ISurrealRestClient with
        member this.Json = jsonClient
        member this.KeyValue = keyValueClient
