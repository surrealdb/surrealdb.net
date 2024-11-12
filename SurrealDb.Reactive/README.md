# SurrealDb.Reactive

Reactive Extensions package for the official SurrealDB SDK for .NET.

## Documentation

View the SDK documentation [here](https://surrealdb.com/docs/integration/libraries/dotnet).

## How to install

```sh
dotnet add package SurrealDb.Reactive
```

## Getting started

Follow the documentation of the .NET SDK to setup a new `SurrealDbClient`. When done, you can now use the methods provided by the Reactive Extensions package for .NET.

### Examples - Observe Live Query changes

```csharp
client
    .ObserveTable<PostRecord>("post")
    // Similar to observing the query "LIVE SELECT * FROM post;"
    .Subscribe(response =>
    {
        // you can listen to any live query response
    });
```

```csharp
client
    .ObserveTable<PostRecord>("post")
    .OfType<SurrealDbLiveQueryCreateResponse<PostRecord>>()
    .Select(r => r.Result)
    .Subscribe(record =>
    {
        // you can filter out the LQ type and get every created records in realtime
    });
```

### Examples - Record table aggregation

If you want to get a list of all records being created/updated/deleted since watching the Live Query, you can aggregate all the records using the `AggregateRecords`.

```csharp
using var liveQuery = client.ListenLive<PostRecord>(liveQueryUuid);

liveQuery
    .GetResults()
    .ToObservable()
    .AggregateRecords(new Dictionary<string, TestRecord>())
    .Select(x => x.Values.ToList())
    .Subscribe(records =>
    {
        // You can consume the list of all records, being updating in realtime on each CREATE, UPDATE or DELETE event
    });
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
