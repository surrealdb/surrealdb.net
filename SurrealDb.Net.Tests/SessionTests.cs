using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Net.Internals;
using SurrealDb.Net.Models.Sessions;

namespace SurrealDb.Net.Tests;

public class SessionTests
{
    [Test]
    [ConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldSupportSessions(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        bool result = await client.SupportsSession();

        if (client.Engine is SurrealDbHttpEngine)
        {
            result.Should().BeFalse();
        }
        else
        {
            result.Should().BeTrue();
        }
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    [BeforeSurrealVersion("3.0")]
    public async Task ShouldNotSupportSessions(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        bool result = await client.SupportsSession();

        result.Should().BeFalse();
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ListEmptySessions(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();

        await using var client = surrealDbClientGenerator.Create(connectionString);

        bool supportsSession = await client.SupportsSession();
        if (client.Engine is SurrealDbHttpEngine)
        {
            supportsSession.Should().BeFalse();
            return;
        }

        supportsSession.Should().BeTrue();

        var result = await client.Sessions();

        result.Should().BeEquivalentTo(Array.Empty<Guid>());
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ListMultipleSessions(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();

        await using var client = surrealDbClientGenerator.Create(connectionString);

        bool supportsSession = await client.SupportsSession();
        if (client.Engine is SurrealDbHttpEngine)
        {
            supportsSession.Should().BeFalse();
            return;
        }

        supportsSession.Should().BeTrue();

        var session1 = await client.CreateSession();
        var session2 = await client.CreateSession();
        var session3 = await client.CreateSession();

        var result = await client.Sessions();

        result.Should().HaveCount(3);

        await session1.DisposeAsync();
        await session2.DisposeAsync();
        await session3.DisposeAsync();
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task CreateSession(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await using var result = await client.CreateSession();

        result.SessionId.Should().NotBeNull();
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ForkSession(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        var firstSession = await client.CreateSession();
        await using var result = await firstSession.ForkSession();

        result.SessionId.Should().NotBeNull();

        await firstSession.DisposeAsync();
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ForkSessionShouldKeepNamespaceDatabaseAndAuth(string connectionString)
    {
        var options = SurrealDbOptions.Create().FromConnectionString(connectionString).Build();
        if (new Uri(options.Endpoint!).Scheme is "http" or "https")
            return;

        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);
        await client.ApplySchemaAsync(SurrealSchemaFile.Post);

        await using var session = await client.CreateSession();
        await session.Use(dbInfo.Namespace, dbInfo.Database);
        if (!options.IsEmbedded)
        {
            await session.SignIn(
                new RootAuth { Username = options.Username!, Password = options.Password! }
            );
        }

        await using var fork = await session.ForkSession();

        var response = await fork.RawQuery("RETURN [session::ns(), session::db()];");
        response.GetValue<string[]>(0).Should().Equal(dbInfo.Namespace, dbInfo.Database);

        // 💡 Records are only visible to an authenticated user
        var posts = await fork.Select<Post>("post");
        posts.Should().HaveCount(2);
    }

    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldReplayVariablesBeforeRequestsAfterReconnection(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);
        for (int index = 0; index < 50; index++)
        {
            await client.Set($"v{index}", index);
        }

        await ((SurrealDbWsEngine)client.Engine).ForceReconnectAsync();

        // 💡 Sent while the variables are being replayed, it must wait for the last of them
        var response = await client.RawQuery("RETURN $v49;");
        response.GetValue<int?>(0).Should().Be(49);
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ForkSessionShouldKeepVariablesSetConcurrently(string connectionString)
    {
        var options = SurrealDbOptions.Create().FromConnectionString(connectionString).Build();
        if (new Uri(options.Endpoint!).Scheme is "http" or "https")
            return;

        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);

        await using var session = await client.CreateSession();
        await session.Use(dbInfo.Namespace, dbInfo.Database);

        const int count = 200;
        await Task.WhenAll(
            Enumerable.Range(0, count).Select(index => session.Set($"v{index}", index))
        );

        await using var fork = await session.ForkSession();

        var response = await fork.RawQuery(
            $"RETURN [{string.Join(", ", Enumerable.Range(0, count).Select(index => $"$v{index}"))}];"
        );
        response.GetValue<int?[]>(0).Should().Equal(Enumerable.Range(0, count).Cast<int?>());
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ForkSessionShouldKeepVariables(string connectionString)
    {
        var options = SurrealDbOptions.Create().FromConnectionString(connectionString).Build();
        if (new Uri(options.Endpoint!).Scheme is "http" or "https")
            return;

        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);

        await using var session = await client.CreateSession();
        await session.Use(dbInfo.Namespace, dbInfo.Database);
        await session.Set("answer", 42);
        await session.Set("removed", "value");
        await session.Unset("removed");

        await using var fork = await session.ForkSession();

        var response = await fork.RawQuery("RETURN [$answer, $removed];");
        response.GetValue<int?[]>(0).Should().Equal(42, null);
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task CloseSession(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        var result = await client.CreateSession();
        await result.CloseSession();

        result.SessionState.Should().Be(SessionState.Closed);

        await result.DisposeAsync();
    }
}
