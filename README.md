# surrealdb.net

The official SurrealDB library for .NET.

[![](https://img.shields.io/badge/status-beta-ff00bb.svg?style=flat-square)](https://github.com/surrealdb/surrealdb.net)
[![](https://img.shields.io/badge/docs-view-44cc11.svg?style=flat-square)](https://surrealdb.com/docs/integration/libraries/dotnet)
[![](https://img.shields.io/badge/license-Apache_License_2.0-00bfff.svg?style=flat-square)](https://github.com/surrealdb/surrealdb.net)
[![](https://img.shields.io/nuget/v/surrealdb.net?style=flat-square)](https://www.nuget.org/packages/SurrealDb.Net)
[![](https://img.shields.io/codecov/c/github/surrealdb/surrealdb.net?style=flat-square)](https://codecov.io/github/surrealdb/surrealdb.net?branch=main)

⚠️ This driver is currently community maintained.

## Getting started

### Installation

```
dotnet add package SurrealDb.Net
```

### How to use?

Supported protocols:

- ✅ HTTP(S)
- ✅ WS(S)
- 🚧 and more to come...

#### Construct a new SurrealDB client

##### As-is

You can easily create a new SurrealDB client easily. All you have to do is define the `endpoint` to the SurrealDB instance.

```csharp
var clientHttp = new SurrealDbClient("http://127.0.0.1:8000");
var clientHttps = new SurrealDbClient("https://cloud.surrealdb.com");
var clientWs = new SurrealDbClient("ws://127.0.0.1:8000/rpc");
var clientWss = new SurrealDbClient("wss://cloud.surrealdb.com/rpc");

// Signin & Use ns/db
```

##### Static constructor

There are some static constructors that you can use for specific contexts. The advantage of this syntax is that all you only need to do is define the `host` and not the `endpoint`.

```csharp
var clientHttp = SurrealDbHttpClient.New("127.0.0.1:8000");
var clientHttps = SurrealDbHttpsClient.New("cloud.surrealdb.com");
var clientWs = SurrealDbWsClient.New("127.0.0.1:8000");
var clientWss = SurrealDbWssClient.New("cloud.surrealdb.com");

// Signin & Use ns/db
```

##### Dependency injection

Last but not least, you can use Dependency Injection with the `services.AddSurreal()` function.

###### Default instance

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

###### Connection String

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

###### Multiple instances

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

#### Use the client

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
		var weatherForecast = await _surrealDbClient.Select<WeatherForecast>(Table, id, cancellationToken);

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
        return _surrealDbClient.PatchAll(Table, patches, cancellationToken);
    }

    [HttpPatch("{id}")]
    public Task<WeatherForecast> Patch(
        string id,
        JsonPatchDocument<WeatherForecast> patches,
        CancellationToken cancellationToken
    )
    {
        var thing = new Thing(Table, id);

        return _surrealDbClient.Patch(thing, patches, cancellationToken);
    }

	[HttpDelete]
	public Task DeleteAll(CancellationToken cancellationToken)
	{
		return _surrealDbClient.Delete(Table, cancellationToken);
	}

	[HttpDelete("{id}")]
	public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
	{
		bool success = await _surrealDbClient.Delete(Table, id, cancellationToken);

		if (!success)
			return NotFound();

		return Ok();
	}
}
```

## How to contribute?



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

```bash
# Run this command at the root of the project
dotnet tool install csharpier
```

You can then use it as a cli:

```bash
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

```
surreal start --log debug --user root --pass root memory --auth --allow-guests
```

Once ready, go to the root directory of the project and run the following command:

```
dotnet watch test --project SurrealDb.Net.Tests
```

Due to the asynchronous nature of Live Queries, they are tested against a separate project named `SurrealDb.Net.LiveQuery.Tests`. Where the default test project allow full parallelization, this project completely disable test parallelization. To execute tests on Live Queries, run the following command:

```
dotnet watch test --project SurrealDb.Net.LiveQuery.Tests
```

Note 1: Because Live Query tests are not run in parallel, it can take quite some time to run all tests.

Note 2: You can run the two test projects in parallel.

### Benchmarking

This project also contains [benchmarks](https://benchmarkdotnet.org/) in order to detect possible performance regressions.

You will need a local SurrealDB instance alongside the tests. Start one using the following command:

```
surreal start --user root --pass root memory --auth --allow-guests
```

Once ready, go to the root directory of the project and run the following command:

```
dotnet run -c Release --project SurrealDb.Net.Benchmarks --filter '*'
```
