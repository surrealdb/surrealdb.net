namespace SurrealDb.Net.Tests.Queryable;

public class ExtensionsQueryableTests : BaseQueryableTests
{
    private const string? DefaultNamingPolicy = null;
    private readonly VerifySettings _verifySettings = new();

    public ExtensionsQueryableTests()
    {
        _verifySettings.UseDirectory("Snapshots");
        _verifySettings.IgnoreParameters();
    }

    [Test]
    public void ToQueryString()
    {
        string query = Posts.OrderBy(p => p.CreatedAt).ToQueryString();

        query
            .Should()
            .Be(
                """
                SELECT content AS Content, created_at AS CreatedAt, id AS Id, status AS Status, title AS Title FROM post ORDER BY created_at
                """
            );
    }

    [Test]
    public void ToQueryStringWithParameters()
    {
        string country = "USA";
        string city = "New York";
        string query = Addresses
            .Where(a => a.IsActive && a.Country == country && a.City == city)
            .ToQueryString();

        query
            .Should()
            .Be(
                """
                SELECT City, Country, id AS Id, IsActive, Number, State, Street, ZipCode FROM address WHERE IsActive && Country == $country && City == $city
                """
            );
    }

    [Test]
    [RemoteConnectionStringFixtureGenerator]
    public async Task ToListAsync(string connectionString)
    {
        var func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            await client.Delete("post");

            var post = new Post
            {
                Id = new RecordIdOfString("post", "another"),
                Title = "A new article",
                Content = "This is a new article created using the .NET SDK",
            };

            await client.Create(post);

            return await client.Select<Post>("post").ToListAsync();
        };

        var assert = await func.Should().NotThrowAsync();

        await Verify(assert.Subject, _verifySettings);
    }

    [Test]
    [RemoteConnectionStringFixtureGenerator]
    public async Task ToArrayAsync(string connectionString)
    {
        var func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            await client.Delete("post");

            var post = new Post
            {
                Id = new RecordIdOfString("post", "another"),
                Title = "A new article",
                Content = "This is a new article created using the .NET SDK",
            };

            await client.Create(post);

            return await client.Select<Post>("post").ToArrayAsync();
        };

        var assert = await func.Should().NotThrowAsync();

        await Verify(assert.Subject, _verifySettings);
    }

    [Test]
    [RemoteConnectionStringFixtureGenerator]
    public async Task CountAsync(string connectionString)
    {
        var func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            await client.Delete("post");

            var post = new Post
            {
                Id = new RecordIdOfString("post", "another"),
                Title = "A new article",
                Content = "This is a new article created using the .NET SDK",
            };

            await client.Create(post);

            return await client.Select<Post>("post").CountAsync();
        };

        var assert = await func.Should().NotThrowAsync();

        assert.Subject.Should().Be(1);
    }

    [Test]
    [RemoteConnectionStringFixtureGenerator]
    public async Task CountAsyncWithPredicate(string connectionString)
    {
        var func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            await client.Delete("post");

            var postOne = new Post
            {
                Id = new RecordIdOfString("post", "one"),
                Title = "A new article",
                Content = "This is a new article created using the .NET SDK",
            };
            var postTwo = new Post
            {
                Id = new RecordIdOfString("post", "two"),
                Title = "Another article",
                Content = "This is a new article created using the .NET SDK",
            };
            var postThree = new Post
            {
                Id = new RecordIdOfString("post", "three"),
                Title = "Another article",
                Content = "This is a new article created using the .NET SDK",
            };

            await client.Create(postOne);
            await client.Create(postTwo);
            await client.Create(postThree);

            return await client.Select<Post>("post").CountAsync(p => p.Title == "Another article");
        };

        var assert = await func.Should().NotThrowAsync();

        assert.Subject.Should().Be(2);
    }

    [Test]
    [RemoteConnectionStringFixtureGenerator]
    public async Task LongCountAsync(string connectionString)
    {
        var func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            await client.Delete("post");

            var post = new Post
            {
                Id = new RecordIdOfString("post", "another"),
                Title = "A new article",
                Content = "This is a new article created using the .NET SDK",
            };

            await client.Create(post);

            return await client.Select<Post>("post").LongCountAsync();
        };

        var assert = await func.Should().NotThrowAsync();

        assert.Subject.Should().Be(1);
    }

    [Test]
    [RemoteConnectionStringFixtureGenerator]
    public async Task LongCountAsyncWithPredicate(string connectionString)
    {
        var func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            await client.Delete("post");

            var postOne = new Post
            {
                Id = new RecordIdOfString("post", "one"),
                Title = "A new article",
                Content = "This is a new article created using the .NET SDK",
            };
            var postTwo = new Post
            {
                Id = new RecordIdOfString("post", "two"),
                Title = "Another article",
                Content = "This is a new article created using the .NET SDK",
            };
            var postThree = new Post
            {
                Id = new RecordIdOfString("post", "three"),
                Title = "Another article",
                Content = "This is a new article created using the .NET SDK",
            };

            await client.Create(postOne);
            await client.Create(postTwo);
            await client.Create(postThree);

            return await client
                .Select<Post>("post")
                .LongCountAsync(p => p.Title == "Another article");
        };

        var assert = await func.Should().NotThrowAsync();

        assert.Subject.Should().Be(2);
    }

    [Test]
    [RemoteConnectionStringFixtureGenerator]
    public async Task ElementAtAsync(string connectionString)
    {
        var func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            await client.Delete("post");

            var postOne = new Post
            {
                Id = new RecordIdOfString("post", "one"),
                Title = "A new article",
                Content = "This is a new article created using the .NET SDK",
            };
            var postTwo = new Post
            {
                Id = new RecordIdOfString("post", "two"),
                Title = "Another article",
                Content = "This is a new article created using the .NET SDK",
            };
            var postThree = new Post
            {
                Id = new RecordIdOfString("post", "three"),
                Title = "Another article",
                Content = "This is a new article created using the .NET SDK",
            };

            await client.Create(postOne);
            await client.Create(postTwo);
            await client.Create(postThree);

            await using var client2 = surrealDbClientGenerator.Create(
                connectionString,
                namingPolicy: DefaultNamingPolicy
            );
            await client2.Use(dbInfo.Namespace, dbInfo.Database);

            return await client2.Select<Post>("post").ElementAtAsync(1);
        };

        var assert = await func.Should().NotThrowAsync();

        await Verify(assert.Subject, _verifySettings);
    }

    [Test]
    [RemoteConnectionStringFixtureGenerator]
    public async Task ElementAtOrDefaultAsync(string connectionString)
    {
        var func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            await client.Delete("post");

            var postOne = new Post
            {
                Id = new RecordIdOfString("post", "one"),
                Title = "A new article",
                Content = "This is a new article created using the .NET SDK",
            };
            var postTwo = new Post
            {
                Id = new RecordIdOfString("post", "two"),
                Title = "Another article",
                Content = "This is a new article created using the .NET SDK",
            };
            var postThree = new Post
            {
                Id = new RecordIdOfString("post", "three"),
                Title = "Another article",
                Content = "This is a new article created using the .NET SDK",
            };

            await client.Create(postOne);
            await client.Create(postTwo);
            await client.Create(postThree);

            await using var client2 = surrealDbClientGenerator.Create(
                connectionString,
                namingPolicy: DefaultNamingPolicy
            );
            await client2.Use(dbInfo.Namespace, dbInfo.Database);

            return await client2.Select<Post>("post").ElementAtOrDefaultAsync(1);
        };

        var assert = await func.Should().NotThrowAsync();

        await Verify(assert.Subject, _verifySettings);
    }
}
