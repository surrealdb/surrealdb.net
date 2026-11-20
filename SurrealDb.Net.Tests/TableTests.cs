namespace SurrealDb.Net.Tests;

public class TableTests
{
    [Test]
    [ConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldReceiveTable(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);

        var response = await client.RawQuery("RETURN type::table('post');");

        response.GetValue<Table>(0).Should().Be(new Table("post"));
    }

    [Test]
    [ConnectionStringFixtureGenerator]
    [SinceSurrealVersion("3.0")]
    public async Task ShouldSendTable(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();
        var dbInfo = surrealDbClientGenerator.GenerateDatabaseInfo();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        await client.Use(dbInfo.Namespace, dbInfo.Database);
        await client.ApplySchemaAsync(SurrealSchemaFile.Post);

        var parameters = new Dictionary<string, object?> { { "tb", new Table("post") } };

        var response = await client.RawQuery("RETURN $tb;", parameters);
        response.GetValue<Table>(0).Should().Be(new Table("post"));

        response = await client.RawQuery("RETURN count(SELECT * FROM $tb);", parameters);
        response.GetValue<int>(0).Should().Be(2);
    }
}
