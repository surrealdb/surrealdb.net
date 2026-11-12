# SurrealDb.AgentMemory

The official .NET client for the [SurrealDB Agent Memory](https://surrealdb.com/agent-memory) REST API.
It has the same surface as the JavaScript (`@surrealdb/memory`) and Python (`surrealdb-memory`) clients,
with .NET naming: `PascalCase`, `Async` suffixes, and `CancellationToken` for cancellation.

## Install

```sh
dotnet add package SurrealDb.AgentMemory
```

## Quick start

```csharp
using SurrealDb.AgentMemory;

// Create a client, pinned to one context
using var memory = new AgentMemoryClient(new AgentMemoryOptions
{
    Endpoint = Environment.GetEnvironmentVariable("AGENT_MEMORY_ENDPOINT")!,
    Context = "acme-prod",
    ApiKey = Environment.GetEnvironmentVariable("AGENT_MEMORY_API_KEY")!,
});

// Upload a document
var document = await memory.Documents.UploadAsync(new DocumentUploadOptions
{
    File = AgentMemoryFile.FromPath("handbook.pdf"),
    Title = "Handbook",
});

// Remember a fact and recall it
await memory.RememberAsync("I just got promoted to CTO", new RememberOptions { Scopes = "user/tobie" });
var hits = await memory.RecallAsync("What is Tobie's role?", new RecallOptions { K = 10 });

// Chat (server-driven memory loop)
var chat = await memory.ChatAsync("What do you know about me?");
Console.WriteLine(chat.Reply);
```

The responses are the models generated from the API specification, in the `SurrealDb.AgentMemory.Model` namespace.

## Dependency injection

```csharp
builder.Services.AddAgentMemory(options =>
{
    options.Endpoint = builder.Configuration["AgentMemory:Endpoint"]!;
    options.Context = "acme-prod";
    options.ApiKey = builder.Configuration["AgentMemory:ApiKey"]!;
});

public class MemoryService(AgentMemoryClient memory) { /* ... */ }
```

The options are validated when you register them. The client's `HttpClient` comes from `IHttpClientFactory`, and
`AddAgentMemory` returns its `IHttpClientBuilder`, so you can add handlers to it. The client already retries reads and
idempotent writes, so a handler that retries as well multiplies the attempts.

To talk to several contexts or deployments, register keyed clients and resolve them with `[FromKeyedServices]`:

```csharp
builder.Services.AddKeyedAgentMemory("prod", o => { o.Endpoint = prodEndpoint; o.Context = "acme"; o.ApiKey = prodKey; });
builder.Services.AddKeyedAgentMemory("staging", o => { o.Endpoint = stagingEndpoint; o.Context = "acme"; o.ApiKey = stagingKey; });

public class Sync([FromKeyedServices("prod")] AgentMemoryClient prod, [FromKeyedServices("staging")] AgentMemoryClient staging);
```

## Memory operations

```csharp
// Persist facts from free text and/or caller-supplied triples (idempotent).
await memory.RememberAsync("Tobie prefers dark mode", new RememberOptions { Infer = InferModes.Full });

// Persist a batch of conversation messages.
await memory.RememberManyAsync(
[
    new BatchMessage(content: "I moved to Lisbon", role: TurnRole.User),
    new BatchMessage(content: "Noted.", role: TurnRole.Assistant),
]);

// Recall, context, reflection, and forgetting.
await memory.RecallAsync("Where does Tobie live?", new RecallOptions { K = 5 });
await memory.ContextAsync("Summarise preferences", new ContextOptions { K = 5 });
await memory.ReflectAsync("What changed this week?", new ReflectOptions { Persist = true });
await memory.ForgetAsync("Remove old project notes", new ForgetOptions { Purge = true });

// What the context knows about a subject, in one round trip.
await memory.LookupAsync("Who is Tobie?");

// Snapshots and maintenance.
await memory.StateAsync(limit: 500); // bounded per table; check `Truncated`
await memory.ProfileAsync();
await memory.WhoamiAsync();
await memory.ConsolidateAsync(new ConsolidateOptions { DryRun = true });
await memory.ElaborateAsync(new ElaborateOptions { EntityRef = "person:tobie" });
await memory.FsckAsync();
await memory.InspectAsync("person:tobie");
await memory.AuditAsync(new AuditOptions { Limit = 50 });

// Self-service API keys.
var minted = await memory.Keys.CreateAsync(new KeyCreateOptions { Name = "ci", TtlSeconds = 3600 });
await memory.Keys.ListAsync(); // one page; `Keys.ListAllAsync()` walks them all
await memory.Keys.RotateAsync("ci");
await memory.Keys.DeleteAsync("ci");
```

### Streaming chat

```csharp
string? sessionId = null;
await foreach (var chunk in memory.StreamChatAsync("Tell me a story"))
{
    Console.Write(chunk.Delta);
    sessionId = chunk.SessionId ?? sessionId; // sent on the opening `meta` frame

    if (chunk.Done)
    {
        Console.WriteLine($"\nsession: {sessionId}, citations: {chunk.Citations?.Count ?? 0}");
    }
}
```

The opening `meta` frame carries the session and trace ids, and the terminal `done` frame carries the full reply, what
the turn wrote to memory, and its citations. A failure after the stream opened is never mistaken for a reply that
simply ended: an `error` frame, or a stream that closes before its terminal frame, raises
`AgentMemoryStreamException`, and a dropped connection raises `AgentMemoryConnectionException`.

## Known-about lookups

`LookupAsync(query)` answers what the context knows about a subject in one round trip. Everything it returns is a stored
row: nothing is generated, nothing is summarised by a model, and an identical query returns an identical answer.

Branch on the resolution kind rather than inferring the case from an array length. The generated `ResolutionJson`
has one property per kind, and exactly one of them is set:

```csharp
var answer = await memory.LookupAsync("Atlas");
var resolution = answer.Resolution;

if (resolution.ResolutionJsonOneOf is { } entity)            // kind: entity
    Render(entity.Subject, answer.Facts.Items);
else if (resolution.ResolutionJsonOneOf1 is { } ambiguous)   // kind: ambiguous
    OfferChoice(ambiguous.Candidates);                       // each has a `Distinguisher`
else if (resolution.ResolutionJsonOneOf2 is not null)        // kind: topic
    RenderCluster(answer.Entities.Items, answer.Passages.Items);
else if (resolution.ResolutionJsonOneOf3 is { } empty)       // kind: empty
    Suggest(empty.Nearest);                                  // "stored under another name"
```

Every section is bounded and reports `Truncated`; none of them page. A section that was cut points at its own
collection, which does walk:

| Section | Walk it with |
| --- | --- |
| `Facts` | `memory.Facts.AllAttributesAsync(new AttributeFilters { Entity = entity })` |
| `Relations` | `memory.Facts.AllEdgesOfAsync(entity)`: the section carries edges in either direction |
| `Events` | `memory.Facts.AllActionsAsync(new ActionFilters { Actor = entity })` |
| `Passages` | `memory.RecallAsync(...)` |
| `Uncertainty` | `memory.Uncertainty.ListAsync(new UncertaintyListOptions { Entity = entity })` |

Following a trail is cheap: pass `Subject` to skip resolution when a hop already knows which entity it landed on.

```csharp
await memory.LookupAsync("Atlas", new LookupOptions
{
    Subject = "product/atlas",
    Include = [LookupSections.Facts, LookupSections.Relations],
});
```

### Finding a subject

```csharp
// Name search: lexical, deterministic, best match first. A ranked head, not a walk.
await memory.Entities.SearchAsync("tobie", new EntitySearchOptions { Type = "person", Limit = 5 });

// Where to start when there is no query yet.
await memory.Entities.TopAsync(new TopEntitiesOptions { By = EntityRankings.Coverage });

// One hop out, each neighbour carrying its own fact count.
await memory.Entities.NeighboursAsync("person", "tobie", new NeighbourhoodOptions { MinFacts = 2 });

// What changed about a subject: every key's supersession chain, newest first.
await memory.Entities.ChangesAsync("person", "tobie", new PageOptions { Limit = 50 });

// The collections a bounded head points at, all cursor-paged in the same order.
await memory.Facts.AttributesAsync(new AttributeListOptions { Entity = "person/tobie" });
await memory.Facts.AllEdgesOfAsync("person/tobie"); // both directions
await memory.Facts.ActionsAsync(new ActionListOptions { Actor = "person/tobie", Since = "2026-01-01T00:00:00Z" });
```

### Settling a contradiction

```csharp
var open = await memory.Uncertainty.ListAsync(new UncertaintyListOptions { Resolved = false });

foreach (var flag in open.Unknowns)
{
    // `Resolvable` says whether settling would succeed for this key.
    if (flag.Resolvable)
    {
        await memory.Uncertainty.ResolveAsync(flag.Id, "SurrealDB",
            new ResolveUncertaintyOptions { Note = "confirmed in the offer letter" });
    }
}
```

## Namespaces

| Namespace | Highlights |
| --- | --- |
| `memory.Documents` | `UploadAsync`, `ReprocessAsync`, `GetAsync`, `RawAsync`, `ChunksAsync`, `AllChunksAsync`, `ListAsync`, `ListAllAsync`, `CountAsync`, `DeleteAsync`, `QueryAsync`, `RecomputeLinksAsync`, `Keywords.*` |
| `memory.Entities` | `ListAsync`, `ListAllAsync`, `CountAsync`, `SearchAsync`, `TopAsync`, `GetAsync`, `NeighboursAsync`, `AllNeighboursAsync`, `ChangesAsync`, `AllChangesAsync`, `HistoryAsync`, `DeleteAsync` |
| `memory.Facts` | `AttributesAsync`, `AllAttributesAsync`, `RelationsAsync`, `AllRelationsAsync`, `AllEdgesOfAsync`, `ActionsAsync`, `AllActionsAsync` |
| `memory.Uncertainty` | `ListAsync`, `ListAllAsync`, `CountAsync`, `ResolveAsync` |
| `memory.Sessions` | `CreateAsync` → `Session` (`TurnsAsync`, `AllTurnsAsync`, `ContextAsync`, `CloseAsync`) |
| `memory.Lifecycle` | `ExpireAsync`, `DecayAsync` |
| `memory.Traces` | `ListAsync`, `ListAllAsync`, `GetAsync`, `StatsAsync` |
| `memory.Principals` | `ListAsync`, `ListAllAsync`, `GetAsync`, `EffectiveAsync`, `GrantAsync`, `RevokeAsync` |
| `memory.Scopes` | `ListAsync`, `ListAllAsync`, `RegisterAsync`, `DeleteAsync`, `ForgetAsync` |
| `memory.Keys` | `CreateAsync`, `ListAsync`, `ListAllAsync`, `DeleteAsync`, `RotateAsync` |

## Pagination

Every list surface pages the same way. `ListAsync` returns one page (the rows, plus a `Page` block) and takes
`Limit` (default 100, max 500) and `Cursor`, plus `Count` on the endpoints that offer a total:

```csharp
var first = await memory.Entities.ListAsync(new EntityListOptions { Limit = 50 });
// first.Entities: the rows; first.Page: HasMore, NextCursor, TotalSize

string? cursor = null;
do
{
    var page = await memory.Entities.ListAsync(new EntityListOptions { Limit = 50, Cursor = cursor });
    Handle(page.Entities);
    cursor = page.Page.NextCursor;
} while (!string.IsNullOrEmpty(cursor));
```

End a walk on the cursor, never on a short page: a page is bounded in the database and then filtered for visibility,
so `/scopes` and `/keys` can return fewer rows than `Limit` while more pages remain.

Every listing also has a `ListAllAsync` that does the walk for you, and the ones worth counting have a `CountAsync`
that reads `TotalSize` from a single one-row request. `ListAllAsync` is an unbounded read by construction;
`AllChunksAsync`, `AllNeighboursAsync`, `AllChangesAsync` and the `Facts` walks take a `max` for the bounded case.

`Count` costs a full count of the filtered set, so it is opt-in. `Scopes.ListAsync` and `AuditAsync` take `Limit` and
`Cursor` only. `NeighbourhoodOptions` refuses `Count` together with `MinFacts`, and the document listings refuse
`Cursor` together with the deprecated `Page`, because the server rejects both pairings with a `400`.

`AgentMemoryPagination.WalkPagesAsync` and `CollectPagesAsync` are public, for walking any other paginated surface.

## Delegation

`memory.OnBehalfOf(principalId)` returns a client whose every request carries the delegation header, so calls run
with that principal's authorisation. This requires the `manage` grant. The original client is left unchanged.

The header is spelled `X-Spectron-On-Behalf-Of`. That is a wire constant rather than branding: the service matches the
name exactly, so it stays until the service renames it.

```csharp
var asAlex = memory.OnBehalfOf("principal:alex");
await asAlex.RememberAsync("Reviewed the Q3 plan");
await asAlex.RecallAsync("What did Alex review?");
```

## Errors

Every failure is an `AgentMemoryException` (with `Status`, `Title`, `Detail`, `Type`, `Instance` and `Extensions`
from the problem details), or one of its subclasses:

| Class | Typical cause |
| --- | --- |
| `AgentMemoryAuthException` | 401 |
| `AgentMemoryScopeException` | 403 |
| `AgentMemoryNotFoundException` | 404 |
| `AgentMemoryValidationException` | 400 / 422 |
| `AgentMemoryRateLimitException` | 429 (`RetryAfter` when provided) |
| `AgentMemoryServerException` | 5xx |
| `AgentMemoryConnectionException` | Network failure (also mid-stream) or timeout |
| `AgentMemoryStreamException` | A streaming chat reported an error, or ended before its terminal frame |

Your own cancellation is not an error: it surfaces as `OperationCanceledException`, apart from a timeout.
`AgentMemoryException.FromResponse` builds the typed error from a status and a body.

## Retries and idempotency

Reads retry on `5xx` and connection failures with back-off `250ms`, `500ms`, `1000ms` (up to `MaxRetries`,
default `3`). `RememberAsync` and `RememberManyAsync` carry an `Idempotency-Key` derived from the request and a
30-second window, derived the same way as in the JavaScript and Python clients, so they are retried safely too; so are the
reads that travel as a `POST` (`RecallAsync`, `LookupAsync`, `ContextAsync`, `Documents.QueryAsync`,
`Documents.Keywords.SearchAsync`, `Session.ContextAsync`). Other writes are not retried. Timeouts and cancellations
are never retried.

## Timeouts and cancellation

Requests have a 30-second deadline by default (`AgentMemoryOptions.Timeout`). Uploads have no deadline unless you set
`DocumentUploadOptions.Timeout`, because a large body can legitimately take longer than any fixed one; set
`UploadProgress` to observe one. `Documents.RawAsync` takes its own timeout. A streaming chat is bounded only by your
`CancellationToken`.

## Scope

Write and session calls take `Scopes`, and reads (`RecallAsync`, `ContextAsync`, `Documents.QueryAsync`,
`Documents.Keywords.SearchAsync`) take the same shape as `Lens`. The wire format is a `ScopeSets`: a DNF selector,
`string[][]`. The outer array is an OR of clauses; each inner array is an AND of `key/value` paths, so
`[["team/a"], ["team/b", "clearance/secret"]]` means `team/a OR (team/b AND clearance/secret)`.

A bare string is a single-path clause and a flat array is an OR of single-path clauses. Empty paths and empty clauses
are dropped; leave `Scopes` unset to use the key's default write region.

```csharp
new RememberOptions { Scopes = "team/eng" };                                // [["team/eng"]]
new RememberOptions { Scopes = new[] { "team/eng", "org/acme" } };          // OR: [["team/eng"], ["org/acme"]]
new RememberOptions { Scopes = Scope.AllOf("team/eng", "org/acme") };       // AND: [["team/eng", "org/acme"]]
```

A read lens narrows the region a query reads from, and may use `/*` to select a subtree. It can only narrow: the key's
grants still apply, so a lens outside the granted region returns no rows rather than a `403`.

## Regenerating the models

`spec/openapi.json` is the API specification the models in `Generated/` come from. To pick up a new version, replace
it and run `./generate.sh` (it needs `openapi-generator-cli`, which pins its version in `openapitools.json`). Review
the diff of `Generated/Client/AgentMemoryJson.g.cs`: new models appear there as new entries.

## License

Apache-2.0. See the [LICENSE](https://github.com/surrealdb/surrealdb.net/blob/main/LICENSE) file.
