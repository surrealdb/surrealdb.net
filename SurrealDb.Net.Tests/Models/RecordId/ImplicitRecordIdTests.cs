using System.Text;

namespace SurrealDb.Net.Tests.Models;

public class ImplicitRecordIdTests
{
    [Test]
    public void ShouldCreateRecordIdFromTupleImplicitly()
    {
        RecordId recordId = ("table", "id");

        recordId.Table.Should().Be("table");
        recordId.DeserializeId<string>().Should().Be("id");
    }

    [Test]
    public void ShouldCreateRecordIdFromTupleWithIntegerIdImplicitly()
    {
        RecordId recordId = ("table", 844654);

        recordId.Table.Should().Be("table");
        recordId.DeserializeId<int>().Should().Be(844654);
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldCreateRecordIdFromTupleOnClientMethodCall(string connectionString)
    {
        // Test taken from "SelectTests.cs" file

        RecordIdRecord? result = null;

        Func<Task> func = async () =>
        {
            await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
            var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

            string filePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Schemas/recordId.surql"
            );
            string fileContent = File.ReadAllText(filePath, Encoding.UTF8);
            string query = fileContent;

            using var client = surrealDbClientGenerator.Create(connectionString);
            await client.Use(dbInfo.Namespace, dbInfo.Database);
            (await client.RawQuery(query)).EnsureAllOks();

            result = await client.Select<RecordIdRecord>(("recordId", 17493));
        };

        await func.Should().NotThrowAsync();

        result.Should().NotBeNull();
        result!.Name.Should().Be("number");
    }
}
