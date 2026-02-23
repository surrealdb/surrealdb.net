global using FluentAssertions;
global using SurrealDb.Net.Models;
global using SurrealDb.Net.Models.Auth;
global using SurrealDb.Net.Tests.Extensions;
global using SurrealDb.Net.Tests.Fixtures;
global using CborSerializer = Dahomey.Cbor.Cbor;
global using SurrealDbRecord = SurrealDb.Net.Models.Record;
global using SurrealDbRelationRecord = SurrealDb.Net.Models.RelationRecord;
using TUnit.Core.Interfaces;

[assembly: ParallelLimiter<CustomParallelLimit>]

public record CustomParallelLimit : IParallelLimit
{
    // TODO : Parallelism issue with HTTP in v3
    public int Limit => 1;
}
