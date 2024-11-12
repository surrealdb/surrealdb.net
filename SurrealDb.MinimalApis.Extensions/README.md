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

## Contributing

### .NET release versions

The .NET release versions must follow these rules:

- Should target at least the latest LTS (Long-Term Support) version
- Should target at least the latest STS (Standard-Term Support) version

SurrealDb.Net targets .NET versions following the [.NET Support Policy by Microsoft](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core). Additionally, SurrealDb.Net targets .NET Standard 2.1 explicitly to continue support of the Mono runtime (Unity, Xamarin, etc...).

Note that the support for .NET standard 2.1 will be maintained until further notice.

| Version           | Description | Release Date      | End of Support    |
| ----------------- | ----------- | ----------------- | ----------------- |
| .NET Standard 2.1 |             | June 27, 2016     | N/A               |
| .NET 6            | LTS         | November 8, 2021  | November 12, 2024 |
| .NET 7            | STS         | November 8, 2022  | May 14, 2024      |
| .NET 8            | Current LTS | November 14, 2023 | November 10, 2026 |

### Formatting

This project is using [CSharpier](https://csharpier.com/), an opinionated code formatter.

#### Command line

You can install it on your machine via `dotnet tool`.

```sh
# Run this command at the root of the project
dotnet tool install csharpier
```

You can then use it as a cli:

```sh
dotnet csharpier .
```

The list of command-line options is available here: https://csharpier.com/docs/CLI

#### IDE integration

CSharpier supports [multiple code editors](https://csharpier.com/docs/Editors), including Visual Studio, Jetbrains Rider, VSCode and Neovim. You will be able to run format on file save after configuring the settings in your IDE.
