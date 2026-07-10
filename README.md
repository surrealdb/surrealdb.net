<br>

<p align="center">
    <img width=120 src="https://raw.githubusercontent.com/surrealdb/icons/main/surreal.svg" />
    &nbsp;
    <img width=120 src="https://raw.githubusercontent.com/surrealdb/icons/main/dotnet.svg" />
</p>

<h3 align="center">The official SurrealDB SDK for .NET.</h3>

<br>

<p align="center">
    <a href="https://github.com/surrealdb/surrealdb.net"><img src="https://img.shields.io/badge/status-beta-ff00bb.svg?style=flat-square"></a>
    &nbsp;
    <a href="https://surrealdb.com/docs/integration/libraries/dotnet"><img src="https://img.shields.io/badge/docs-view-44cc11.svg?style=flat-square"></a>
    &nbsp;
    <a href="https://www.nuget.org/packages/SurrealDb.Net"><img src="https://img.shields.io/nuget/v/surrealdb.net?style=flat-square"></a>
    &nbsp;
    <a href="https://www.nuget.org/packages/SurrealDb.Net"><img src="https://img.shields.io/nuget/dt/surrealdb?style=flat-square"></a>
    &nbsp;
    <a href="https://codecov.io/github/surrealdb/surrealdb.net"><img src="https://img.shields.io/codecov/c/github/surrealdb/surrealdb.net?style=flat-square"></a>
</p>

<p align="center">
    <a href="https://surrealdb.com/discord"><img src="https://img.shields.io/discord/902568124350599239?label=discord&style=flat-square&color=5a66f6"></a>
    &nbsp;
    <a href="https://twitter.com/surrealdb"><img src="https://img.shields.io/badge/twitter-follow_us-1d9bf0.svg?style=flat-square"></a>
    &nbsp;
    <a href="https://www.linkedin.com/company/surrealdb/"><img src="https://img.shields.io/badge/linkedin-connect_with_us-0a66c2.svg?style=flat-square"></a>
    &nbsp;
    <a href="https://www.youtube.com/channel/UCjf2teVEuYVvvVC-gFZNq6w"><img src="https://img.shields.io/badge/youtube-subscribe-fc1c1c.svg?style=flat-square"></a>
</p>

# surrealdb.net

The official SurrealDB SDK for .NET.

## Documentation

