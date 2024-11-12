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
using var clientHttp = new SurrealDbClient("http://127.0.0.1:8000");
using var clientHttps = new SurrealDbClient("https://127.0.0.1:8000");
using var clientWs = new SurrealDbClient("ws://127.0.0.1:8000/rpc");
using var clientWss = new SurrealDbClient("wss://127.0.0.1:8000/rpc");

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

services.AddSurreal(options);
```

Then you will be able to use the `ISurrealDbClient` interface or `SurrealDbClient` class anywhere.

```csharp
public class MyClass
{
  private readonly ISurrealDbClient _client;

  public MyClass(ISurrealDbClient client)
  {
    _client = client;
  }

  // ...
}
```

Note that the default lifetime of this service is `Singleton`. You can override this as follows:

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

Having a default instance for a project is enough most of the time, but there may be times when you'd like to target multiple SurrealDB instances, either at different addresses or at the same address but inside different NS/DBs. You can use multiple instances as long as you provide 1 interface per client, as in the following example.

```csharp
interface IBackupSurrealDbClient : ISurrealDbClient { }
interface IMonitoringSurrealDbClient : ISurrealDbClient { }

services.AddSurreal(configuration.GetConnectionString("SurrealDB.Main"));
services.AddSurreal<IBackupSurrealDbClient>(configuration.GetConnectionString("SurrealDB.Backup"));
services.AddSurreal<IMonitoringSurrealDbClient>(configuration.GetConnectionString("SurrealDB.Monitoring"));
```

Here you will have 3 instances:

- the default one, you can keep using `ISurrealDbClient` interface or `SurrealDbClient` class anywhere
- a client for backup purpose, using the `IBackupSurrealDbClient` interface
- a client for monitoring purpose, using the `IMonitoringSurrealDbClient` interface

### Use the client

```csharp
[ApiController]
[Route("[controller]")]
public class WeatherForecastController : ControllerBase
{
  private const string Table = "weatherForecast";

  private readonly ISurrealDbClient _surrealDbClient;

  public WeatherForecastController(ISurrealDbClient surrealDbClient)
  {
    _surrealDbClient = surrealDbClient;
  }

  [HttpGet]
  public Task<IEnumerable<WeatherForecast>> GetAll(CancellationToken cancellationToken)
  {
    return _surrealDbClient.Select<WeatherForecast>(Table, cancellationToken);
  }

  [HttpGet("{id}")]
  public async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
  {
    var weatherForecast = await _surrealDbClient.Select<WeatherForecast>((Table, id), cancellationToken);

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

    return _surrealDbClient.Create(Table, weatherForecast, cancellationToken);
  }

  [HttpPut]
  public Task<WeatherForecast> Update(WeatherForecast data, CancellationToken cancellationToken)
  {
    return _surrealDbClient.Upsert(data, cancellationToken);
  }

  [HttpPatch]
  public Task<IEnumerable<WeatherForecast>> PatchAll(
    JsonPatchDocument<WeatherForecast> patches,
    CancellationToken cancellationToken
  )
  {
    return _surrealDbClient.Patch(Table, patches, cancellationToken);
  }

  [HttpPatch("{id}")]
  public Task<WeatherForecast> Patch(
    string id,
    JsonPatchDocument<WeatherForecast> patches,
    CancellationToken cancellationToken
  )
  {
    return _surrealDbClient.Patch((Table, id), patches, cancellationToken);
  }

  [HttpDelete]
  public Task DeleteAll(CancellationToken cancellationToken)
  {
    return _surrealDbClient.Delete(Table, cancellationToken);
  }

  [HttpDelete("{id}")]
  public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
  {
    bool success = await _surrealDbClient.Delete((Table, id), cancellationToken);

    if (!success)
      return NotFound();

    return Ok();
  }
}
```

## Contributing

### Prerequisites

Before contributing to this repository, please take note of the [Contributing](./CONTRIBUTING.md) guidelines. To contribute to this project, you will also need to install the following tools:

* The .NET SDK, preferably the latest stable version which is available for [download here](https://dotnet.microsoft.com/download)
* The [Rust programming language](https://www.rust-lang.org/learn/get-started)

The test and benchmark projects are highly dependent on the local Rust crate used by embedded providers. This crate is located in the [./rust-embedded](./rust-embedded) folder of this repository. To build the crate, make sure you installed the Rust toolchain on your machine and then follow these steps:

```sh
cd ./rust-embedded
cargo build
```

If the command line was successful, the compiled libraries are generated in the target folder and automatically copied when the .NET projects are built. 

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
| .NET 9            | STS         | November 12, 2024 | May 12, 2026      |

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

### Testing

This project was written following testing best practices:

- TDD, leveraging:
  - clean code/architecture
  - regression testing
  - adding new features and tests easily
- a vast majority of tests are integration tests, ensuring compatibility with a concrete SurrealDB version
- each integration test is using a separate SurrealDB namespace/database

Unit/Integration tests are written using [xUnit](https://xunit.net/) and [FluentAssertions](https://fluentassertions.com/).

You will need a local SurrealDB instance alongside the tests. Start one using the following command:

```sh
surreal start --log debug --user root --pass root memory --allow-guests
```

Once ready, go to the root directory of the project and run the following command:

```sh
dotnet watch test --project SurrealDb.Net.Tests
```

Due to the asynchronous nature of Live Queries, they are tested against a separate project named `SurrealDb.Net.LiveQuery.Tests`. Where the default test project allow full parallelization, this project completely disable test parallelization. To execute tests on Live Queries, run the following command:

```sh
dotnet watch test --project SurrealDb.Net.LiveQuery.Tests
```

Note 1: Because Live Query tests are not run in parallel, it can take quite some time to run all tests.

Note 2: You can run the two test projects in parallel.

### Benchmarking

This project also contains [benchmarks](https://benchmarkdotnet.org/) in order to detect possible performance regressions.

You will need a local SurrealDB instance alongside the tests. Start one using the following command:

```sh
surreal start --user root --pass root memory --allow-guests
```

Once ready, go to the root directory of the project and run the following command:

```sh
dotnet run -c Release --project SurrealDb.Net.Benchmarks.Remote --filter '*'
```

```sh
./prepare_embedded_benchmarks.sh -s
dotnet run -c Release --project SurrealDb.Net.Benchmarks.Embedded --filter '*'
./prepare_embedded_benchmarks.sh -e
```

For Windows:

```sh
./prepare_embedded_benchmarks.ps1 -s
dotnet run -c Release --project SurrealDb.Net.Benchmarks.Embedded --filter '*'
./prepare_embedded_benchmarks.ps1 -e
```