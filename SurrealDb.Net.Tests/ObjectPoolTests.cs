using System.Text;
using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Net.Internals;
using SurrealDb.Net.Internals.Constants;

namespace SurrealDb.Net.Tests;

public class ObjectPoolTests
{
    [Test]
    [ConnectionStringFixtureGenerator]
    [SinceSurrealVersion("2.2")]
    public async Task ShouldCreateTwoDistinctEngines(string connectionString)
    {
        var options = new SurrealDbOptionsBuilder().FromConnectionString(connectionString).Build();
        if (options.Endpoint!.StartsWith(EndpointConstants.Client.ROCKSDB))
        {
            // 💡 Multi-locks not allowed with rocksdb
            return;
        }

        ISurrealDbEngine? engine1 = null;
        ISurrealDbEngine? engine2 = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator().Configure(
                connectionString,
                lifetime: ServiceLifetime.Scoped
            );
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var scope1 = surrealDbClientGenerator.CreateScope()!;
            using var scope2 = surrealDbClientGenerator.CreateScope()!;

            var client1 = scope1.ServiceProvider.GetRequiredService<SurrealDbClient>();
            await client1.Use(dbInfo.Namespace, dbInfo.Database);
            engine1 = client1.Engine;

            var client2 = scope2.ServiceProvider.GetRequiredService<SurrealDbClient>();
            await client2.Use(dbInfo.Namespace, dbInfo.Database);
            engine2 = client2.Engine;

            await client1.DisposeAsync();
            await client2.DisposeAsync();
        };

        await func.Should().NotThrowAsync();

        engine1.Should().NotBeNull();
        engine2.Should().NotBeNull();
        engine1.Should().NotBe(engine2);
    }

    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    [SinceSurrealVersion("2.2")]
    public async Task ShouldReuseTheSameEngineAcrossClients(string connectionString)
    {
        ISurrealDbEngine? engine1 = null;
        ISurrealDbEngine? engine2 = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator().Configure(
                connectionString,
                lifetime: ServiceLifetime.Scoped
            );
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var scope1 = surrealDbClientGenerator.CreateScope()!;

            var client1 = scope1.ServiceProvider.GetRequiredService<SurrealDbClient>();
            await client1.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client1.Use(dbInfo.Namespace, dbInfo.Database);
            engine1 = client1.Engine;

            await client1.DisposeAsync();

            using var scope2 = surrealDbClientGenerator.CreateScope()!;

            var client2 = scope2.ServiceProvider.GetRequiredService<SurrealDbClient>();
            await client2.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client2.Use(dbInfo.Namespace, dbInfo.Database);
            engine2 = client2.Engine;

            await client2.DisposeAsync();
        };

        await func.Should().NotThrowAsync();

        engine1.Should().NotBeNull();
        engine2.Should().NotBeNull();
        engine1.Should().Be(engine2);
    }

    [Test]
    [RemoteConnectionStringFixtureGenerator]
    [SinceSurrealVersion("2.2")]
    public async Task ShouldResetAuthWhenClientIsReused(string connectionString)
    {
        var version = await SurrealDbClientGenerator.GetSurrealTestVersion(connectionString);

        User? userSession1 = null;
        User? userSession2 = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator().Configure(
                connectionString,
                lifetime: ServiceLifetime.Scoped
            );
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var scope1 = surrealDbClientGenerator.CreateScope()!;

            var client1 = scope1.ServiceProvider.GetRequiredService<SurrealDbClient>();
            await client1.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client1.Use(dbInfo.Namespace, dbInfo.Database);

            {
                string filePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    $"Schemas/v{version.Major}/user.surql"
                );
                string fileContent = await File.ReadAllTextAsync(filePath, Encoding.UTF8);

                string query = fileContent;
                await client1.RawQuery(query);
            }

            {
                var authParams = new AuthParams
                {
                    Namespace = dbInfo.Namespace,
                    Database = dbInfo.Database,
                    Access = "user_scope",
                    Username = "johndoe",
                    Email = "john.doe@example.com",
                    Password = "password123",
                };

                var jwt = await client1.SignUp(authParams);
                await client1.Authenticate(jwt);
            }

            userSession1 = await client1.Info<User>();

            await client1.DisposeAsync();

            using var scope2 = surrealDbClientGenerator.CreateScope()!;

            await using var client2 = scope2.ServiceProvider.GetRequiredService<SurrealDbClient>();
            await client2.Use(dbInfo.Namespace, dbInfo.Database);

            userSession2 = await client2.Info<User>();
        };

        await func.Should().NotThrowAsync();

        userSession1.Should().NotBeNull();
        userSession2.Should().BeNull();
    }

    [Test]
    [RemoteConnectionStringFixtureGenerator]
    [SinceSurrealVersion("2.2")]
    public async Task ShouldResetDbNs(string connectionString)
    {
        connectionString += ";NS=alpha;DB=alpha";

        DatabaseInfo? dbInfo = null;
        SessionInfo? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator().Configure(
                connectionString,
                lifetime: ServiceLifetime.Scoped
            );
            dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var scope1 = surrealDbClientGenerator.CreateScope()!;

            await using var client1 = scope1.ServiceProvider.GetRequiredService<SurrealDbClient>();
            await client1.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client1.Use(dbInfo.Namespace, dbInfo.Database);

            using var scope2 = surrealDbClientGenerator.CreateScope()!;

            await using var client2 = scope2.ServiceProvider.GetRequiredService<SurrealDbClient>();
            await client2.SignIn(new RootAuth { Username = "root", Password = "root" });

            var response = await client2.Query($"SELECT * FROM $session;");

            var list = response.GetValue<List<SessionInfo>>(0)!;
            result = list[0];
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull();

        result!.Namespace.Should().Be("alpha");
        result!.Database.Should().Be("alpha");
    }
}
