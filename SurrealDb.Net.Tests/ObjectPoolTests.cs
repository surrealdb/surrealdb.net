using System.Text;
using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Net.Internals;

namespace SurrealDb.Net.Tests;

public class ObjectPoolTests
{
    [Theory]
    [InlineData("Endpoint=http://127.0.0.1:8000;Serialization=CBOR")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=CBOR")]
    public async Task ShouldCreateTwoDistinctEngines(string connectionString)
    {
        ISurrealDbEngine? engine1 = null;
        ISurrealDbEngine? engine2 = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator().Configure(
                connectionString,
                ServiceLifetime.Scoped
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

    [Theory]
    [InlineData("Endpoint=http://127.0.0.1:8000;Serialization=JSON")]
    [InlineData("Endpoint=http://127.0.0.1:8000;Serialization=CBOR")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=JSON")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=CBOR")]
    public async Task ShouldReuseTheSameEngineAcrossClients(string connectionString)
    {
        ISurrealDbEngine? engine1 = null;
        ISurrealDbEngine? engine2 = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator().Configure(
                connectionString
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

    [Theory]
    [InlineData("Endpoint=http://127.0.0.1:8000;Serialization=JSON")]
    [InlineData("Endpoint=http://127.0.0.1:8000;Serialization=CBOR")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=JSON")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=CBOR")]
    public async Task ShouldResetAuthWhenClientIsReused(string connectionString)
    {
        User? userSession1 = null;
        User? userSession2 = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator().Configure(
                connectionString,
                ServiceLifetime.Scoped
            );
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var scope1 = surrealDbClientGenerator.CreateScope()!;

            var client1 = scope1.ServiceProvider.GetRequiredService<SurrealDbClient>();
            await client1.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client1.Use(dbInfo.Namespace, dbInfo.Database);

            {
                string filePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Schemas/user.surql"
                );
                string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

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
                    Password = "password123"
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

    [Theory]
    [InlineData("Endpoint=http://127.0.0.1:8000;Serialization=JSON;NS=alpha;DB=alpha")]
    [InlineData("Endpoint=http://127.0.0.1:8000;Serialization=CBOR;NS=alpha;DB=alpha")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=JSON;NS=alpha;DB=alpha")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=CBOR;NS=alpha;DB=alpha")]
    public async Task ShouldResetDbNs(string connectionString)
    {
        DatabaseInfo? dbInfo = null;
        SessionInfo? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator().Configure(
                connectionString,
                ServiceLifetime.Scoped
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
