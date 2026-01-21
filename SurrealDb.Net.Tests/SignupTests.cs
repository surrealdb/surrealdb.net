namespace SurrealDb.Net.Tests;

public class SignUpTests
{
    [Test]
    [RemoteConnectionStringFixtureGenerator]
    public async Task ShouldSignUpUsingScopeAuth(string connectionString)
    {
        Jwt? jwt = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.User);

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

            jwt = await client.SignUp(authParams);
        };

        await func.Should().NotThrowAsync();

        jwt.Should().NotBeNull();
        jwt!.Value.Token.Should().BeValidJwt();
    }

    [Test]
    [EmbeddedConnectionStringFixtureGenerator]
    public async Task SignUpIsNotSupportedInEmbeddedMode(string connectionString)
    {
        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.User);

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

            await client.SignUp(authParams);
        };

        await func.Should()
            .ThrowAsync<NotSupportedException>()
            .WithMessage("Authentication is not enabled in embedded mode.");
    }
}
