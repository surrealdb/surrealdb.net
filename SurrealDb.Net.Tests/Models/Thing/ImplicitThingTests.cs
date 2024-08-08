using System.Text;

namespace SurrealDb.Net.Tests.Models;

public class ImplicitThingTests
{
    [Fact]
    public void ShouldCreateThingFromTupleImplicitly()
    {
        Thing thing = ("table", "id");

        thing.Table.ToString().Should().Be("table");
        thing.Id.ToString().Should().Be("id");
        thing.ToString().Should().Be("table:id");
    }

    [Fact]
    public void ShouldCreateThingFromTupleWithIntegerIdImplicitly()
    {
        Thing thing = ("table", 42);

        thing.Table.ToString().Should().Be("table");
        thing.Id.ToString().Should().Be("42");
        thing.ToString().Should().Be("table:42");
    }

    [Theory]
    [InlineData("Endpoint=http://127.0.0.1:8000;Serialization=JSON")]
    [InlineData("Endpoint=http://127.0.0.1:8000;Serialization=CBOR")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=JSON")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;Serialization=CBOR")]
    public async Task ShouldCreateThingFromTupleOnClientMethodCall(string connectionString)
    {
        // Test taken from "SelectTests.cs" file

        RecordIdRecord? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            string filePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Schemas/thing.surql"
            );
            string fileContent = File.ReadAllText(filePath, Encoding.UTF8);
            string query = fileContent;

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.SignIn(new RootAuth { Username = "root", Password = "root" });
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            await client.RawQuery(query);

            result = await client.Select<RecordIdRecord>(("thing", 17493));
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull();
        result!.Name.Should().Be("number");
    }
}
