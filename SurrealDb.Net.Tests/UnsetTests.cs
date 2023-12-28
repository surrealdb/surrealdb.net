using SurrealDb.Net.Models.Response;
using System.Text;

namespace SurrealDb.Net.Tests;

public class UnsetTests
{
    [Theory]
    [InlineData("http://127.0.0.1:8000")]
    [InlineData("ws://127.0.0.1:8000/rpc")]
    public async Task ShouldUnsetParam(string url)
    {
        SurrealDbResponse? response = null;

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
                    "Schemas/post.surql"
                );
                string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

                string query = fileContent;

                await client.Query(query);
            }

            await client.Set("status", "DRAFT");
            await client.Unset("status");

            {
                string query = "SELECT * FROM post WHERE status == $status;";

                response = await client.Query(query);
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

    [Theory]
    [InlineData("http://127.0.0.1:8000")]
    [InlineData("ws://127.0.0.1:8000/rpc")]
    public async Task KeyShouldNotBeNull(string url)
    {
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
                    "Schemas/post.surql"
                );
                string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

                string query = fileContent;

                await client.Query(query);
            }

            await client.Set("status", "DRAFT");
            await client.Unset(null!);
        };

        await func.Should()
            .ThrowAsync<ArgumentNullException>()
            .WithMessage("Value cannot be null. (Parameter 'key')");
    }

    [Theory]
    [InlineData("http://127.0.0.1:8000")]
    [InlineData("ws://127.0.0.1:8000/rpc")]
    public async Task KeyShouldBeAlphanumeric(string url)
    {
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
                    "Schemas/post.surql"
                );
                string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

                string query = fileContent;

                await client.Query(query);
            }

            await client.Set("st at us", "DRAFT");
            await client.Unset(null!);
        };

        await func.Should()
            .ThrowAsync<ArgumentException>()
            .WithMessage("Variable name should be alphanumeric (Parameter 'key')");
    }
}
