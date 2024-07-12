namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class DecimalConverterTests : BaseCborConverterTests
{
    [Fact]
    public async Task Serialize()
    {
        string result = await SerializeCborBinaryAsHexaAsync(255.69m);

        result.Should().Be("ca663235352e3639");
    }

    [Fact]
    public async Task DeserializeFromString()
    {
        decimal result = await DeserializeCborBinaryAsHexaAsync<decimal>("ca663235352e3639");

        result.Should().Be(255.69m);
    }

    [Fact]
    public async Task DeserializeFromInteger()
    {
        decimal result = await DeserializeCborBinaryAsHexaAsync<decimal>("0a");

        result.Should().Be(10);
    }

    [Fact]
    public async Task DeserializeFromFloat()
    {
        decimal result = await DeserializeCborBinaryAsHexaAsync<decimal>("fb4035570a3d70a3d7");

        result.Should().Be(21.34m);
    }
}
