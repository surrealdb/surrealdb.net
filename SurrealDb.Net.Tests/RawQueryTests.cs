using System.Text;
using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net.Tests;

public class RawQueryTests
{
    private readonly VerifySettings _verifySettings = new();

    public RawQueryTests()
    {
        _verifySettings.DisableRequireUniquePrefix();
        _verifySettings.UseDirectory("Snapshots");
        _verifySettings.IgnoreParametersForVerified();
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldQueryWithParams(string connectionString)
    {
        SurrealDbResponse? response = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            {
                string query = "SELECT * FROM post WHERE status == $status;";

                response = await client.RawQuery(
                    query,
                    new Dictionary<string, object?> { { "status", "DRAFT" } }
                );
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
    public async Task ShouldHaveOneProtocolErrorResult(string connectionString)
    {
        var version = await SurrealDbClientGenerator.GetSurrealTestVersion(connectionString);

        string versionFolder = $"v{version.Major}";
        _verifySettings.UseDirectory($"Snapshots/{versionFolder}");

        SurrealDbResponse? response = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            {
                const string query = "abc def;";

                response = await client.RawQuery(query);
            }
        };

        var exception = await func.Should().ThrowAsync<SurrealDbException>();

        await Verify(exception.Which.Message, _verifySettings);
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldHave4Results(string connectionString)
    {
        SurrealDbResponse? response = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            {
                const string query =
                    @"
SELECT * FROM post;
SELECT * FROM empty;
SELECT * FROM post:first;
SELECT xyz FROM post;
";

                response = await client.RawQuery(query);
            }
        };

        await func.Should().NotThrowAsync();

        response.Should().NotBeNull();
        response!.Count.Should().Be(4);
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldIterateOnOkResults(string connectionString)
    {
        SurrealDbResponse? response = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            {
                const string query =
                    @"
SELECT * FROM post;
SELECT * FROM empty;
SELECT * FROM post:first;

BEGIN TRANSACTION;
CREATE post;
CANCEL TRANSACTION;
";

                response = await client.RawQuery(query);
            }
        };

        await func.Should().NotThrowAsync();

        response.Should().NotBeNull();
        response!.Oks.Should().NotBeNull().And.HaveCount(3);
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldIterateOnErrorResults(string connectionString)
    {
        var version = await SurrealDbClientGenerator.GetSurrealTestVersion(connectionString);

        SurrealDbResponse? response = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            {
                const string query =
                    @"
SELECT * FROM post;
SELECT * FROM empty;
SELECT * FROM post:first;

BEGIN TRANSACTION;
CREATE post;
CANCEL TRANSACTION;
";

                response = await client.RawQuery(query);
            }
        };

        await func.Should().NotThrowAsync();

        int numberOfErrors = version.Major >= 3 ? 2 : 1;

        response.Should().NotBeNull();
        response!.Errors.Should().NotBeNull().And.HaveCount(numberOfErrors);
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldReturnFirstOkResult(string connectionString)
    {
        SurrealDbResponse? response = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            {
                const string query =
                    @"
SELECT * FROM post;
SELECT * FROM empty;
SELECT * FROM post:first;

BEGIN TRANSACTION;
CREATE post;
CANCEL TRANSACTION;
";

                response = await client.RawQuery(query);
            }
        };

        await func.Should().NotThrowAsync();

        response.Should().NotBeNull();
        response!.FirstOk.Should().BeOfType<SurrealDbOkResult>();
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldReturnFirstError(string connectionString)
    {
        SurrealDbResponse? response = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            {
                const string query =
                    @"
SELECT * FROM post;
SELECT * FROM empty;
SELECT * FROM post:first;

BEGIN TRANSACTION;
CREATE post;
CANCEL TRANSACTION;
";

                response = await client.RawQuery(query);
            }
        };

        await func.Should().NotThrowAsync();

        response.Should().NotBeNull();
        response!.FirstError.Should().BeOfType<SurrealDbErrorResult>();
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldHaveError(string connectionString)
    {
        SurrealDbResponse? response = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            {
                const string query =
                    @"
SELECT * FROM post;
SELECT * FROM empty;
SELECT * FROM post:first;

BEGIN TRANSACTION;
CREATE post;
CANCEL TRANSACTION;
";

                response = await client.RawQuery(query);
            }
        };

        await func.Should().NotThrowAsync();

        response.Should().NotBeNull();
        response!.HasErrors.Should().BeTrue();
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldGetValueFromIndex(string connectionString)
    {
        SurrealDbResponse? response = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            {
                const string query =
                    @"
SELECT * FROM post;
SELECT * FROM empty;
SELECT * FROM post:first;

BEGIN TRANSACTION;
CREATE post;
CANCEL TRANSACTION;
";

                response = await client.RawQuery(query);
            }
        };

        await func.Should().NotThrowAsync();

        response.Should().NotBeNull();
        var list = response!.GetValue<List<Post>>(0);

        list.Should().NotBeNull().And.HaveCount(2);
    }
}
