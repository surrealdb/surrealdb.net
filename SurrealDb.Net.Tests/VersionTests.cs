namespace SurrealDb.Net.Tests;

public class VersionTests
{
    [Test]
    [ConnectionStringFixtureGenerator]
    public async Task ShouldGetVersion(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();

        await using var client = surrealDbClientGenerator.Create(connectionString);
        string result = await client.Version();

        result.Should().BeValidSemver();
    }
}
