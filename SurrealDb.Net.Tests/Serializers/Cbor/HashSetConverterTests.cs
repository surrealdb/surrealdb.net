namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class HashSetConverterTests : BaseCborConverterTests
{
    [Test]
    public async Task Serialize()
    {
        var value = new HashSet<string>(["char", "string", "int"]);

        string result = await SerializeCborBinaryAsHexaAsync(value);

        result.Should().Be("d83883646368617266737472696e6763696e74");
    }

    [Test]
    public async Task Deserialize()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<HashSet<string>>(
            "d83883646368617266737472696e6763696e74"
        );

        result.Should().BeEquivalentTo(new HashSet<string>(["char", "string", "int"]));
    }
}
