module SurrealDB.Client.FSharp.Rest.Endpoints

open System
open System.Net.Http

open SurrealDB.Client.FSharp

/// <summary>
/// Updates the default headers of a given HTTP client, to match the given configuration.
/// </summary>
let applyConfig config httpClient =
    updateDefaultHeader ACCEPT_HEADER APPLICATION_JSON httpClient

    match config.credentials with
    | ValueSome credentials -> applyCredentialHeaders credentials httpClient
    | ValueNone -> ()

    match config.ns with
    | ValueSome ns -> updateDefaultHeader NS_HEADER ns httpClient
    | ValueNone -> ()

    match config.db with
    | ValueSome db -> updateDefaultHeader DB_HEADER db httpClient
    | ValueNone -> ()

    httpClient.BaseAddress <- Uri(config.baseUrl, UriKind.Absolute)

/// <summary>
/// The SQL endpoint enables advanced SurrealQL queries.
/// </summary>
/// <see href="https://surrealdb.com/docs/integration/http#sql"/>
/// <param name="query">The query to execute.</param>
/// <param name="ct">The cancellation token.</param>
/// <param name="httpClient">The HTTP client to use.</param>
/// <returns>The records as a JSON Node.</returns>
/// <remarks>
/// Assumes the HTTP client has the correct headers already set.
/// </remarks>
let postSql jsonOptions query ct httpClient =
    executeTextRequest jsonOptions HttpMethod.Post "sql" query ct httpClient

/// <summary>
/// This HTTP RESTful endpoint selects all records in a specific table in the database.
/// </summary>
/// <see href="https://surrealdb.com/docs/integration/http#select-all"/>
/// <param name="table">The table to create the record in.</param>
/// <param name="ct">The cancellation token.</param>
/// <param name="httpClient">The HTTP client to use.</param>
/// <returns>The records as a JSON Node.</returns>
/// <remarks>
/// Assumes the HTTP client has the correct headers already set.
/// </remarks>
let getKeyTable jsonOptions table ct httpClient =
    executeEmptyRequest jsonOptions HttpMethod.Get $"key/%s{table}" ct httpClient

/// <summary>
/// This HTTP RESTful endpoint creates a record in a specific table in the database.
/// </summary>
/// <see href="https://surrealdb.com/docs/integration/http#create-all"/>
/// <param name="table">The table to create the record in.</param>
/// <param name="record">The record to create as a JSON Node.</param>
/// <param name="ct">The cancellation token.</param>
/// <param name="httpClient">The HTTP client to use.</param>
/// <returns>The created record as a JSON Node.</returns>
/// <remarks>
/// Assumes the HTTP client has the correct headers already set.
/// </remarks>
let postKeyTable jsonOptions table record ct httpClient =
    executeJsonRequest jsonOptions HttpMethod.Post $"key/%s{table}" record ct httpClient

/// <summary>
/// This HTTP RESTful endpoint deletes all records from the specified table in the database.
/// </summary>
/// <see href="https://surrealdb.com/docs/integration/http#delete-all"/>
/// <param name="table">The table to delete all records from.</param>
/// <param name="ct">The cancellation token.</param>
/// <param name="httpClient">The HTTP client to use.</param>
/// <returns>An empty array as a JSON Node.</returns>
/// <remarks>
/// Assumes the HTTP client has the correct headers already set.
/// </remarks>
let deleteKeyTable jsonOptions table ct httpClient =
    executeEmptyRequest jsonOptions HttpMethod.Delete $"key/%s{table}" ct httpClient

/// <summary>
/// This HTTP RESTful endpoint selects a specific record from the database.
/// </summary>
/// <see href="https://surrealdb.com/docs/integration/http#select-one"/>
/// <param name="table">The table to select the record from.</param>
/// <param name="id">The id of the record to select.</param>
/// <param name="ct">The cancellation token.</param>
/// <param name="httpClient">The HTTP client to use.</param>
/// <returns>An array with the record (or empty) as a JSON Node.</returns>
/// <remarks>
/// Assumes the HTTP client has the correct headers already set.
/// </remarks>
let getKeyTableId jsonOptions table id ct httpClient =
    executeEmptyRequest jsonOptions HttpMethod.Get $"key/%s{table}/%s{id}" ct httpClient

/// <summary>
/// This HTTP RESTful endpoint creates a single specific record into the database.
/// </summary>
/// <see href="https://surrealdb.com/docs/integration/http#create-one"/>
/// <param name="table">The table to create the record in.</param>
/// <param name="id">The id of the record to create.</param>
/// <param name="record">The record to create as a JSON Node.</param>
/// <param name="ct">The cancellation token.</param>
/// <param name="httpClient">The HTTP client to use.</param>
/// <returns>The created record as a JSON Node.</returns>
/// <remarks>
/// Assumes the HTTP client has the correct headers already set.
/// </remarks>
let postKeyTableId jsonOptions table id record ct httpClient =
    executeJsonRequest jsonOptions HttpMethod.Post $"key/%s{table}/%s{id}" record ct httpClient

/// <summary>
/// This HTTP RESTful endpoint creates or updates a single specific record in the database.
/// </summary>
/// <see href="https://surrealdb.com/docs/integration/http#update-one"/>
/// <param name="table">The table to create or update the record in.</param>
/// <param name="id">The id of the record to create or update.</param>
/// <param name="record">The record to create or update as a JSON Node.</param>
/// <param name="ct">The cancellation token.</param>
/// <param name="httpClient">The HTTP client to use.</param>
/// <returns>The created or updated record as a JSON Node.</returns>
/// <remarks>
/// Assumes the HTTP client has the correct headers already set.
/// </remarks>
let putKeyTableId jsonOptions table id =
    executeJsonRequest jsonOptions HttpMethod.Put $"key/%s{table}/%s{id}"

/// <summary>
/// This HTTP RESTful endpoint creates or updates a single specific record in the database.
/// If the record already exists, then only the specified fields will be updated.
/// </summary>
/// <see href="https://surrealdb.com/docs/integration/http#modify-one"/>
/// <param name="table">The table to update the record in.</param>
/// <param name="id">The id of the record to update.</param>
/// <param name="record">The partial record to update as a JSON Node.</param>
/// <param name="ct">The cancellation token.</param>
/// <param name="httpClient">The HTTP client to use.</param>
/// <returns>The updated record as a JSON Node.</returns>
/// <remarks>
/// Assumes the HTTP client has the correct headers already set.
/// </remarks>
let patchKeyTableId jsonOptions table id record ct httpClient =
    executeJsonRequest jsonOptions HttpMethod.Patch $"key/%s{table}/%s{id}" record ct httpClient

/// <summary>
/// This HTTP RESTful endpoint deletes a single specific record from the database.
/// </summary>
/// <see href="https://surrealdb.com/docs/integration/http#delete-one"/>
/// <param name="table">The table to delete the record from.</param>
/// <param name="id">The id of the record to delete.</param>
/// <param name="ct">The cancellation token.</param>
/// <param name="httpClient">The HTTP client to use.</param>
/// <returns>An empty array as a JSON Node.</returns>
/// <remarks>
/// Assumes the HTTP client has the correct headers already set.
/// </remarks>
let deleteKeyTableId jsonOptions table id ct httpClient =
    executeEmptyRequest jsonOptions HttpMethod.Delete $"key/%s{table}/%s{id}" ct httpClient
