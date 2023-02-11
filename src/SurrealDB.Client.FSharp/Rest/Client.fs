namespace SurrealDB.Client.FSharp.Rest

open System
open SurrealDB.Client.FSharp

/// <summary>
/// Represents a REST client for the SurrealDB JSON API.
/// </summary>
type ISurrealRestClient =
    inherit IDisposable

    /// <summary>
    /// Gets the JSON API client. Use this to perform operations where you do not have classes to represent the data.
    /// </summary>
    abstract Json : ISurrealRestJsonClient with get

    /// <summary>
    /// Gets the key-value API client. Use this to perform operations where you have classes to represent the data.
    /// </summary>
    abstract KeyValue : ISurrealRestKeyValueClient with get

type SurrealRestClient(config, httpClient, ?jsonOptions) =
    do Endpoints.applyConfig config httpClient

    let jsonOptions =
        jsonOptions
        |> Option.defaultValue SurrealConfig.defaultJsonOptions

    let jsonClient : ISurrealRestJsonClient =
        SurrealRestJsonClient(httpClient, jsonOptions)

    let keyValueClient : ISurrealRestKeyValueClient =
        SurrealRestKeyValueClient(jsonClient, jsonOptions)

    interface ISurrealRestClient with
        member this.Json = jsonClient
        member this.KeyValue = keyValueClient

    interface System.IDisposable with
        member this.Dispose() = httpClient.Dispose()
