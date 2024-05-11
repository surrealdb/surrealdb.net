using System.Net;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net.Tests;

public class RawQueryTests
{
    [Theory]
    [InlineData("Endpoint=http://127.0.0.1:8000")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=JSON")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=CBOR")]
    public async Task ShouldQueryWithParams(string connectionString)
    {
        SurrealDbResponse? response = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            {
                string filePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Schemas/post.surql"
                );
                string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

                string query = fileContent;

                await client.RawQuery(query);
            }

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

    [Theory]
    [InlineData("Endpoint=http://127.0.0.1:8000")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=JSON")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=CBOR")]
    public async Task ShouldHaveOneProtocolErrorResult(string connectionString)
    {
        var options = SurrealDbOptions.Create().FromConnectionString(connectionString).Build();
        bool isWebsocket = options.Endpoint!.StartsWith(
            "ws://",
            StringComparison.OrdinalIgnoreCase
        );

        SurrealDbResponse? response = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            {
                string filePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Schemas/post.surql"
                );
                string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

                string query = fileContent;

                await client.RawQuery(query);
            }

            {
                string query = "abc def;";

                response = await client.RawQuery(query);
            }
        };

        if (isWebsocket)
        {
            await func.Should()
                .ThrowAsync<SurrealDbException>()
                .WithMessage(
                    @"There was a problem with the database: Parse error: Failed to parse query at line 1 column 5 expected query to end
  |
1 | abc def;
  |     ^ perhaps missing a semicolon on the previous statement?
"
                );
        }
        else
        {
            await func.Should().NotThrowAsync();

            response.Should().NotBeNull().And.HaveCount(1);

            var firstResult = response![0];
            firstResult.Should().BeOfType<SurrealDbProtocolErrorResult>();

            var errorResult = firstResult as SurrealDbProtocolErrorResult;

            errorResult!.Code.Should().Be(HttpStatusCode.BadRequest);
            errorResult!.Details.Should().Be("Request problems detected");
            errorResult!
                .Description.Should()
                .Be(
                    "There is a problem with your request. Refer to the documentation for further information."
                );
            errorResult!
                .Information.Should()
                .Contain(
                    @"There was a problem with the database: Parse error: Failed to parse query at line 1 column 5 expected query to end"
                );
        }
    }

    [Theory]
    [InlineData("Endpoint=http://127.0.0.1:8000")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=JSON")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=CBOR")]
    public async Task ShouldHave4Results(string connectionString)
    {
        SurrealDbResponse? response = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            {
                string filePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Schemas/post.surql"
                );
                string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

                string query = fileContent;

                await client.RawQuery(query);
            }

            {
                string query =
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

    [Theory]
    [InlineData("Endpoint=http://127.0.0.1:8000")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=JSON")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=CBOR")]
    public async Task ShouldIterateOnOkResults(string connectionString)
    {
        SurrealDbResponse? response = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            {
                string filePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Schemas/post.surql"
                );
                string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

                string query = fileContent;

                await client.RawQuery(query);
            }

            {
                string query =
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

    [Theory]
    [InlineData("Endpoint=http://127.0.0.1:8000")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=JSON")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=CBOR")]
    public async Task ShouldIterateOnErrorResults(string connectionString)
    {
        SurrealDbResponse? response = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            {
                string filePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Schemas/post.surql"
                );
                string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

                string query = fileContent;

                await client.RawQuery(query);
            }

            {
                string query =
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
        response!.Errors.Should().NotBeNull().And.HaveCount(1);
    }

    [Theory]
    [InlineData("Endpoint=http://127.0.0.1:8000")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=JSON")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=CBOR")]
    public async Task ShouldReturnFirstOkResult(string connectionString)
    {
        SurrealDbResponse? response = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            {
                string filePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Schemas/post.surql"
                );
                string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

                string query = fileContent;

                await client.RawQuery(query);
            }

            {
                string query =
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

    [Theory]
    [InlineData("Endpoint=http://127.0.0.1:8000")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=JSON")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=CBOR")]
    public async Task ShouldReturnFirstError(string connectionString)
    {
        SurrealDbResponse? response = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            {
                string filePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Schemas/post.surql"
                );
                string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

                string query = fileContent;

                await client.RawQuery(query);
            }

            {
                string query =
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

    [Theory]
    [InlineData("Endpoint=http://127.0.0.1:8000")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=JSON")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=CBOR")]
    public async Task ShouldHaveError(string connectionString)
    {
        SurrealDbResponse? response = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            {
                string filePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Schemas/post.surql"
                );
                string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

                string query = fileContent;

                await client.RawQuery(query);
            }

            {
                string query =
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

    [Theory]
    [InlineData("Endpoint=http://127.0.0.1:8000")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=JSON")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=CBOR")]
    public async Task ShouldGetValueFromIndex(string connectionString)
    {
        SurrealDbResponse? response = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);

            {
                string filePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Schemas/post.surql"
                );
                string fileContent = File.ReadAllText(filePath, Encoding.UTF8);

                string query = fileContent;

                await client.RawQuery(query);
            }

            {
                string query =
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
