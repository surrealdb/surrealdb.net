namespace SurrealDb.Net.Tests;

public class AuthenticateTests
{
    [Test]
    [RemoteConnectionStringFixtureGenerator]
    public async Task ShouldAuthenticate(string connectionString)
    {
        Tokens? tokens = null;
        IEnumerable<Post>? list = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.User);
            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

#pragma warning disable CS0618 // Type or member is obsolete
            var authParams = new AuthParams
            {
                Namespace = dbInfo.Namespace,
                Database = dbInfo.Database,
                Scope = "user_scope",
                Access = "user_scope",
                Username = "johndoe",
                Email = "john.doe@example.com",
                Password = "password123",
            };
#pragma warning restore CS0618 // Type or member is obsolete

            tokens = await client.SignUp(authParams);

            await client.Authenticate(tokens);

            list = await client.Select<Post>("post");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(2);
    }

    [Test]
    [EmbeddedConnectionStringFixtureGenerator]
    public async Task InvalidateIsNotSupportedInEmbeddedMode(string connectionString)
    {
        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.User);
            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

#pragma warning disable CS0618 // Type or member is obsolete
            var authParams = new AuthParams
            {
                Namespace = dbInfo.Namespace,
                Database = dbInfo.Database,
                Scope = "user_scope",
                Access = "user_scope",
                Username = "johndoe",
                Email = "john.doe@example.com",
                Password = "password123",
            };
#pragma warning restore CS0618 // Type or member is obsolete

            var jwt = await client.SignUp(authParams);

            await client.Authenticate(jwt);
        };

        await func.Should()
            .ThrowAsync<NotSupportedException>()
            .WithMessage("Authentication is not enabled in embedded mode.");
    }

    [Test]
    [RemoteConnectionStringFixtureGenerator]
    public async Task ShouldFailWhenInvalidate(string connectionString)
    {
        Tokens? tokens = null;
        IEnumerable<Post>? list = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.User);
            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

#pragma warning disable CS0618 // Type or member is obsolete
            var authParams = new AuthParams
            {
                Namespace = dbInfo.Namespace,
                Database = dbInfo.Database,
                Scope = "user_scope",
                Access = "user_scope",
                Username = "johndoe",
                Email = "john.doe@example.com",
                Password = "password123",
            };
#pragma warning restore CS0618 // Type or member is obsolete

            tokens = await client.SignUp(authParams);

            await client.Invalidate();

            list = await client.Select<Post>("post");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(0);
    }
}
