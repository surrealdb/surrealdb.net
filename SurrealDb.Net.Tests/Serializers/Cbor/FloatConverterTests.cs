namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class FloatConverterTests : BaseCborConverterTests
{
    [Fact]
    public async Task Serialize()
    {
        string result = await SerializeCborBinaryAsHexaAsync(888.44f);

        result.Should().Be("fa445e1c29");
    }

    [Fact]
    public async Task Deserialize()
    {
        float result = await DeserializeCborBinaryAsHexaAsync<float>("fa445e1c29");

        result.Should().Be(888.44f);
    }
}
