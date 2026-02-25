using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Net.Internals;

namespace SurrealDb.Net.Tests;

public class TransactionTests
{
    [Test]
    [ConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldSupportTransactions(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        bool result = await client.SupportsTransactions();

        if (client.Engine is SurrealDbWsEngine)
        {
            result.Should().BeTrue();
        }
        else
        {
            result.Should().BeFalse();
        }
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    [BeforeSurrealVersion("3.0")]
    public async Task ShouldNotSupportTransactions(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        bool result = await client.SupportsTransactions();

        result.Should().BeFalse();
    }

    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task BeginTransaction(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();

        await using var client = surrealDbClientGenerator.Create(connectionString);

        await using var session = await client.CreateSession();

        await using var transaction = await session.BeginTransaction();

        transaction.Should().NotBeNull();
    }

    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task CommitTransaction(string connectionString)
    {
        IEnumerable<Post>? beforeCommitList = null;
        IEnumerable<Post>? afterCommitList = null;

        var options = SurrealDbOptions.Create().FromConnectionString(connectionString).Build();

        Func<Task> func = async () =>
        {
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

            await using var transaction = await session.BeginTransaction();

            var post = new Post
            {
                Title = "A new article",
                Content = "This is a new article created using a transaction",
            };

            _ = await session.Create("post", post);

            beforeCommitList = await client.Select<Post>("post");

            await transaction.Commit();

            afterCommitList = await client.Select<Post>("post");
        };

        await func.Should().NotThrowAsync();

        beforeCommitList.Should().HaveCount(2);
        afterCommitList.Should().HaveCount(3);
    }

    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task RollbackTransaction(string connectionString)
    {
        IEnumerable<Post>? afterCommitList = null;

        var options = SurrealDbOptions.Create().FromConnectionString(connectionString).Build();

        Func<Task> func = async () =>
        {
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

            await using var transaction = await session.BeginTransaction();

            var post = new Post
            {
                Title = "A new article",
                Content = "This is a new article created using a transaction",
            };

            _ = await session.Create("post", post);

            await transaction.Rollback();

            afterCommitList = await client.Select<Post>("post");
        };

        await func.Should().NotThrowAsync();

        afterCommitList.Should().HaveCount(2);
    }

    [Test]
    [WebsocketConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldRollbackOnDispose(string connectionString)
    {
        IEnumerable<Post>? afterCommitList = null;

        var options = SurrealDbOptions.Create().FromConnectionString(connectionString).Build();

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            {
                await using var session = await client.CreateSession();

                await session.Use(dbInfo.Namespace, dbInfo.Database);
                if (!options.IsEmbedded)
                {
                    await session.SignIn(
                        new RootAuth { Username = options.Username!, Password = options.Password! }
                    );
                }

                await using var transaction = await session.BeginTransaction();

                var post = new Post
                {
                    Title = "A new article",
                    Content = "This is a new article created using a transaction",
                };

                _ = await session.Create("post", post);
            }

            afterCommitList = await client.Select<Post>("post");
        };

        await func.Should().NotThrowAsync();

        afterCommitList.Should().HaveCount(2);
    }
}
