using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Net.Internals;

namespace SurrealDb.Net.Tests;

public class ReconnectTests
{
    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldRestoreRootSessionStateAfterReconnection(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);
        await client.ApplySchemaAsync(SurrealSchemaFile.Post);
        await client.Set("answer", 42);

        await ((SurrealDbWsEngine)client.Engine).ForceReconnectAsync();

        var response = await client.RawQuery(
            "RETURN [session::ns(), session::db(), <string> $answer];"
        );
        response.GetValue<string[]>(0).Should().Equal(dbInfo.Namespace, dbInfo.Database, "42");

        var posts = await client.Select<Post>("post");
        posts.Should().HaveCount(2);
    }

    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldAttachAndRestoreChildSessionsAfterReconnection(string connectionString)
    {
        var options = SurrealDbOptions.Create().FromConnectionString(connectionString).Build();

        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);
        await client.ApplySchemaAsync(SurrealSchemaFile.Post);

        await using var session = await client.CreateSession();
        await session.Use(dbInfo.Namespace, dbInfo.Database);
        await session.SignIn(
            new RootAuth { Username = options.Username!, Password = options.Password! }
        );
        await session.Set("name", "child");

        await ((SurrealDbWsEngine)client.Engine).ForceReconnectAsync();

        var sessions = await client.Sessions();
        sessions.Should().Contain(session.SessionId!.Value);

        var response = await session.RawQuery("RETURN [session::ns(), session::db(), $name];");
        response.GetValue<string[]>(0).Should().Equal(dbInfo.Namespace, dbInfo.Database, "child");

        // 💡 Records are only visible to an authenticated user
        var posts = await session.Select<Post>("post");
        posts.Should().HaveCount(2);
    }
}
