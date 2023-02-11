namespace SurrealDB.Client.FSharp.Rest

open System.Net.Http
open System.Text.Json
open System.Text.Json.Nodes
open System.Threading
open System.Threading.Tasks

type ISurrealRestJsonClient =
    abstract CreateAsync :
        table: string * record: JsonNode * cancellationToken: CancellationToken -> Task<RestApiResult>

    abstract CreateAsync :
        table: string * id: string * record: JsonNode * cancellationToken: CancellationToken -> Task<RestApiResult>

    abstract DeleteAllAsync : table: string * cancellationToken: CancellationToken -> Task<RestApiResult>

    abstract DeleteAsync : table: string * id: string * cancellationToken: CancellationToken -> Task<RestApiResult>

    abstract GetAllAsync : table: string * cancellationToken: CancellationToken -> Task<RestApiResult>

    abstract GetAsync : table: string * id: string * cancellationToken: CancellationToken -> Task<RestApiResult>

    abstract InsertOrUpdateAsync :
        table: string * id: string * record: JsonNode * cancellationToken: CancellationToken -> Task<RestApiResult>

    abstract PatchAsync :
        table: string * id: string * record: JsonNode * cancellationToken: CancellationToken -> Task<RestApiResult>

    abstract SqlAsync : query: string * cancellationToken: CancellationToken -> Task<RestApiResult>

type internal SurrealRestJsonClient(httpClient: HttpClient, jsonOptions: JsonSerializerOptions) =
    interface ISurrealRestJsonClient with
        member this.CreateAsync(table, record, cancellationToken) =
            Endpoints.postKeyTable jsonOptions httpClient cancellationToken table record

        member this.CreateAsync(table, id, record, cancellationToken) =
            Endpoints.postKeyTableId jsonOptions httpClient cancellationToken table id record

        member this.DeleteAllAsync(table, cancellationToken) =
            Endpoints.deleteKeyTable jsonOptions httpClient cancellationToken table

        member this.DeleteAsync(table, id, cancellationToken) =
            Endpoints.deleteKeyTableId jsonOptions httpClient cancellationToken table id

        member this.GetAllAsync(table, cancellationToken) =
            Endpoints.getKeyTable jsonOptions httpClient cancellationToken table

        member this.GetAsync(table, id, cancellationToken) =
            Endpoints.getKeyTableId jsonOptions httpClient cancellationToken table id

        member this.InsertOrUpdateAsync(table, id, record, cancellationToken) =
            Endpoints.putKeyTableId jsonOptions httpClient cancellationToken table id record

        member this.PatchAsync(table, id, record, cancellationToken) =
            Endpoints.patchKeyTableId jsonOptions httpClient cancellationToken table id record

        member this.SqlAsync(query, cancellationToken) =
            Endpoints.postSql jsonOptions httpClient cancellationToken query