View the SDK documentation [here](https://surrealdb.com/docs/integration/libraries/dotnet).

## Features

- HTTP and WebSocket connections
- Embedded in-memory and persistent modes (Memory, RocksDB, SurrealKV)
- Dependency injection
- Authentication
- Live queries
- Client-side transactions

## How to install

```sh
dotnet add package SurrealDb.Net
```

## Getting started

This library supports connecting to SurrealDB over the remote HTTP and WebSocket connection protocols `http`, `https`, `ws`, and `wss`.

> The examples below require SurrealDB to be [installed](https://surrealdb.com/install) and running on port 8000.

### Constructing a new SurrealDB client

You can easily create a new SurrealDB client. All you have to do is define the `endpoint` to the SurrealDB instance.

```csharp
await using var clientHttp = new SurrealDbClient("http://127.0.0.1:8000");
await using var clientHttps = new SurrealDbClient("https://127.0.0.1:8000");
await using var clientWs = new SurrealDbClient("ws://127.0.0.1:8000/rpc");
await using var clientWss = new SurrealDbClient("wss://127.0.0.1:8000/rpc");

// Now you can call other methods including Signin & Use
```

### Dependency injection

You can use Dependency Injection with the `services.AddSurreal()` function.

#### Default instance

```csharp
var options = SurrealDbOptions
  .Create()
  .WithEndpoint("http://127.0.0.1:8000")
  .WithNamespace("test")
  .WithDatabase("test")
  .WithUsername("root")
  .WithPassword("root")
  .Build();

services.AddSurreal(options); // Access `SurrealDbSession` scoped to the request
```

Then you will be able to use the `ISurrealDbSession` interface or `SurrealDbSession` class anywhere. Note that you will need to use `SurrealDbSession` instead of `SurrealDbClient` because `SurrealDbClient` can only be used in a Singleton context, see below.

#### Lifetime compatibility

| Class              | Singleton | Scoped | Transient |
| ------------------ | --------- | ------ | --------- |
| `SurrealDbClient`  | ✅        | ❌     | ❌        |
| `SurrealDbSession` | ❌        | ✅     | ✅        |

```csharp
public class MyClass
{
  private readonly ISurrealDbSession _db;

  public MyClass(ISurrealDbSession db)
  {
    _db = db;
  }

  // ...
}
```

Note that the default lifetime of this service is `Scoped`. You can override this as follows:

```csharp
services.AddSurreal(options, ServiceLifetime.Scoped);
```

#### Connection String

Consider the following `appsettings.json` file:

```json
{
  "AllowedHosts": "*",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "SurrealDB": "Server=http://127.0.0.1:8000;Namespace=test;Database=test;Username=root;Password=root"
  }
}
```

You can use the Connection String instead of having to deal with a `SurrealDbOptions`.

```csharp
services.AddSurreal(configuration.GetConnectionString("SurrealDB"));
```

It will automatically create a new SurrealDB using the `Server endpoint` and configure the client using the different values for `namespace`, `database`, `username` and `password`. Note that these values are optional but the `endpoint` is still required.

#### Multiple instances

Having a default instance for a project is enough most of the time, but there may be times when you'd like to target multiple SurrealDB instances, either at different addresses or at the same address but inside different NS/DBs. You can use multiple instances using Keyed service injection, as in the following example.

```csharp
services.AddSurreal(configuration.GetConnectionString("SurrealDB.Main"));
services.AddKeyedSurreal("backup", configuration.GetConnectionString("SurrealDB.Backup"));
services.AddKeyedSurreal("monitoring", configuration.GetConnectionString("SurrealDB.Monitoring"));
```

Here you will have 3 instances:

- the default one, you can keep using `ISurrealDbSession` interface or `SurrealDbSession` class anywhere
- a client for backup purpose, using the `[FromKeyedServices("backup")]` attribute
- a client for monitoring purpose, using the `[FromKeyedServices("monitoring")]` attribute

### Use the client

```csharp
[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
  private const string Table = "weatherForecast";

  private readonly ISurrealDbSession _db;

  public WeatherForecastController(ISurrealDbSession db)
  {
    _db = db;
  }

  [HttpGet]
  public Task<IEnumerable<WeatherForecast>> GetAll(CancellationToken cancellationToken)
  {
    return _db.Select<WeatherForecast>(Table, cancellationToken);
  }

  [HttpGet("{id}")]
  public async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
  {
    var weatherForecast = await _db.Select<WeatherForecast>((Table, id), cancellationToken);

    if (weatherForecast is null)
      return NotFound();

    return Ok(weatherForecast);
  }

  [HttpPost]
  public Task<WeatherForecast> Create(CreateWeatherForecast data, CancellationToken cancellationToken)
  {
    var weatherForecast = new WeatherForecast
    {
      Date = data.Date,
      Country = data.Country,
      TemperatureC = data.TemperatureC,
      Summary = data.Summary
    };

    return _db.Create(Table, weatherForecast, cancellationToken);
  }

  [HttpPut]
  public Task<WeatherForecast> Update(WeatherForecast data, CancellationToken cancellationToken)
  {
    return _db.Upsert(data, cancellationToken);
  }

  [HttpPatch]
  public Task<IEnumerable<WeatherForecast>> PatchAll(
    JsonPatchDocument<WeatherForecast> patches,
    CancellationToken cancellationToken
  )
  {
    return _db.Patch(Table, patches, cancellationToken);
  }

  [HttpPatch("{id}")]
  public Task<WeatherForecast> Patch(
    string id,
    JsonPatchDocument<WeatherForecast> patches,
    CancellationToken cancellationToken
  )
  {
    return _db.Patch((Table, id), patches, cancellationToken);
  }

  [HttpDelete]
  public Task DeleteAll(CancellationToken cancellationToken)
  {
    return _db.Delete(Table, cancellationToken);
  }

  [HttpDelete("{id}")]
  public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
  {
    bool success = await _db.Delete((Table, id), cancellationToken);

    if (!success)
      return NotFound();

    return Ok();
  }
}
```

## Embedded mode

SurrealDB can run embedded directly inside your .NET process — no external server needed. Three engines are available:

| Package                                                                                                                                                                               | Storage                | Use case             |
| ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- | ---------------------- | -------------------- |
| [![NuGet](https://img.shields.io/nuget/v/SurrealDb.Embedded.InMemory?style=flat-square)](https://www.nuget.org/packages/SurrealDb.Embedded.InMemory) `SurrealDb.Embedded.InMemory`    | In-memory (volatile)   | Testing, prototyping |
| [![NuGet](https://img.shields.io/nuget/v/SurrealDb.Embedded.RocksDb?style=flat-square)](https://www.nuget.org/packages/SurrealDb.Embedded.RocksDb) `SurrealDb.Embedded.RocksDb`       | RocksDB (persistent)   | Local storage        |
| [![NuGet](https://img.shields.io/nuget/v/SurrealDb.Embedded.SurrealKv?style=flat-square)](https://www.nuget.org/packages/SurrealDb.Embedded.SurrealKv) `SurrealDb.Embedded.SurrealKv` | SurrealKV (persistent) | Local storage        |

### Install

```sh
dotnet add package SurrealDb.Embedded.InMemory
```

### Basic example

```csharp
using SurrealDb.Embedded.InMemory;

await using var db = new SurrealDbMemoryClient();

await db.Use("test", "test");

var created = await db.Create("person", new { Name = "John", Age = 30 });
var results = await db.Select<Person>("person");
```

> Learn more about embedding SurrealDB in the [official docs](https://surrealdb.com/docs/languages/dotnet/embedding).

## .NET release versions

The .NET release versions must follow these rules:

- Should target at least the latest LTS (Long-Term Support) version
- Should target at least the latest STS (Standard-Term Support) version

SurrealDb.Net targets .NET versions following the [.NET Support Policy by Microsoft](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core). Additionally, SurrealDb.Net targets .NET Standard 2.1 explicitly to continue support of the Mono runtime (Unity, Xamarin, etc...).

Note that the support for .NET standard 2.1 will be maintained until further notice.

| Version           | Description | Release Date      | End of Support    |
| ----------------- | ----------- | ----------------- | ----------------- |
| .NET Standard 2.1 |             | June 27, 2016     | N/A               |
| .NET 8            | LTS         | November 14, 2023 | November 10, 2026 |
| .NET 9            | STS         | November 12, 2024 | November 10, 2026 |
| .NET 10           | Current LTS | November 11, 2025 | November 14, 2028 |

## Contributing

- [Contributing guidelines](./CONTRIBUTING.md)
- [Setup the project for contributions](./GET_STARTED.md)
