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

            await client.Set("value", new ValueSetObject());

            {
                const string query = "RETURN $value;";
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

            await client.Set("value", new ValueSetObject2());

            {
                const string query = "RETURN $value;";
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

            await client.Set("value", new RecursiveStructure());

            {
                const string query = "RETURN $value;";
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
}
