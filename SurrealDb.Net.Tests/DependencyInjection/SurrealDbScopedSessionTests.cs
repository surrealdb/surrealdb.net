using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Net.Internals;

namespace SurrealDb.Net.Tests.DependencyInjection;

public class SurrealDbScopedSessionTests
{
    [Test]
    [ConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldCreateTwoSessions(string connectionString)
    {
        Guid? sessionId1 = null;
        Guid? sessionId2 = null;

        ISurrealDbEngine? engine1 = null;
        ISurrealDbEngine? engine2 = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator().Configure(
                connectionString,
                lifetime: ServiceLifetime.Scoped
            );
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var scope1 = surrealDbClientGenerator.CreateAsyncScope()!;
            await using var scope2 = surrealDbClientGenerator.CreateAsyncScope()!;

            var client1 = scope1.ServiceProvider.GetRequiredService<SurrealDbSession>();
            await client1.Use(dbInfo.Namespace, dbInfo.Database);
            sessionId1 = client1.SessionId;
            engine1 = client1.Engine;

            var client2 = scope2.ServiceProvider.GetRequiredService<SurrealDbSession>();
            await client2.Use(dbInfo.Namespace, dbInfo.Database);
            sessionId2 = client2.SessionId;
            engine2 = client2.Engine;

            await client1.DisposeAsync();
            await client2.DisposeAsync();
        };

        await func.Should().NotThrowAsync();

        sessionId1.Should().HaveValue();
        sessionId2.Should().HaveValue();
        sessionId1.Value.Should().NotBe(sessionId2.Value);

        engine1.Should().NotBeNull();
        engine2.Should().NotBeNull();
        engine1.Should().Be(engine2);
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldKeepNamespaceAndDatabasePerScope(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator().Configure(
            connectionString,
            lifetime: ServiceLifetime.Scoped
        );
        var dbInfo1 = surrealDbClientGenerator.GenerateDatabaseInfo();
        var dbInfo2 = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var scope1 = surrealDbClientGenerator.CreateAsyncScope()!;
        await using var scope2 = surrealDbClientGenerator.CreateAsyncScope()!;

        var client1 = scope1.ServiceProvider.GetRequiredService<SurrealDbSession>();
        var client2 = scope2.ServiceProvider.GetRequiredService<SurrealDbSession>();

        await client1.Use(dbInfo1.Namespace, dbInfo1.Database);
        await client2.Use(dbInfo2.Namespace, dbInfo2.Database);

        const string query = "RETURN [session::ns(), session::db()];";

        var response1 = await client1.RawQuery(query);
        response1.GetValue<string[]>(0).Should().Equal(dbInfo1.Namespace, dbInfo1.Database);

        var response2 = await client2.RawQuery(query);
        response2.GetValue<string[]>(0).Should().Equal(dbInfo2.Namespace, dbInfo2.Database);
    }
}
