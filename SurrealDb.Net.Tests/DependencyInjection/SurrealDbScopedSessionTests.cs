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
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator(
                ServiceLifetime.Scoped
            );
            surrealDbClientGenerator.Configure(connectionString);
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
    [WebsocketConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldNotShareSessionStateBetweenScopes(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator(
            ServiceLifetime.Scoped
        );
        surrealDbClientGenerator.Configure(connectionString);
        var dbInfo1 = surrealDbClientGenerator.GenerateDatabaseInfo();
        var dbInfo2 = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var scope1 = surrealDbClientGenerator.CreateAsyncScope()!;
        await using var scope2 = surrealDbClientGenerator.CreateAsyncScope()!;

        var client1 = scope1.ServiceProvider.GetRequiredService<SurrealDbSession>();
        var client2 = scope2.ServiceProvider.GetRequiredService<SurrealDbSession>();

        await client1.Use(dbInfo1.Namespace, dbInfo1.Database);
        await client2.Use(dbInfo2.Namespace, dbInfo2.Database);

        // 💡 This state is what a reconnection or a fork replays
        var sessionInfos = ((SurrealDbWsEngine)client1.Engine).SessionInfos;
        sessionInfos.Get(client1.SessionId)!.Ns.Should().Be(dbInfo1.Namespace);
        sessionInfos.Get(client2.SessionId)!.Ns.Should().Be(dbInfo2.Namespace);
    }

    [Test]
    [RemoteConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldNotShareSessionStateBetweenScopesOverHttp(string connectionString)
    {
        var options = SurrealDbOptions.Create().FromConnectionString(connectionString).Build();
        if (new Uri(options.Endpoint!).Scheme is not ("http" or "https"))
            return;

        await using var surrealDbClientGenerator = new SurrealDbClientGenerator(
            ServiceLifetime.Scoped
        );
        surrealDbClientGenerator.Configure(connectionString);
        var dbInfo1 = surrealDbClientGenerator.GenerateDatabaseInfo();
        var dbInfo2 = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var scope1 = surrealDbClientGenerator.CreateAsyncScope()!;
        await using var scope2 = surrealDbClientGenerator.CreateAsyncScope()!;

        var client1 = scope1.ServiceProvider.GetRequiredService<SurrealDbSession>();
        var client2 = scope2.ServiceProvider.GetRequiredService<SurrealDbSession>();
        var engine = (SurrealDbHttpEngine)client1.Engine;

        foreach (var client in new[] { client1, client2 })
        {
            // 💡 The first request of a session creates its state, whatever the server answers
            try
            {
                await engine.Attach(client.SessionId!.Value, CancellationToken.None);
            }
            catch (Exception) { }
        }

        await engine.Use(
            dbInfo1.Namespace,
            dbInfo1.Database,
            client1.SessionId,
            null,
            CancellationToken.None
        );
        await engine.Use(
            dbInfo2.Namespace,
            dbInfo2.Database,
            client2.SessionId,
            null,
            CancellationToken.None
        );

        engine.SessionInfos.Get(client1.SessionId)!.Ns.Should().Be(dbInfo1.Namespace);
        engine.SessionInfos.Get(client2.SessionId)!.Ns.Should().Be(dbInfo2.Namespace);
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldKeepNamespaceAndDatabasePerScope(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator(
            ServiceLifetime.Scoped
        );
        surrealDbClientGenerator.Configure(connectionString);
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

    [Test]
    [Property(
        "issue",
        "https://discord.com/channels/902568124350599239/1014970862031609877/1536036498984538122"
    )]
    [ConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldUseSameSessionWithinMultipleServicesOfTheSameScope(
        string connectionString
    )
    {
        Guid? sessionId1 = null;
        Guid? sessionId2 = null;

        ISurrealDbEngine? engine1 = null;
        ISurrealDbEngine? engine2 = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator(
                ServiceLifetime.Scoped
            );
            surrealDbClientGenerator.RegisterService<SessionTester>();

            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            {
                await using var setupGenerator = new SurrealDbClientGenerator();
                await using var client = setupGenerator.Create(connectionString);
                await client.Use(dbInfo.Namespace, dbInfo.Database);
            }

            surrealDbClientGenerator.Configure(connectionString, dbInfo.Namespace, dbInfo.Database);

            // Define the table up front so the concurrent writes below do not both try
            // to create it implicitly (transaction conflict on embedded engines).
            // Kept alive until the end: the first scope currently owns the embedded engine.
            await using var setupScope = surrealDbClientGenerator.CreateAsyncScope()!;
            var setupSession = setupScope.ServiceProvider.GetRequiredService<SurrealDbSession>();
            await setupSession.RawQuery("DEFINE TABLE inner SCHEMALESS;");

            await using var scope = surrealDbClientGenerator.CreateAsyncScope()!;

            var tester1 = scope.ServiceProvider.GetRequiredService<SessionTester>();
            var tester2 = scope.ServiceProvider.GetRequiredService<SessionTester>();

            Task[] tasks = [tester1.Hello(), tester2.Hello()];
            await Task.WhenAll(tasks);

            sessionId1 = tester1.Session.SessionId;
            engine1 = tester1.Session.Engine;

            sessionId2 = tester2.Session.SessionId;
            engine2 = tester2.Session.Engine;
        };

        await func.Should().NotThrowAsync();

        sessionId1.Should().HaveValue();
        sessionId2.Should().HaveValue();
        sessionId1.Value.Should().Be(sessionId2.Value);

        engine1.Should().NotBeNull();
        engine2.Should().NotBeNull();
        engine1.Should().Be(engine2);
    }

    private class SessionTester(SurrealDbSession session)
    {
        public readonly SurrealDbSession Session = session;

        class InnerRecord : Record;

        public async Task Hello()
        {
            await Session.Create(new InnerRecord { Id = RecordId.From("inner", Guid.NewGuid()) });
        }
    }
}
