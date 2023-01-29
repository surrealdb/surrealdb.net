namespace SurrealDB.Client.FSharp

open System.Net.Http
open System.Text.Json
open System.Text.Json.Nodes
open System.Threading
open System.Threading.Tasks

[<Struct>]
type SurrealResult<'a> = { result: 'a; info: ResponseInfo }

[<RequireQualifiedAccess>]
module SurrealResult =
    let mapResult (f: 'a -> 'b) ma =
        { result = f ma.result; info = ma.info }

    let ofRaw (struct (json, info)) = { result = json; info = info }

type ISurrealKeyValueJsonClient =
    abstract GetAllAsync :
        table: string * cancellationToken: CancellationToken -> Task<Result<SurrealResult<JsonNode>, SurrealError>>

    abstract GetAsync :
        table: string * id: string * cancellationToken: CancellationToken ->
        Task<Result<SurrealResult<JsonNode>, SurrealError>>

    abstract CreateAsync :
        table: string * record: JsonNode * cancellationToken: CancellationToken ->
        Task<Result<SurrealResult<JsonNode>, SurrealError>>

    abstract CreateAsync :
        table: string * id: string * record: JsonNode * cancellationToken: CancellationToken ->
        Task<Result<SurrealResult<JsonNode>, SurrealError>>

    abstract UpdateAsync :
        table: string * id: string * record: JsonNode * cancellationToken: CancellationToken ->
        Task<Result<SurrealResult<JsonNode>, SurrealError>>

    abstract ModifyAsync :
        table: string * id: string * record: JsonNode * cancellationToken: CancellationToken ->
        Task<Result<SurrealResult<JsonNode>, SurrealError>>

    abstract DeleteAllAsync :
        table: string * cancellationToken: CancellationToken -> Task<Result<SurrealResult<JsonNode>, SurrealError>>

    abstract DeleteAsync :
        table: string * id: string * cancellationToken: CancellationToken ->
        Task<Result<SurrealResult<JsonNode>, SurrealError>>

    abstract SqlAsync :
        query: string * cancellationToken: CancellationToken -> Task<Result<SurrealResult<JsonNode>, SurrealError>>

type ISurrealKeyValueClient =
    inherit ISurrealKeyValueJsonClient

    abstract GetAllAsync<'record> :
        table: string * cancellationToken: CancellationToken -> Task<Result<SurrealResult<'record []>, SurrealError>>

    abstract GetAsync<'record> :
        table: string * id: string * cancellationToken: CancellationToken ->
        Task<Result<SurrealResult<'record voption>, SurrealError>>

    abstract CreateAsync<'record> :
        table: string * record: 'record * cancellationToken: CancellationToken ->
        Task<Result<SurrealResult<'record voption>, SurrealError>>

    abstract CreateAsync<'record> :
        table: string * id: string * record: 'record * cancellationToken: CancellationToken ->
        Task<Result<SurrealResult<'record voption>, SurrealError>>

    abstract UpdateAsync<'record> :
        table: string * id: string * record: 'record * cancellationToken: CancellationToken ->
        Task<Result<SurrealResult<'record voption>, SurrealError>>

    abstract ModifyAsync<'record> :
        table: string * id: string * record: 'record * cancellationToken: CancellationToken ->
        Task<Result<SurrealResult<'record voption>, SurrealError>>

    abstract DeleteAllAsync :
        table: string * cancellationToken: CancellationToken -> Task<Result<SurrealResult<unit>, SurrealError>>

    abstract DeleteAsync :
        table: string * id: string * cancellationToken: CancellationToken ->
        Task<Result<SurrealResult<unit>, SurrealError>>

    abstract SqlIgnoreAsync :
        query: string * cancellationToken: CancellationToken -> Task<Result<SurrealResult<unit>, SurrealError>>

    abstract UseAsync : ns: string * db: string * cancellationToken: CancellationToken -> Task<Result<SurrealResult<unit>, SurrealError>>
    abstract UseNamespaceAsync : ns: string * cancellationToken: CancellationToken -> Task<Result<SurrealResult<unit>, SurrealError>>
    abstract UseDatabaseAsync : db: string * cancellationToken: CancellationToken -> Task<Result<SurrealResult<unit>, SurrealError>>

