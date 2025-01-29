namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class DoubleConverterTests : BaseCborConverterTests
{
    [Test]
    public async Task Serialize()
    {
        string result = await SerializeCborBinaryAsHexaAsync(174.52137945421947d);

        result.Should().Be("fb4065d0af23f715ba");
    }

    [Test]
    public async Task Deserialize()
    {
        double result = await DeserializeCborBinaryAsHexaAsync<double>("fb4065d0af23f715ba");

        result.Should().Be(174.52137945421947d);
    }
}
