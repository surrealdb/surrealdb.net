namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class NoneConverterTests : BaseCborConverterTests
{
    [Fact]
    public async Task Serialize()
    {
        var value = new None();

        string result = await SerializeCborBinaryAsHexaAsync(value);

        result.Should().Be("c6f6");
    }

    [Fact]
    public async Task Deserialize()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<None>("c6f6");

        result.Should().Be(new None());
    }
}
