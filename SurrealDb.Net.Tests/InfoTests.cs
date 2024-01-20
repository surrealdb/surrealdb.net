﻿using System.Text;

namespace SurrealDb.Net.Tests;

public class User : SurrealDbRecord
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public DateTime RegisteredAt { get; set; }
}

public class InfoTests
{
    [Theory]
    [InlineData("http://127.0.0.1:8000")]
    [InlineData("ws://127.0.0.1:8000/rpc")]
    public async Task ShouldNotRetrieveInfoForRootUser(string url)
    {
        User? currentUser = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(url);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            currentUser = await client.Info<User>();
        };

        await func.Should().NotThrowAsync();

        currentUser.Should().BeNull();
    }

    [Theory]
    [InlineData("http://127.0.0.1:8000")]
    [InlineData("ws://127.0.0.1:8000/rpc")]
    public async Task ShouldRetrieveInfoForScopedUser(string url)
    {
        User? currentUser = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(url);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            {
                string filePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Schemas/user.surql"
                );
                string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

                string query = fileContent;
                await client.RawQuery(query);
            }

            {
                var authParams = new AuthParams
                {
                    Namespace = dbInfo.Namespace,
                    Database = dbInfo.Database,
                    Scope = "user_scope",
                    Username = "johndoe",
                    Email = "john.doe@example.com",
                    Password = "password123"
                };

                var jwt = await client.SignUp(authParams);
                await client.Authenticate(jwt);
            }

            currentUser = await client.Info<User>();
        };

        await func.Should().NotThrowAsync();

        var expected = new User
        {
            Id = currentUser?.Id,
            Username = "johndoe",
            Email = "john.doe@example.com",
            Password = string.Empty, // 💡 Forbid password retrieval
            Avatar = "https://www.gravatar.com/avatar/8eb1b522f60d11fa897de1dc6351b7e8",
            RegisteredAt = currentUser?.RegisteredAt ?? default
        };

        currentUser.Should().BeEquivalentTo(expected);
        currentUser?.Id.Should().NotBeNull();
    }
}
