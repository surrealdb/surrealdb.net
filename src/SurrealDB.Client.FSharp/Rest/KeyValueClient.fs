namespace SurrealDB.Client.FSharp.Rest

open System.Net.Http
open System.Threading
open System.Threading.Tasks

open SurrealDB.Client.FSharp

type ISurrealRestKeyValueClient =
    abstract CreateAsync<'record> :
        table: string * record: 'record * cancellationToken: CancellationToken -> Task<Result<'record, RequestError>>

    abstract CreateAsync<'record> :
        table: string * id: string * record: 'record * cancellationToken: CancellationToken ->
        Task<Result<'record, RequestError>>

    abstract DeleteAllAsync : table: string * cancellationToken: CancellationToken -> Task<Result<unit, RequestError>>

    abstract DeleteAsync :
        table: string * id: string * cancellationToken: CancellationToken -> Task<Result<unit, RequestError>>

    abstract GetAllAsync<'record> :
        table: string * cancellationToken: CancellationToken -> Task<Result<'record [], RequestError>>

    abstract GetAsync<'record> :
        table: string * id: string * cancellationToken: CancellationToken -> Task<Result<'record, RequestError>>

    abstract ModifyAsync<'record> :
        table: string * id: string * record: 'record * cancellationToken: CancellationToken ->
        Task<Result<'record, RequestError>>

    abstract UpdateAsync<'record> :
        table: string * id: string * record: 'record * cancellationToken: CancellationToken ->
        Task<Result<'record, RequestError>>

type internal SurrealRestKeyValueClient(jsonClient: ISurrealRestJsonClient, jsonOptions) =
    interface ISurrealRestKeyValueClient with
        member this.CreateAsync<'record>(table, record, cancellationToken) =
            task {
                let record = Json.serializeNode<'record> jsonOptions record
                let! response = jsonClient.CreateAsync(table, record, cancellationToken)
                return parseOneItem<'record> jsonOptions response
            }

        member this.CreateAsync<'record>(table, id, record, cancellationToken) =
            task {
                let record = Json.serializeNode<'record> jsonOptions record
                let! response = jsonClient.CreateAsync(table, id, record, cancellationToken)
                return parseOneItem<'record> jsonOptions response
            }

        member this.DeleteAllAsync(table, cancellationToken) =
            task {
                let! response = jsonClient.DeleteAllAsync(table, cancellationToken)
                return parseNoItems response
            }

        member this.DeleteAsync(table, id, cancellationToken) =
            task {
                let! response = jsonClient.DeleteAsync(table, id, cancellationToken)
                return parseNoItems response
            }

        member this.GetAllAsync<'record>(table, cancellationToken) =
            task {
                let! response = jsonClient.GetAllAsync(table, cancellationToken)
                return parseManyItems<'record> jsonOptions response
            }

        member this.GetAsync<'record>(table, id, cancellationToken) =
            task {
                let! response = jsonClient.GetAsync(table, id, cancellationToken)
                return parseOneItem<'record> jsonOptions response
            }

        member this.ModifyAsync<'record>(table, id, record, cancellationToken) =
            task {
                let record = Json.serializeNode<'record> jsonOptions record
                let! response = jsonClient.ModifyAsync(table, id, record, cancellationToken)
                return parseOneItem<'record> jsonOptions response
            }

        member this.UpdateAsync<'record>(table, id, record, cancellationToken) =
            task {
                let record = Json.serializeNode<'record> jsonOptions record
                let! response = jsonClient.UpdateAsync(table, id, record, cancellationToken)
                return parseOneItem<'record> jsonOptions response
            }
