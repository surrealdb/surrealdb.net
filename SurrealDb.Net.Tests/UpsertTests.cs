namespace SurrealDb.Net.Tests;

public class UpsertTests
{
    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldCreateNewPost(string connectionString)
    {
        IEnumerable<Post>? list = null;
        Post? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            var post = new Post
            {
                Id = ("post", "another"),
                Title = "A new article",
                Content = "This is a new article created using the .NET SDK",
            };

            result = await client.Upsert(post);

            list = await client.Select<Post>("post").ToListAsync();
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(3);

        result.Should().NotBeNull();
        result!.Title.Should().Be("A new article");
        result!.Content.Should().Be("This is a new article created using the .NET SDK");
        result!.CreatedAt.Should().NotBeNull();
        result!.Status.Should().Be("DRAFT");

        var anotherPost = list!.First(p => p.Id! == ("post", "another"));

        anotherPost.Should().NotBeNull();
        anotherPost!.Title.Should().Be("A new article");
        anotherPost!.Content.Should().Be("This is a new article created using the .NET SDK");
        anotherPost!.CreatedAt.Should().NotBeNull();
        anotherPost!.Status.Should().Be("DRAFT");
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldUpdateExistingPost(string connectionString)
    {
        IEnumerable<Post>? list = null;
        Post? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            var existingCreatedAt = DateTime.UtcNow;
            string existingStatus = "DRAFT";

            var post = new Post
            {
                Id = ("post", "first"),
                Title = "[Updated] First article",
                Content = "[Edit] This is my first article",
                CreatedAt = existingCreatedAt,
                Status = existingStatus,
            };

            result = await client.Upsert(post);

            list = await client.Select<Post>("post").ToListAsync();
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(2);

        result.Should().NotBeNull();
        result!.Title.Should().Be("[Updated] First article");
        result!.Content.Should().Be("[Edit] This is my first article");
        result!.CreatedAt.Should().NotBeNull();
        result!.Status.Should().Be("DRAFT");
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldCreateNewPostUsingStringRecordId(string connectionString)
    {
        IEnumerable<Post>? list = null;
        Post? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            var post = new Post
            {
                Title = "A new article",
                Content = "This is a new article created using the .NET SDK",
            };

            result = await client.Upsert<Post, Post>(new StringRecordId("post:another"), post);

            list = await client.Select<Post>("post").ToListAsync();
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(3);

        result.Should().NotBeNull();
        result!.Title.Should().Be("A new article");
        result!.Content.Should().Be("This is a new article created using the .NET SDK");
        result!.CreatedAt.Should().NotBeNull();
        result!.Status.Should().Be("DRAFT");

        var anotherPost = list!.First(r => r.Id!.DeserializeId<string>() == "another");

        anotherPost.Should().NotBeNull();
        anotherPost!.Title.Should().Be("A new article");
        anotherPost!.Content.Should().Be("This is a new article created using the .NET SDK");
        anotherPost!.CreatedAt.Should().NotBeNull();
        anotherPost!.Status.Should().Be("DRAFT");
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldUpdateExistingPostUsingStringRecordId(string connectionString)
    {
        IEnumerable<Post>? list = null;
        Post? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            var existingCreatedAt = DateTime.UtcNow;
            string existingStatus = "DRAFT";

            var post = new Post
            {
                Title = "[Updated] First article",
                Content = "[Edit] This is my first article",
                CreatedAt = existingCreatedAt,
                Status = existingStatus,
            };

            result = await client.Upsert<Post, Post>(new StringRecordId("post:first"), post);

            list = await client.Select<Post>("post").ToListAsync();
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(2);

        result.Should().NotBeNull();
        result!.Title.Should().Be("[Updated] First article");
        result!.Content.Should().Be("[Edit] This is my first article");
        result!.CreatedAt.Should().NotBeNull();
        result!.Status.Should().Be("DRAFT");
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    [SinceSurrealVersion("2.1")]
    public async Task ShouldCreatePersonUsingADictionary(string connectionString)
    {
        Person? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            var person = new Dictionary<string, object>
            {
                { "title", "Mr." },
                {
                    "name",
                    new Dictionary<string, object>
                    {
                        { "first_name", "Jaime" },
                        { "last_name", "Lannister" },
                    }
                },
                { "marketing", false },
            };

            var list = await client.Upsert<Dictionary<string, object>, Person>("person", person);
            result = list.Single();
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull();
        result!.Title.Should().Be("Mr.");
        result!.Name.FirstName.Should().Be("Jaime");
        result!.Name.LastName.Should().Be("Lannister");
        result!.Marketing.Should().BeFalse();
    }
}
