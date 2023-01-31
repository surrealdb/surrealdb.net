namespace SurrealDB.Client.FSharp.Rest

open System.Net.Http
open System.Text.Json
open System.Text.Json.Nodes
open System.Threading
open System.Threading.Tasks

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

type internal SurrealRestJsonClient(httpClient: HttpClient, jsonOptions: JsonSerializerOptions) =
     interface ISurrealRestJsonClient with
        member this.CreateAsync(table, record, cancellationToken) =
            Endpoints.postKeyTable jsonOptions table record cancellationToken httpClient

        member this.CreateAsync(table, id, record, cancellationToken) =
            Endpoints.postKeyTableId jsonOptions table id record cancellationToken httpClient

        member this.DeleteAllAsync(table, cancellationToken) =
            Endpoints.deleteKeyTable jsonOptions table cancellationToken httpClient

        member this.DeleteAsync(table, id, cancellationToken) =
            Endpoints.deleteKeyTableId jsonOptions table id cancellationToken httpClient

        member this.GetAllAsync(table, cancellationToken) =
            Endpoints.getKeyTable jsonOptions table cancellationToken httpClient

        member this.GetAsync(table, id, cancellationToken) =
            Endpoints.getKeyTableId jsonOptions table id cancellationToken httpClient

        member this.ModifyAsync(table, id, record, cancellationToken) =
            Endpoints.patchKeyTableId jsonOptions table id record cancellationToken httpClient

        member this.UpdateAsync(table, id, record, cancellationToken) =
            Endpoints.putKeyTableId jsonOptions table id record cancellationToken httpClient

        member this.SqlAsync(query, cancellationToken) =
            Endpoints.postSql jsonOptions query cancellationToken httpClient
