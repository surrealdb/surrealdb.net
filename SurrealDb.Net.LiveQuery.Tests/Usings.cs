global using FluentAssertions;
global using SurrealDb.Net.Models.Auth;
global using SurrealDb.Net.Tests.Fixtures;
global using SurrealDbRecord = SurrealDb.Net.Models.Record;
using TUnit.Core.Interfaces;

[assembly: ParallelLimiter<CustomParallelLimit>]

public record CustomParallelLimit : IParallelLimit
{
    public int Limit => 4;
}
