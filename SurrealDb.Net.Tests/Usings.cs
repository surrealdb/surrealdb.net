global using FluentAssertions;
global using SurrealDb.Net.Models;
global using SurrealDb.Net.Models.Auth;
global using SurrealDb.Net.Tests.Extensions;
global using SurrealDb.Net.Tests.Fixtures;
global using CborSerializer = Dahomey.Cbor.Cbor;
global using SurrealDbRecord = SurrealDb.Net.Models.Record;
global using SurrealDbRelationRecord = SurrealDb.Net.Models.RelationRecord;
using TUnit.Core.Interfaces;
using TUnit.Core.Logging;

[assembly: ParallelLimiter<CustomParallelLimit>]

public record CustomParallelLimit : IParallelLimit
{
    // TODO : Parallelism issue with HTTP in v3
    public int Limit => 1;
}

public class AssemblyHooks
{
    private static readonly SurrealDbContainer _container = new();

    [After(TestDiscovery)]
    public static async Task StartAsync(
        TestDiscoveryContext context,
        CancellationToken cancellationToken
    )
    {
        var logger = context.GetDefaultLogger();

        if (!SurrealDbContainer.ShouldUseTestContainers())
        {
            await logger.LogInformationAsync(
                "Testcontainers disabled, using a live instance of surrealdb."
            );
            return;
        }

        await logger.LogInformationAsync("Initializing testcontainers..");
        await _container.InitializeAsync(cancellationToken);
        await logger.LogInformationAsync("Testcontainers initialized.");
    }

    [After(Assembly)]
    public static async Task StopAsync()
    {
        await _container.DisposeAsync();
    }
}
