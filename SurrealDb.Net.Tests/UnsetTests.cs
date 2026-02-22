using System.Text;
using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net.Tests;

public class UnsetTests
{
    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldUnsetParam(string connectionString)
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
            await client.Unset("status");

            {
                string query = "SELECT * FROM post WHERE status == $status;";
                response = (await client.RawQuery(query)).EnsureAllOks();
            }
        };

        await func.Should().NotThrowAsync();

        response.Should().NotBeNull().And.HaveCount(1);

        var firstResult = response![0];
        firstResult.Should().BeOfType<SurrealDbOkResult>();

        var okResult = firstResult as SurrealDbOkResult;
        var list = okResult!.GetValue<List<Post>>();

        list.Should().BeEmpty();
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

            await client.Set("status", "DRAFT");
            await client.Unset(null!);
        };

        await func.Should()
            .ThrowAsync<ArgumentNullException>()
            .WithMessage("Value cannot be null. (Parameter 'key')");
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task KeyShouldBeAlphanumeric(string connectionString)
    {
        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            await using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            await client.ApplySchemaAsync(SurrealSchemaFile.Post);

            await client.Set("st at us", "DRAFT");
            await client.Unset(null!);
        };

        await func.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Variable name is not valid. (Parameter 'key')");
    }
}
