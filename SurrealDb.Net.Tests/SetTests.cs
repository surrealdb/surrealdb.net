using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net.Tests;

public class SetTests
{
    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldSetParam(string connectionString)
    {
        SurrealDbResponse? response = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            await client.Set("status", "DRAFT");

            {
                const string query = "SELECT * FROM post WHERE status == $status;";
                response = (await client.RawQuery(query)).EnsureAllOks();
            }
        };

        await func.Should().NotThrowAsync();

        response.Should().NotBeNull().And.HaveCount(1);

        var firstResult = response![0];
        firstResult.Should().BeOfType<SurrealDbOkResult>();

        var okResult = firstResult as SurrealDbOkResult;
        var list = okResult!.GetValue<List<Post>>();

        list.Should().NotBeNull().And.HaveCount(2);
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task KeyShouldNotBeNull(string connectionString)
    {
        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            await client.Set(null!, "DRAFT");
        };

        await func.Should()
            .ThrowAsync<ArgumentNullException>()
            .WithMessage("Value cannot be null. (Parameter 'key')");
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task KeyShouldNotBeEmpty(string connectionString)
    {
        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            await client.Set(string.Empty, "DRAFT");
        };

        await func.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Variable name is not valid. (Parameter 'key')");
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task KeyShouldNotContainWhitepace(string connectionString)
    {
        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            await client.Set("st at us", "DRAFT");
        };

        await func.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Variable name is not valid. (Parameter 'key')");
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task KeyCanContainUnderscore(string connectionString)
    {
        SurrealDbResponse? response = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            await client.Set("st_at_us", "DRAFT");

            {
                const string query = "SELECT * FROM post WHERE status == $st_at_us;";
                response = (await client.RawQuery(query)).EnsureAllOks();
            }
        };

        await func.Should().NotThrowAsync();

        response.Should().NotBeNull().And.HaveCount(1);

        var firstResult = response![0];
        firstResult.Should().BeOfType<SurrealDbOkResult>();

        var okResult = firstResult as SurrealDbOkResult;
        var list = okResult!.GetValue<List<Post>>();

        list.Should().NotBeNull().And.HaveCount(2);
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ComplexKeyShouldBeSurroundedWithBackticks(string connectionString)
    {
        SurrealDbResponse? response = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            await client.Set("a.b@c.d", "DRAFT");

            {
                const string query = "SELECT * FROM post WHERE status == $`a.b@c.d`;";
                response = (await client.RawQuery(query)).EnsureAllOks();
            }
        };

        await func.Should().NotThrowAsync();

        response.Should().NotBeNull().And.HaveCount(1);

        var firstResult = response![0];
        firstResult.Should().BeOfType<SurrealDbOkResult>();

        var okResult = firstResult as SurrealDbOkResult;
        var list = okResult!.GetValue<List<Post>>();

        list.Should().NotBeNull().And.HaveCount(2);
    }

    private class ValueSetObject
    {
        public RecordId? Id { get; set; }
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldSetObjectParam(string connectionString)
    {
        SurrealDbResponse? response = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            await client.Set("obj", new ValueSetObject());

            {
                const string query = "RETURN $obj;";
                response = (await client.RawQuery(query)).EnsureAllOks();
            }
        };

        await func.Should().NotThrowAsync();

        response.Should().NotBeNull().And.HaveCount(1);

        var firstResult = response![0];
        firstResult.Should().BeOfType<SurrealDbOkResult>();

        var okResult = firstResult as SurrealDbOkResult;
        var outputValue = okResult!.GetValue<ValueSetObject>();

        outputValue.Should().NotBeNull();
        outputValue!.Id.Should().BeNull();
    }

    private class ValueSetObject2
    {
        public RecordIdOf<string>? Id { get; set; }
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldSetObjectParam2(string connectionString)
    {
        SurrealDbResponse? response = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            await client.Set("obj", new ValueSetObject2());

            {
                const string query = "RETURN $obj;";
                response = (await client.RawQuery(query)).EnsureAllOks();
            }
        };

        await func.Should().NotThrowAsync();

        response.Should().NotBeNull().And.HaveCount(1);

        var firstResult = response![0];
        firstResult.Should().BeOfType<SurrealDbOkResult>();

        var okResult = firstResult as SurrealDbOkResult;
        var outputValue = okResult!.GetValue<ValueSetObject2>();

        outputValue.Should().NotBeNull();
        outputValue!.Id.Should().BeNull();
    }

    private class RecursiveStructure
    {
        public string Name { get; set; } = string.Empty;
        public RecursiveStructure? Inner { get; set; }
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldSetObjectWithRecursiveStructure(string connectionString)
    {
        SurrealDbResponse? response = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            await client.Set("obj", new RecursiveStructure());

            {
                const string query = "RETURN $obj;";
                response = (await client.RawQuery(query)).EnsureAllOks();
            }
        };

        await func.Should().NotThrowAsync();

        response.Should().NotBeNull().And.HaveCount(1);

        var firstResult = response![0];
        firstResult.Should().BeOfType<SurrealDbOkResult>();

        var okResult = firstResult as SurrealDbOkResult;
        var outputValue = okResult!.GetValue<RecursiveStructure>();

        outputValue.Should().NotBeNull();
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldKeepTheValueAsItWasWhenSet(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        var tags = new List<string> { "a" };
        await client.Set("tags", tags);

        // 💡 A later change to the object must not reach the server
        tags.Add("b");

        var response = await client.RawQuery("RETURN $tags;");
        response.GetValue<List<string>>(0).Should().Equal("a");
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldForkTheValueAsItWasWhenSet(string connectionString)
    {
        var options = SurrealDbOptions.Create().FromConnectionString(connectionString).Build();
        if (new Uri(options.Endpoint!).Scheme is "http" or "https")
        {
            // 💡 Sessions are not supported over HTTP
            return;
        }

        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        await using var session = await client.CreateSession();
        await session.Use(dbInfo.Namespace, dbInfo.Database);

        var tags = new List<string> { "a" };
        await session.Set("tags", tags);
        tags.Add("b");

        await using var fork = await session.ForkSession();

        var response = await fork.RawQuery("RETURN $tags;");
        response.GetValue<List<string>>(0).Should().Equal("a");
    }
}
