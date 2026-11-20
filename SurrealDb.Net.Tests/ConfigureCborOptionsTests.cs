using Microsoft.Extensions.DependencyInjection;

namespace SurrealDb.Net.Tests;

public class ConfigureCborOptionsTests
{
    [Test]
    [RemoteConnectionStringFixtureGenerator]
    public async Task ShouldConfigureCborOptionsOncePerClient(string connectionString)
    {
        int configureCount = 0;

        await using var client = new SurrealDbClient(
            SurrealDbOptions.Create().FromConnectionString(connectionString).Build(),
            configureCborOptions: _ => Interlocked.Increment(ref configureCount)
        );

        await client.Version();
        await client.Version();
        await client.Health();

        configureCount.Should().Be(1);
    }
}
