namespace SurrealDB.Client.FSharp.Rest

open System.Net
open System.Net.Http
open System.Text.Json.Nodes
open System.Threading
open System.Threading.Tasks

open SurrealDB.Client.FSharp

type ISurrealRestJsonClient =
    abstract CreateAsync :
        table: string * record: JsonNode * cancellationToken: CancellationToken -> Task<RestApiResult<JsonNode>>

    abstract CreateAsync :
        table: string * id: string * record: JsonNode * cancellationToken: CancellationToken ->
        Task<RestApiResult<JsonNode>>

    abstract DeleteAllAsync : table: string * cancellationToken: CancellationToken -> Task<RestApiResult<JsonNode>>

    abstract DeleteAsync :
        table: string * id: string * cancellationToken: CancellationToken -> Task<RestApiResult<JsonNode>>

    abstract GetAllAsync : table: string * cancellationToken: CancellationToken -> Task<RestApiResult<JsonNode>>

    abstract GetAsync :
        table: string * id: string * cancellationToken: CancellationToken -> Task<RestApiResult<JsonNode>>

    abstract ModifyAsync :
        table: string * id: string * record: JsonNode * cancellationToken: CancellationToken ->
        Task<RestApiResult<JsonNode>>

    abstract UpdateAsync :
        table: string * id: string * record: JsonNode * cancellationToken: CancellationToken ->
        Task<RestApiResult<JsonNode>>

    abstract SqlAsync : query: string * cancellationToken: CancellationToken -> Task<RestApiResult<JsonNode>>

type internal SurrealRestJsonClient(config: SurrealConfig, httpClient: HttpClient) =
     interface ISurrealRestJsonClient with
        member this.CreateAsync(table, record, cancellationToken) =
            Endpoints.postKeyTable table record cancellationToken httpClient

        member this.CreateAsync(table, id, record, cancellationToken) =
            Endpoints.postKeyTableId table id record cancellationToken httpClient

        member this.DeleteAllAsync(table, cancellationToken) =
            Endpoints.deleteKeyTable table cancellationToken httpClient

        member this.DeleteAsync(table, id, cancellationToken) =
            Endpoints.deleteKeyTableId table id cancellationToken httpClient

        member this.GetAllAsync(table, cancellationToken) =
            Endpoints.getKeyTable table cancellationToken httpClient

        member this.GetAsync(table, id, cancellationToken) =
            Endpoints.getKeyTableId table id cancellationToken httpClient

        member this.ModifyAsync(table, id, record, cancellationToken) =
            Endpoints.patchKeyTableId table id record cancellationToken httpClient

        member this.SqlAsync(query, cancellationToken) =
            Endpoints.postSql query cancellationToken httpClient

        member this.UpdateAsync(table, id, record, cancellationToken) =
            Endpoints.putKeyTableId table id record cancellationToken httpClient
