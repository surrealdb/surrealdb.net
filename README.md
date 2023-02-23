# surrealdb.net

The official SurrealDB libraries for .NET

[![](https://img.shields.io/badge/status-beta-ff00bb.svg?style=flat-square)](https://github.com/surrealdb/surrealdb.net) [![](https://img.shields.io/badge/docs-view-44cc11.svg?style=flat-square)](https://surrealdb.com/docs/integration/libraries/dotnet) [![](https://img.shields.io/badge/license-Apache_License_2.0-00bfff.svg?style=flat-square)](https://github.com/surrealdb/surrealdb.net)

This library provides a simple way to connect to a [SurrealDB](https://surrealdb.com) server from .NET applications.

Currently only the F# library is available. The C# library will be available soon, but in the meantime you can use the non-official [C# library](https://github.com/Surreal-Net/Surreal.Net).

## Quickstart

### Install the package

```bash
> dotnet add package SurrealDB.Client.FSharp
```

### Configure how to connect to the server

```fsharp
open SurrealDB.Client.FSharp

let configResult : Result<SurrealConfig, SurrealConfigError list> =
    SurrealConfig
        .Builder()
        .WithBaseUrl("http://localhost:8010")
        .WithBasicCredentials("root", "root")
        .WithNamespace("testns")
        .WithDatabase("testdb")
        .Build()

let config =
    match configResult with
    | Ok config -> config
    | Error errors ->
        failwithf "Invalid configuration: %A" errors
```

Then you need to decide which API to use. The server exposes a [REST API](https://surrealdb.com/docs/integration/http) with access to a SQL and a Tables endpoints. It also exposes a [WebSocket API](https://surrealdb.com/docs/integration/websockets) which is not officially documented yet, and therefore not supported by this library yet.

Even in REST, you need to choose if you are going to work with a freeform JSON API with `SurrealJsonClient`, with a strongly typed Table API with `SurrealTableClient`, or with a strongly typed SQL API with `SurrealSqlClient` (not yet implemented).

### Use the JSON API

```fsharp
use httpClient = new HttpClient()
let jsonOptions = Json.defaultOptions
let ct = CancellationToken.None
let endpoints: ISurrealJsonClient =
    new SurrealJsonClient(config, httpClient, jsonOptions)

type Person =
    { id: string
      firstName: string
      lastName: string
      age: int }
```

#### Create a new record

```fsharp
let john : Person =
    { firstName = "John"
      lastName = "Doe"
      age = 24
      id = "" }

let johnJson : JsonNode =
    JsonSerializer.SerializeToNode(john, jsonOptions)

let! createJohnResult : RestApiResult<JsonNode> =
    endpoints.InsertAsync("people", "john", johnJson, ct)
```

All responses have a headers section, which in this case you can print like:

```fsharp
printfn "Create John result headers:\n%A" createJohnResult.headers
```

```text
Create John result headers:
{ version = "surreal-1.0.0-beta.8+20220930.c246533"
  server = "SurrealDB"
  status = OK
  date = "Thu, 23 Feb 2023 17:43:00 GMT" }
```

The result follows the specification of the [SurrealDB REST API](https://surrealdb.com/docs/integration/http), so there are different levels of errors. You have to check the different cases like:

```fsharp
match createJohnResult.statements with
| Ok statements ->
    match statements.[0].response with
    | Ok jsonNode ->
        printfn "Create John result:\n%A" (jsonNode.ToString())
    | Error err ->
        printfn "Create John statement error:\n%A" err
| Error err ->
    printfn "Create John request error:\n%A" err
```

On a clean database, the result will be:

```text
Create John result:
"[
  {
    "age": 24,
    "firstName": "John",
    "id": "people:john",
    "lastName": "Doe"
  }
]"
```

This JSON API is very low level for most use cases, so you probably want to use the Table API and the SQL API instead. But if you need access to the raw SurrealDB JSON API, you can use the `SurrealJsonClient` class.

## Contributing

Clone the repository and open the solution in Visual Studio or use the `dotnet` CLI.

The file `docker-compose.yml` contains a configuration for a local SurrealDB server, exposed through port 8010. You can use it to run local samples.

To run unit tests, you need to run `> dotnet test --filter Category=UnitTest`. No SurrealDB server is required.

To run integration tests, you need to run `> dotnet test --filter Category=IntegrationTest`. A SurrealDB server is automatically started and stopped during the tests. A new instance of the server is used for each test, so you can run the tests in parallel. However for the moment being, the integration tests are run sequentially, because I haven't found a way to run [Testcontainers](https://dotnet.testcontainers.org/) in parallel.
