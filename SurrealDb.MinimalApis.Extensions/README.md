# SurrealDb.MinimalApis.Extensions

A set of extensions and helpers to use SurrealDB with ASP.NET Core Minimal APIs.

## Documentation

View the SDK documentation [here](https://surrealdb.com/docs/integration/libraries/dotnet).

## How to install

```sh
dotnet add package SurrealDb.MinimalApis.Extensions
dotnet add package SurrealDb.Net
dotnet add package Microsoft.AspNetCore.OpenApi
```

## Getting started

The `MapSurrealEndpoints` extension method allows you to map API endpoints for an entity/table.

```csharp
app
  .MapGroup("/api")
  .MapSurrealEndpoints<WeatherForecast>(
    "/weatherForecast",
    new() { Tags = ["WeatherForecast"], EnableMutations = false }
  )
  .MapSurrealEndpoints<Todo>("/todo", new() { Tags = ["Todo"] });
```

This method will generate the following API endpoints:

| Endpoint     | Description                                    |
| ------------ | ---------------------------------------------- |
| GET /        | Select all records from a SurrealDB table.     |
| GET /{id}    | Select a single record from a SurrealDB table. |
| POST /       | Create a new record in a SurrealDB table.      |
| PUT /        | Update a record in a SurrealDB table.          |
| PATCH /      | Patch all records in a SurrealDB table.        |
| PATCH /{id}  | Patch a single record in a SurrealDB table.    |
| DELETE /     | Delete all records from a SurrealDB table.     |
| DELETE /{id} | Delete a single record from a SurrealDB table. |

You can choose to disable some of the API endpoints via the options pattern.
