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
