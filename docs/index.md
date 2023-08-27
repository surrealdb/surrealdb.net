# SurrealDb

The official SurrealDB library for .NET

[![](https://img.shields.io/badge/status-beta-ff00bb.svg?style=flat-square)](https://github.com/surrealdb/surrealdb.net) [![](https://img.shields.io/badge/docs-view-44cc11.svg?style=flat-square)](https://surrealdb.com/docs/integration/libraries/dotnet) [![](https://img.shields.io/badge/license-Apache_License_2.0-00bfff.svg?style=flat-square)](https://github.com/surrealdb/surrealdb.net)

## Getting started

### Installation

```
dotnet add package SurrealDb
```

### How to use?

Supported protocols:

-   ✅ HTTP(S)
-   ✅ WS(S)
-   🚧 and more to come...

#### Construct a new SurrealDB client

##### As-is

You can easily create a new SurrealDB client easily. All you have to do is define the `endpoint` to the SurrealDB instance.

```csharp
var clientHttp = new SurrealDbClient("http://localhost:8000");
var clientHttps = new SurrealDbClient("https://cloud.surrealdb.com");
var clientWs = new SurrealDbClient("ws://localhost:8000/rpc");
var clientWss = new SurrealDbClient("wss://cloud.surrealdb.com/rpc");

// Signin & Use ns/db
```

##### Static constructor

There are some static constructors that you can use for specific contexts. The advantage of this syntax is that all you only need to do is define the `host` and not the `endpoint`.

```csharp
var clientHttp = SurrealDbHttpClient.New("localhost:8000");
var clientHttps = SurrealDbHttpsClient.New("cloud.surrealdb.com");
var clientWs = SurrealDbWsClient.New("localhost:8000");
var clientWss = SurrealDbWssClient.New("cloud.surrealdb.com");

// Signin & Use ns/db
```

##### Dependency injection

Last but not least, you can use Dependency Injection with the `services.AddSurreal()` function.

###### Default instance

```csharp
var options = SurrealDbOptions
	.Create()
	.WithEndpoint("http://localhost:8000")
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
		"SurrealDB": "Server=http://localhost:8000;Namespace=test;Database=test;Username=root;Password=root"
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

-   the default one, you can keep using `ISurrealDbClient` interface or `SurrealDbClient` class anywhere
-   a client for backup purpose, using the `IBackupSurrealDbClient` interface
-   a client for monitoring purpose, using the `IMonitoringSurrealDbClient` interface

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
	[Route("/")]
	public Task<List<WeatherForecast>> GetAll(CancellationToken cancellationToken)
	{
		return _surrealDbClient.Select<WeatherForecast>(Table, cancellationToken);
	}

	[HttpGet]
	[Route("/:id")]
	public async Task<IActionResult> Get(string id, CancellationToken cancellationToken)
	{
		var weatherForecast = await _surrealDbClient.Select<WeatherForecast>(Table, id, cancellationToken);

		if (weatherForecast is null)
			return NotFound();

		return Ok(weatherForecast);
	}

	[HttpPost]
	[Route("/")]
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
	[Route("/")]
	public Task<WeatherForecast> Update(WeatherForecast data, CancellationToken cancellationToken)
	{
		return _surrealDbClient.Upsert(data, cancellationToken);
	}

	[HttpPatch]
	[Route("/:id")]
	public Task<WeatherForecast> Patch(string id, Dictionary<string, object> data, CancellationToken cancellationToken)
	{
		var thing = new Thing(Table, id);

		return _surrealDbClient.Patch<WeatherForecast>(thing, data, cancellationToken);
	}

	[HttpDelete]
	[Route("/")]
	public Task DeleteAll(CancellationToken cancellationToken)
	{
		return _surrealDbClient.Delete(Table, cancellationToken);
	}

	[HttpDelete]
	[Route("/:id")]
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

This project was written following testing best practices:

-   TDD, leveraging:
    -   clean code/architecture
    -   regression testing
    -   adding new features and tests easily
-   xUnit and FluentAssertions libraries
-   a vast majority of tests are integration tests, ensuring compatibility with a concrete SurrealDB version
-   each integration test is using a separate SurrealDB instance

### Testing

Unit/Integration tests are written using [xUnit](https://xunit.net/) and [FluentAssertions](https://fluentassertions.com/).

You will need a local SurrealDB instance alongside the tests. Start one using the following command:

```
surreal start --log debug --user root --pass root memory
```

Once ready, go to the root directory of the project and run the following command:

```
dotnet watch test --project SurrealDb.Tests
```

### Benchmarking

This project also contains [benchmarks](https://benchmarkdotnet.org/) in order to detect possible performance regressions.

You will need a local SurrealDB instance alongside the tests. Start one using the following command:

```
surreal start --log debug --user root --pass root memory
```

Once ready, go to the root directory of the project and run the following command:

```
dotnet run -c Release --project SurrealDb.Benchmarks --filter '*'
```

### Documentation

The documentation website of this project is automatically generated using [docfx](https://dotnet.github.io/docfx/).

If you want to contribute to the documentation, be sure to have .NET SDK on your computer (.NET 6.0 or higher) and then install the cli tool:

```
dotnet tool update -g docfx
```

Then run the following command to start the web server:

```
docfx docs\docfx.json --serve
```