type internal RestApiSurrealKeyValueJsonClient(httpClient: HttpClient, config: SurrealConfig) =
    let execute input requester =
        task {
            let! response = requester input
            return
                response
                |> Result.map SurrealResult.ofRaw
        }

    interface ISurrealKeyValueJsonClient with
        member this.GetAllAsync(table, cancellationToken) =
            execute () (fun _ -> RestApi.Json.getKeyTable table cancellationToken httpClient)

        member this.GetAsync(table, id, cancellationToken) =
            execute () (fun _ -> RestApi.Json.getKeyTableId table id cancellationToken httpClient)


        member this.CreateAsync(table, record, cancellationToken) =
            execute record (fun record -> RestApi.Json.postKeyTable table record cancellationToken httpClient)

        member this.CreateAsync(table, id, record, cancellationToken) =
            execute record (fun record -> RestApi.Json.postKeyTableId table id record cancellationToken httpClient)

        member this.ModifyAsync(table, id, record, cancellationToken) =
            execute record (fun record -> RestApi.Json.patchKeyTableId table id record cancellationToken httpClient)

        member this.UpdateAsync(table, id, record, cancellationToken) =
            execute record (fun record -> RestApi.Json.putKeyTableId table id record cancellationToken httpClient)

        member this.DeleteAllAsync(table, cancellationToken) =
            execute () (fun _ -> RestApi.Json.deleteKeyTable table cancellationToken httpClient)

        member this.DeleteAsync(table, id, cancellationToken) =
            execute () (fun _ -> RestApi.Json.deleteKeyTableId table id cancellationToken httpClient)

        member this.SqlAsync(query, cancellationToken) =
            execute () (fun _ -> RestApi.Json.postSql query cancellationToken httpClient)

type internal RestApiSurrealKeyValueClient(httpClient: HttpClient, config: SurrealConfig) =
    inherit RestApiSurrealKeyValueJsonClient(httpClient, config)

    interface ISurrealKeyValueClient with
        member this.GetAllAsync<'record>(table, cancellationToken) =
            (this :> ISurrealKeyValueJsonClient).GetAllAsync(table, cancellationToken)
            |> TaskResult.map (SurrealResult.mapResult (Json.deserializeNode<'record []>))

        member this.GetAsync<'record>(table, id, cancellationToken) =
            (this :> ISurrealKeyValueJsonClient).GetAsync(table, id, cancellationToken)
            |> TaskResult.map (SurrealResult.mapResult (Json.deserializeNode<'record []> >> Array.tryHeadValue))

        member this.CreateAsync<'record>(table, record, cancellationToken) =
            (this :> ISurrealKeyValueJsonClient).CreateAsync(table, (Json.serializeNode<'record> record), cancellationToken)
            |> TaskResult.map (SurrealResult.mapResult (Json.deserializeNode<'record []> >> Array.tryHeadValue))

        member this.CreateAsync<'record>(table, id, record, cancellationToken) =
            (this :> ISurrealKeyValueJsonClient).CreateAsync(table, id, (Json.serializeNode<'record> record), cancellationToken)
            |> TaskResult.map (SurrealResult.mapResult (Json.deserializeNode<'record []> >> Array.tryHeadValue))

        member this.ModifyAsync<'record>(table, id, record, cancellationToken) =
            (this :> ISurrealKeyValueJsonClient).ModifyAsync(table, id, (Json.serializeNode<'record> record), cancellationToken)
            |> TaskResult.map (SurrealResult.mapResult (Json.deserializeNode<'record []> >> Array.tryHeadValue))

        member this.UpdateAsync<'record>(table, id, record, cancellationToken) =
            (this :> ISurrealKeyValueJsonClient).UpdateAsync(table, id, (Json.serializeNode<'record> record), cancellationToken)
            |> TaskResult.map (SurrealResult.mapResult (Json.deserializeNode<'record []> >> Array.tryHeadValue))

        member this.DeleteAllAsync(table, cancellationToken) =
            (this :> ISurrealKeyValueJsonClient).DeleteAllAsync(table, cancellationToken)
            |> TaskResult.map (SurrealResult.mapResult (fun _ -> ()))

        member this.DeleteAsync(table, id, cancellationToken) =
            (this :> ISurrealKeyValueJsonClient).DeleteAsync(table, id, cancellationToken)
            |> TaskResult.map (SurrealResult.mapResult (fun _ -> ()))

        member this.SqlIgnoreAsync(query, cancellationToken) =
            (this :> ISurrealKeyValueJsonClient).SqlAsync(query, cancellationToken)
            |> TaskResult.map (SurrealResult.mapResult (fun _ -> ()))

        member this.UseAsync(ns, db, cancellationToken) =
            (this :> ISurrealKeyValueJsonClient).SqlAsync(sprintf "USE NS %s DB %s;" ns db, cancellationToken)
            |> TaskResult.map (SurrealResult.mapResult (fun _ -> ()))

        member this.UseNamespaceAsync(ns, cancellationToken) =
            (this :> ISurrealKeyValueJsonClient).SqlAsync(sprintf "USE NS %s;" ns, cancellationToken)
            |> TaskResult.map (SurrealResult.mapResult (fun _ -> ()))

        member this.UseDatabaseAsync(db, cancellationToken) =
            (this :> ISurrealKeyValueJsonClient).SqlAsync(sprintf "USE DB %s;" db, cancellationToken)
            |> TaskResult.map (SurrealResult.mapResult (fun _ -> ()))



