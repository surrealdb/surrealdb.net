# surrealdb.net

In-memory provider of the official SurrealDB SDK for .NET.

## Documentation

View the SDK documentation [here](https://surrealdb.com/docs/integration/libraries/dotnet).

## How to install

```sh
dotnet add package SurrealDb.Embedded.InMemory
```

## Getting started

### Constructing a new SurrealDB client

You can easily create a new SurrealDB memory client which will provide an in-memory instance of SurrealDB. 

```csharp
using var db = new SurrealDbMemoryClient();

const string TABLE = "person";

var person = new Person
{
    Title = "Founder & CEO",
    Name = new() { FirstName = "Tobie", LastName = "Morgan Hitchcock" },
    Marketing = true
};
var created = await db.Create(TABLE, person);
Console.WriteLine(ToJsonString(created));
```

### Dependency injection

You can use Dependency Injection with the `services.AddSurreal()` and `.AddInMemoryProvider()` functions.

#### Default instance

```csharp
services.
  .AddSurreal("Endpoint=mem://")
  .AddInMemoryProvider();
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
    "SurrealDB": "Endpoint=mem://;Namespace=test;Database=test"
  }
}
```

You can use the Connection String instead of having to deal with a `SurrealDbOptions`.

```csharp
services
  .AddSurreal(configuration.GetConnectionString("SurrealDB"))
  .AddInMemoryProvider();
```

It will automatically create a new SurrealDB using the `Client endpoint` and configure the client using the different values for `namespace`, `database`. Note that these values are optional but the `endpoint` is still required.

#### Multiple instances

Having a default instance for a project is enough most of the time, but there may be times when you'd like to target multiple SurrealDB instances, either at different addresses or at the same address but inside different NS/DBs. You can use multiple instances as long as you provide 1 interface per client, as in the following example.

```csharp
interface IBackupSurrealDbClient : ISurrealDbClient { }
interface IMonitoringSurrealDbClient : ISurrealDbClient { }

services
  .AddSurreal(configuration.GetConnectionString("SurrealDB.Main"))
  .AddInMemoryProvider();
services.AddSurreal<IBackupSurrealDbClient>(configuration.GetConnectionString("SurrealDB.Backup"));
services.AddSurreal<IMonitoringSurrealDbClient>(configuration.GetConnectionString("SurrealDB.Monitoring"));
```

Here you will have 3 instances:

- the default one using the memory provider, you can keep using `ISurrealDbClient` interface or `SurrealDbClient` class anywhere
- a remote client for backup purpose, using the `IBackupSurrealDbClient` interface
- a remote client for monitoring purpose, using the `IMonitoringSurrealDbClient` interface

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
    return _surrealDbClient.PatchAll(Table, patches, cancellationToken);
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
