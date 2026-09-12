# SurrealDb.AgentMemory

The official C# / .NET client for the [SurrealDB Agent Memory](https://surrealdb.com/agent-memory) REST API.

Give your agents a memory that outlives a conversation: persistent facts, entities and relations extracted from free text, document knowledge with citations, and a server-driven chat loop — all behind one typed, generated client.

- **Typed end to end** — 60+ operations and 160+ models generated with [openapi-generator](https://openapi-generator.tech)
- **Streaming chat** — server-sent events consumed as an `IAsyncEnumerable<ChatChunk>`
- **Cursor pagination** — `WalkPagesAsync` / `CollectPagesAsync` over every list endpoint

## Install

```sh
dotnet add package SurrealDb.AgentMemory
```

## Quick start

Register the client:

```csharp
using SurrealDb.AgentMemory;
using SurrealDb.AgentMemory.Api;

builder.Services.AddAgentMemory(options =>
{
    options.Endpoint = "https://agent-memory.example.com"; // origin, no trailing slash
    options.ApiKey = builder.Configuration["AgentMemory:ApiKey"]!;
});
```

```csharp
public class MemoryService(DefaultApi api)
{
    public Task<IUploadDocumentApiResponse> UploadAsync(Stream pdf) =>
        api.UploadDocumentAsync(
            "acme", // context id — every operation is scoped to one context
            new FileParameter(pdf, "handbook.pdf", "application/pdf")
        );
}
```

## Remember & recall

Persist facts from free text (the server extracts entities and relations), then query them back:

```csharp
using SurrealDb.AgentMemory.Model;

// Remember: "Tobie prefers dark mode" — idempotent, with optional inference mode
await api.CreateFactAsync("acme", new FactsRequest(text: "Tobie prefers dark mode"));

// Recall: semantic + keyword retrieval over the memory graph
var response = await api.QueryMemoryAsync(
    "acme",
    new QueryMemoryRequestJson("What does Tobie prefer?", k: 10)
);
foreach (var hit in response.Ok().Hits)
{
    Console.WriteLine($"{hit.Score:0.00}  {hit.Text}");
}
```

Batch a whole conversation with `CreateFactsBatchAsync`, and use `ForgetAsync` when a user asks you to unremember something.

## Streaming chat (server-sent events)

`StreamChatAsync` forces `stream: true` and yields `ChatChunk` frames as they arrive — token deltas while the reply is being written, then a terminal frame carrying the full reply, what the turn wrote to memory, and its citations:

```csharp
await foreach (var chunk in api.StreamChatAsync("acme", new ChatRequestJson("What do you know about me?")))
{
    Console.Write(chunk.Delta);

    if (chunk.Done)
    {
        Console.WriteLine();
        Console.WriteLine($"session:  {chunk.SessionId}");
        Console.WriteLine($"citations: {chunk.Citations?.Count ?? 0}");
    }
}
```

The stream honors your `CancellationToken`, frames labelled `event: error` surface as a `ChatStreamException`, and comment / keep-alive lines are skipped.

## Cursor pagination

Every list endpoint returns a `page.nextCursor` (absent on the last page). `WalkPagesAsync` yields pages to exhaustion; `CollectPagesAsync` flattens rows, with an optional `max` early-stop:

```csharp
using SurrealDb.AgentMemory;

// Every action the context has ever seen, one page at a time
await foreach (var page in api.WalkPagesAsync(
                   (cursor, ct) => api.ListActionsAsync("acme", cursor: cursor.ToOption(), cancellationToken: ct),
                   r => r.Ok().Page))
{
    foreach (var action in page.Ok().Actions)
    {
        Console.WriteLine(action.Verb);
    }
}

// Or gather rows in one call — unbounded read, so prefer `max` for previews
var entities = await api.CollectPagesAsync(
    (cursor, ct) => api.ListEntitiesAsync("acme", cursor: cursor.ToOption(), cancellationToken: ct),
    r => r.Ok().Page,
    r => r.Ok().Entities,
    max: 500
);
```

The walk terminates on the cursor token — never on a short page — and fails fast (`InvalidOperationException`) if the server repeats a cursor instead of advancing.

## Multiple deployments

Apps talking to several Agent Memory instances (or contexts with different keys) use keyed registration, resolvable with `[FromKeyedServices]` or `GetRequiredKeyedService`:

```csharp
builder.Services.AddKeyedAgentMemory("prod",  o => { o.Endpoint = "https://mem.acme.io";      o.ApiKey = prodKey;  });
builder.Services.AddKeyedAgentMemory("staging", o => { o.Endpoint = "https://staging.acme.io"; o.ApiKey = stageKey; });

public class MyService(
    [FromKeyedServices("prod")] DefaultApi prod,
    [FromKeyedServices("staging")] DefaultApi staging)
{ /* ... */ }
```

Both `AddAgentMemory` and `AddKeyedAgentMemory` return the standard `IHttpClientBuilder`, so resilience is one line when you want it:

```csharp
builder.Services
    .AddAgentMemory(options => { /* ... */ })
    .AddStandardResilienceHandler(); // from Microsoft.Extensions.Http.Resilience
```

## Responses & errors

Operations come in two flavors:

- `XxxAsync` always returns the response interface — check `IsSuccessStatusCode`, or read the payload with `Ok()` / `TryOk(out var result)`.
- `XxxOrDefaultAsync` returns `null` instead when the server answers with an unexpected status.

Transport failures throw `ApiException`; a chat stream that fails mid-flight (after headers were sent) throws `ChatStreamException`.

## Without dependency injection

The client is a plain class — usable from any host:

```csharp
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using SurrealDb.AgentMemory;
using SurrealDb.AgentMemory.Api;
using SurrealDb.AgentMemory.Client;

var api = new DefaultApi(
    NullLogger<DefaultApi>.Instance,
    new HttpClient { BaseAddress = new Uri("https://agent-memory.example.com/") },
    new JsonSerializerOptionsProvider(AgentMemoryJson.DefaultOptions),
    new DefaultApiEvents(),
    tokenProvider /* TokenProvider<BearerToken> of your choosing */
);
```

The `AddAgentMemory` wiring above is the supported path; the manual form exists for hosts without `IServiceCollection`.

## Regenerating the client

To pick up a new spec version, replace `spec/openapi.json` (the source of truth, downloaded from the Agent Memory deployment) and run:

```sh
cd SurrealDb.AgentMemory
./generate.sh
```

## Versioning

The package version mirrors the Agent Memory API version it was generated from (`0.2.x` today). The API is still pre-1.0 — expect additive and breaking change until it stabilizes.

## License

This repository is licensed under the [Business Source License 1.1](../LICENSE) (BUSL-1.1), consistent with the Agent Memory API specification.
