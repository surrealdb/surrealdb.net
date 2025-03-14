namespace SurrealDb.Net.Tests;

public class EmptyRelation : SurrealDbRelationRecord { }

public class WroteRelation : SurrealDbRelationRecord
{
    public DateTime CreatedAt { get; set; }
    public int NumberOfPages { get; set; }
}

public class RelateTests
{
    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldCreateEmptyRelation(string connectionString)
    {
        IEnumerable<EmptyRelation>? list = null;
        EmptyRelation? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            result = await client.Relate<EmptyRelation>("empty", ("in", "one"), ("out", "one"));

            list = await client.Select<EmptyRelation>("empty");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(1);

        result.Should().NotBeNull();
        result!.In.Should().Be(new RecordIdOfString("in", "one"));
        result!.Out.Should().Be(new RecordIdOfString("out", "one"));
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldCreateWroteRelation(string connectionString)
    {
        IEnumerable<WroteRelation>? list = null;
        WroteRelation? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            var data = new WroteRelation { CreatedAt = DateTime.UtcNow, NumberOfPages = 14 };

            result = await client.Relate<WroteRelation, WroteRelation>(
                "wrote",
                ("user", "one"),
                ("post", "one"),
                data
            );

            list = await client.Select<WroteRelation>("wrote");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(1);

        result.Should().NotBeNull();
        result!.In.Should().Be(new RecordIdOfString("user", "one"));
        result!.Out.Should().Be(new RecordIdOfString("post", "one"));
        result!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        result!.NumberOfPages.Should().Be(14);
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldCreateWroteRelationWithPredefinedId(string connectionString)
    {
        IEnumerable<WroteRelation>? list = null;
        WroteRelation? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            var data = new WroteRelation { CreatedAt = DateTime.UtcNow, NumberOfPages = 14 };

            result = await client.Relate<WroteRelation, WroteRelation>(
                ("wrote", "one"),
                ("user", "one"),
                ("post", "one"),
                data
            );

            list = await client.Select<WroteRelation>("wrote");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(1);

        result.Should().NotBeNull();
        result!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        result!.NumberOfPages.Should().Be(14);

        var relationInList = list!.First(r => r.Id!.DeserializeId<string>() == "one");

        relationInList.Should().NotBeNull();
        relationInList!.In.Should().Be(new RecordIdOfString("user", "one"));
        relationInList!.Out.Should().Be(new RecordIdOfString("post", "one"));
        relationInList!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        relationInList!.NumberOfPages.Should().Be(14);
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldCreateWroteRelationWithIdsFromRecordsPreviouslyCreated(
        string connectionString
    )
    {
        IEnumerable<WroteRelation>? list = null;
        WroteRelation? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            var data = new WroteRelation { CreatedAt = DateTime.UtcNow, NumberOfPages = 14 };

            var userOne = await client.Create<User, User>(
                (StringRecordId)"user:one",
                new User { Username = "user", Password = "password" }
            );
            var postOne = await client.Create<Post, Post>(
                (StringRecordId)"post:one",
                new Post { Title = "title", Content = "content" }
            );

            result = await client.Relate<WroteRelation, WroteRelation>(
                ("wrote", "one"),
                userOne.Id!,
                postOne.Id!,
                data
            );

            list = await client.Select<WroteRelation>("wrote");
        };

        await func.Should().NotThrowAsync();

        list.Should().NotBeNull().And.HaveCount(1);

        result.Should().NotBeNull();
        result!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        result!.NumberOfPages.Should().Be(14);

        var relationInList = list!.First(r => r.Id!.DeserializeId<string>() == "one");

        relationInList.Should().NotBeNull();
        relationInList!.In.Should().Be(new RecordIdOfString("user", "one"));
        relationInList!.Out.Should().Be(new RecordIdOfString("post", "one"));
        relationInList!.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
        relationInList!.NumberOfPages.Should().Be(14);
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldCreateRelationWithIdsFromRecordsPreviouslyCreated(
        string connectionString
    )
    {
        var version = await SurrealDbClientGenerator.GetSurrealTestVersion(connectionString);

        if (version.Major < 2)
        {
            return;
        }

        OutputRecord? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            var complexOne = await client.Create(
                new OutputRecord { Id = ("complex", Guid.NewGuid()) }
            );
            var complexTwo = await client.Create(
                new OutputRecord { Id = ("complex", Guid.NewGuid()) }
            );

            result = await client.Relate<OutputRecord>("relation", complexOne.Id!, complexTwo.Id!);
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull();
    }

    private sealed class OutputRecord : Record;
}
