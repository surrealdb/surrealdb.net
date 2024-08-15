namespace SurrealDb.Net.Tests;

public class VersionTests
{
    [Theory]
    [InlineData("Endpoint=mem://")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root;Serialization=JSON")]
    [InlineData("Endpoint=http://127.0.0.1:8000;User=root;Pass=root;Serialization=CBOR")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root;Serialization=JSON")]
    [InlineData("Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root;Serialization=CBOR")]
    public async Task ShouldGetVersion(string connectionString)
    {
        await using var surrealDbClientGenerator = new SurrealDbClientGenerator();

        using var client = surrealDbClientGenerator.Create(connectionString);
        string result = await client.Version();

        result.Should().BeValidSemver();
    }
}
