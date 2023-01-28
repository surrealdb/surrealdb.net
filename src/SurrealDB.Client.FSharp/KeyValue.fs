namespace SurrealDB.Client.FSharp

open System.Net.Http
open System.Text.Json
open System.Text.Json.Nodes
open System.Threading
open System.Threading.Tasks

[<Struct>]
type SurrealResult<'a> = { result: 'a; info: ResponseInfo }

type ISurrealKeyValueClient =
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

    abstract DeleteAllAsync :
        table: string * cancellationToken: CancellationToken -> Task<Result<SurrealResult<unit>, SurrealError>>

    abstract DeleteAsync :
        table: string * id: string * cancellationToken: CancellationToken ->
        Task<Result<SurrealResult<unit>, SurrealError>>

type RestApiSurrealKeyValueClient(httpClient: HttpClient, config: SurrealConfig) =
    do httpClient |> RestApi.applyConfig config

    new(config) = RestApiSurrealKeyValueClient(new HttpClient(), config)

    member private this.OfJsonResult<'result>(result: Task<Result<struct (JsonNode * ResponseInfo), SurrealError>>) =
        task {
            match! result with
            | Error err -> return Error err
            | Ok (struct (json, info)) ->
                try
                    let result = json.Deserialize<'result>()
                    return Ok { result = result; info = info }
                with
                | :? JsonException as ex -> return Error(DeserializeError ex)
        }

    member private this.OfJsonEmptyResult(result: Task<Result<struct (JsonNode * ResponseInfo), SurrealError>>) =
        task {
            match! result with
            | Error err -> return Error err
            | Ok (struct (_json, info)) -> return Ok { result = (); info = info }
        }

    member private this.OfOptionJsonResult<'result>
        (result: Task<Result<struct (JsonNode * ResponseInfo), SurrealError>>)
        =
        task {
            let! result = this.OfJsonResult<'result []> result

            return
                result
                |> Result.map (fun r ->
                    { result = r.result |> Array.tryHead |> ValueOption.ofOption
                      info = r.info })
        }

    member this.GetAllAsync<'record>(table: string, cancellationToken: CancellationToken) =
        this.OfJsonResult<'record []>(RestApi.Json.getKeyTable table cancellationToken httpClient)

    member this.GetAsync<'record>(table: string, id: string, cancellationToken: CancellationToken) =
        this.OfOptionJsonResult<'record>(RestApi.Json.getKeyTableId table id cancellationToken httpClient)

    member this.CreateAsync<'record>(table: string, record: 'record, cancellationToken: CancellationToken) =
        task {
            let json = JsonSerializer.SerializeToNode(record)
            return! this.OfOptionJsonResult<'record>(RestApi.Json.postKeyTable table json cancellationToken httpClient)
        }

    member this.CreateAsync<'record>(table: string, id: string, record: 'record, cancellationToken: CancellationToken) =
        task {
            let json = JsonSerializer.SerializeToNode(record)

            return!
                this.OfOptionJsonResult<'record>(RestApi.Json.postKeyTableId table id json cancellationToken httpClient)
        }

    member this.DeleteAllAsync(table: string, cancellationToken: CancellationToken) =
        this.OfJsonEmptyResult(RestApi.Json.deleteKeyTable table cancellationToken httpClient)

    member this.DeleteAsync(table: string, id: string, cancellationToken: CancellationToken) =
        this.OfJsonEmptyResult(RestApi.Json.deleteKeyTableId table id cancellationToken httpClient)

    interface ISurrealKeyValueClient with
        member this.GetAllAsync<'record>(table, cancellationToken) =
            this.GetAllAsync<'record>(table, cancellationToken)

        member this.GetAsync<'record>(table, id, cancellationToken) =
            this.GetAsync<'record>(table, id, cancellationToken)

        member this.CreateAsync<'record>(table, record, cancellationToken) =
            this.CreateAsync<'record>(table, record, cancellationToken)

        member this.CreateAsync<'record>(table, id, record, cancellationToken) =
            this.CreateAsync<'record>(table, id, record, cancellationToken)

        member this.DeleteAllAsync(table, cancellationToken) =
            this.DeleteAllAsync(table, cancellationToken)

        member this.DeleteAsync(table, id, cancellationToken) =
            this.DeleteAsync(table, id, cancellationToken)
