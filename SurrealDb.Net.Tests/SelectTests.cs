using System.ComponentModel.DataAnnotations.Schema;
using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Tests;

public class Empty : SurrealDbRecord { }

[Table("post")]
public class Post : SurrealDbRecord
{
    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [CborIgnoreIfDefault]
    [Column("created_at")]
    public DateTime? CreatedAt { get; set; }

    [CborIgnoreIfDefault]
    [Column("status")]
    public string? Status { get; set; }
}

public class Person : SurrealDbRecord
{
    public string Title { get; set; } = string.Empty;
    public PersonName Name { get; set; }
    public bool Marketing { get; set; }
}

public struct PersonName
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

public class ObjectTableId
{
    [CborProperty("location")]
    public string Location { get; set; } = string.Empty;

    [CborProperty("year")]
    public int Year { get; set; }
}

public class SelectTests
{
    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldSelectFromEmptyTable(string connectionString)
    {
        IEnumerable<Empty>? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            result = await client.Select<Empty>("empty").ToListAsync();
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull().And.BeEmpty();
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldSelectFromPostTable(string connectionString)
    {
        IEnumerable<Post>? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            result = await client.Select<Post>("post").ToListAsync();
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull().And.HaveCount(2);

        var list = result!.ToList();

        var firstPost = list.FirstOrDefault(p => p.Id! == ("post", "first"));

        firstPost.Should().NotBeNull();
        firstPost!.Title.Should().Be("First article");
        firstPost!.Content.Should().Be("This is my first article");
        firstPost!.CreatedAt.Should().NotBeNull();
        firstPost!.Status.Should().Be("DRAFT");

        var secondPost = list.First(p => p != firstPost);

        secondPost.Should().NotBeNull();
        secondPost!.Title.Should().Be("Second article");
        secondPost!.Content.Should().Be("Another article");
        secondPost!.CreatedAt.Should().NotBeNull();
        secondPost!.Status.Should().Be("DRAFT");
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldSelectSinglePostUsingTwoArguments(string connectionString)
    {
        Post? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            result = await client.Select<Post>(("post", "first"));
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull();
        result!.Title.Should().Be("First article");
        result!.Content.Should().Be("This is my first article");
        result!.CreatedAt.Should().NotBeNull();
        result!.Status.Should().Be("DRAFT");
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldSelectSinglePostUsingRecordId(string connectionString)
    {
        Post? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            var recordId = new RecordIdOfString("post", "first");

            result = await client.Select<Post>(recordId);
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull();
        result!.Title.Should().Be("First article");
        result!.Content.Should().Be("This is my first article");
        result!.CreatedAt.Should().NotBeNull();
        result!.Status.Should().Be("DRAFT");
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldSelectSingleFromNumberId(string connectionString)
    {
        RecordIdRecord? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.RecordId);

            var recordId = RecordId.From("recordId", 17493);

            result = await client.Select<RecordIdRecord>(recordId);
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull();
        result!.Name.Should().Be("number");
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldSelectSingleFromStringId(string connectionString)
    {
        RecordIdRecord? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.RecordId);

            var recordId = RecordId.From("recordId", "surrealdb");

            result = await client.Select<RecordIdRecord>(recordId);
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull();
        result!.Name.Should().Be("string");
    }

    [Test]
    [Skip("Guid not currently handled")]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldSelectSingleFromGuidId(string connectionString)
    {
        RecordIdRecord? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.RecordId);

            var recordId = RecordId.From(
                "recordId",
                new Guid("8424486b-85b3-4448-ac8d-5d51083391c7")
            );

            result = await client.Select<RecordIdRecord>(recordId);
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull();
        result!.Name.Should().Be("complex");
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldSelectSingleFromObjectId(string connectionString)
    {
        RecordIdRecord? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.RecordId);

            var recordId = RecordId.From(
                "recordId",
                new ObjectTableId { Location = "London", Year = 2023 }
            );

            result = await client.Select<RecordIdRecord>(recordId);
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull();
        result!.Name.Should().Be("object");
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldSelectSingleFromArrayId(string connectionString)
    {
        RecordIdRecord? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.RecordId);

            var recordId = RecordId.From("recordId", new object[] { "London", 2023 });

            result = await client.Select<RecordIdRecord>(recordId);
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull();
        result!.Name.Should().Be("array");
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldSelectSingleFromStringRecordIdType(string connectionString)
    {
        RecordIdRecord? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.RecordId);

            var recordId = new StringRecordId("recordId:surrealdb");

            result = await client.Select<RecordIdRecord>(recordId);
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull();
        result!.Name.Should().Be("string");
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldSelectFromRecordIdRange(string connectionString)
    {
        var version = await SurrealDbClientGenerator.GetSurrealTestVersion(connectionString);

        IEnumerable<Empty>? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            const string alphabet = "abcdefghijklmnopqrstuvwxyz";
            var itemsToInsert = new List<Empty>(26);

            foreach (char c in alphabet)
            {
                itemsToInsert.Add(new Empty { Id = ("empty", c.ToString()) });
            }

            await client.Insert("empty", itemsToInsert);

            result = await client.Select<string, string, Empty>(
                new RecordIdRange<string, string>(
                    "empty",
                    RangeBound.Inclusive("b"),
                    RangeBound.Exclusive("d")
                )
            );
        };

        if (version.Major < 2)
        {
            await func.Should().ThrowAsync<NotImplementedException>();
            return;
        }

        await func.Should().NotThrowAsync();

        result
            .Should()
            .BeEquivalentTo([new Empty { Id = ("empty", "b") }, new Empty { Id = ("empty", "c") }]);
    }

    //[Theory]
    //[InlineData("http://127.0.0.1:8000")]
    //[InlineData("ws://127.0.0.1:8000/rpc")]
    //public async Task ShouldSelectWithComplexQuery(string url)
    //{
    //    IEnumerable<string>? result = null;

    //    Func<Task> func = async () =>
    //    {
    //        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
    //        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

    //        string filePath = Path.Combine(
    //            AppDomain.CurrentDomain.BaseDirectory,
    //            "Schemas/post.surql"
    //        );
    //        string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

    //        string query = fileContent;

    //        using var client = surrealDbClientGenerator.Create(url);
    //        await client.SignIn(new RootAuth { Username = "root", Password = "root" });
    //        await client.Use(dbInfo.Namespace, dbInfo.Database);
    //        await client.Query(query);

    //        // TODO : Do not use post but another table with large number of entries
    //        result = client
    //            .Select<Post>("post")
    //            .Where(p => p.Status == "DRAFT")
    //            .OrderBy(p => p.Id)
    //            .ThenBy(p => p.Title)
    //            .Skip(1)
    //            .Take(5)
    //            .Select(p => p.Content);
    //    };

    //    await func.Should().NotThrowAsync();

    //    result.Should().NotBeNull().And.HaveCount(2);
    //}

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldSelectFromGenericTypeNameTable(string connectionString)
    {
        IEnumerable<Post>? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            result = await client.Select<Post>().ToListAsync();
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull().And.HaveCount(2);

        var list = result!.ToList();

        var firstPost = list.FirstOrDefault(p => p.Id! == ("post", "first"));

        firstPost.Should().NotBeNull();
        firstPost!.Title.Should().Be("First article");
        firstPost!.Content.Should().Be("This is my first article");
        firstPost!.CreatedAt.Should().NotBeNull();
        firstPost!.Status.Should().Be("DRAFT");

        var secondPost = list.First(p => p != firstPost);

        secondPost.Should().NotBeNull();
        secondPost!.Title.Should().Be("Second article");
        secondPost!.Content.Should().Be("Another article");
        secondPost!.CreatedAt.Should().NotBeNull();
        secondPost!.Status.Should().Be("DRAFT");
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldSelectFromGenericTypeNameTableSynchronous(string connectionString)
    {
        IEnumerable<Post>? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            result = client.Select<Post>().ToList();
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull().And.HaveCount(2);

        var list = result!.ToList();

        var firstPost = list.FirstOrDefault(p => p.Id! == ("post", "first"));

        firstPost.Should().NotBeNull();
        firstPost!.Title.Should().Be("First article");
        firstPost!.Content.Should().Be("This is my first article");
        firstPost!.CreatedAt.Should().NotBeNull();
        firstPost!.Status.Should().Be("DRAFT");

        var secondPost = list.First(p => p != firstPost);

        secondPost.Should().NotBeNull();
        secondPost!.Title.Should().Be("Second article");
        secondPost!.Content.Should().Be("Another article");
        secondPost!.CreatedAt.Should().NotBeNull();
        secondPost!.Status.Should().Be("DRAFT");
    }
}
