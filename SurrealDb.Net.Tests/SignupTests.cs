﻿using System.Text;

namespace SurrealDb.Net.Tests;

public class SignUpTests
{
    [Theory]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root;Serialization=JSON")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root;Serialization=CBOR")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root;Serialization=JSON")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root;Serialization=CBOR")]
    public async Task ShouldSignUpUsingScopeAuth(string connectionString)
    {
        Jwt? jwt = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            string filePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Schemas/user.surql"
            );
            string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

            string query = fileContent;
            await client.RawQuery(query);

            var authParams = new AuthParams
            {
                Namespace = dbInfo.Namespace,
                Database = dbInfo.Database,
                Scope = "user_scope",
                Username = "johndoe",
                Email = "john.doe@example.com",
                Password = "password123"
            };

            jwt = await client.SignUp(authParams);
        };

        await func.Should().NotThrowAsync();

        jwt.Should().NotBeNull();
        jwt!.Value.Token.Should().BeValidJwt();
    }

    [Theory]
    [InlineData("Endpoint=mem://")]
    public async Task SignUpIsNotSupportedInEmbeddedMode(string connectionString)
    {
        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            string filePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Schemas/user.surql"
            );
            string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

            string query = fileContent;
            await client.RawQuery(query);

            var authParams = new AuthParams
            {
                Namespace = dbInfo.Namespace,
                Database = dbInfo.Database,
                Scope = "user_scope",
                Username = "johndoe",
                Email = "john.doe@example.com",
                Password = "password123"
            };

            await client.SignUp(authParams);
        };

        await func.Should()
            .ThrowAsync<NotSupportedException>()
            .WithMessage("Authentication is not enabled in embedded mode.");
    }
}
