# surrealdb.net

The official SurrealDB library for .NET

[![](https://img.shields.io/badge/status-beta-ff00bb.svg?style=flat-square)](https://github.com/surrealdb/surrealdb.net) [![](https://img.shields.io/badge/docs-view-44cc11.svg?style=flat-square)](https://surrealdb.com/docs/integration/libraries/dotnet) [![](https://img.shields.io/badge/license-Apache_License_2.0-00bfff.svg?style=flat-square)](https://github.com/surrealdb/surrealdb.net)

## Work in Progress
This fork is still in early development and not ready for a PR. Some major design decisions that still need to be made:

- Integration with DI, where and how. The project currently assumes DI and relies on it to wire up its internals.
- Integration with AspNetCore. Think request-scoped DI, AspNetCore identity.
- In terms of naming and conventions, do we fully follow similar conventions to other surrealdb drivers, or common conventions in the .NET ecosystem, or somewhere in the middle?
- .NET standard or .NET 7/8+?

One of the next steps is to expose a `ISurrealClient` that internally uses all driver implementations (rpc and http) to offer the widest set of methods for end-users. Part of this will be keeping the driver-specific interfaces internal.

## TODO List
- Benchmarks
- More robust tests
- Console App sample
- AspNetCore Api sample
- Implement mappings for geo types
- Implement mappings for more date/time types (currently only `surrealdb:duration` <-> `System.TimeSpan` is implemented)
- Much more...

## Currently Supported Features
- Text RPC engine (all methods as documented in the [docs](https://surrealdb.com/docs/integration/websocket/text)) including live queries
- HTTP engine (all endpoints as documented in the [docs](https://surrealdb.com/docs/integration/http)) including import/export

## Testing strategy
The current setup for testing is done through the `testcontainers` NuGet package. The spinned up container is shared across all test classes to prevent an explosion of docker containers.

##  Not planned
- Implementing `DbConnection` and related types. I'm not even sure if its possible, but I leave this to others to try.
- Implementing an EF Core provider. I rarely use EF Core and will leave the implementation of a surrealdb ef core driver to others.
