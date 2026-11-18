global using FluentAssertions;
global using SurrealDb.Net.Models.Auth;
global using SurrealDb.Net.Tests.Fixtures;
global using SurrealDbRecord = SurrealDb.Net.Models.Record;
using TUnit.Core.Interfaces;
using TUnit.Core.Logging;

[assembly: ParallelLimiter<CustomParallelLimit>]

public record CustomParallelLimit : IParallelLimit
{
    public int Limit => 4;
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
