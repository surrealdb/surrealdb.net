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

        result.Should().BeTrue();
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
