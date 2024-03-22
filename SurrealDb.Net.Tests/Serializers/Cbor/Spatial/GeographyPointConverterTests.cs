using Microsoft.Spatial;

namespace SurrealDb.Net.Tests.Serializers.Cbor.Spatial;

public class GeographyPointConverterTests : BaseCborConverterTests
{
    [Fact]
    public async Task Serialize()
    {
        var value = GeographyPoint.Create(32.0, 64.0);

        string result = await SerializeCborBinaryAsHexaAsync(value);

        result.Should().Be("d85882623634623332");
    }

    [Fact]
    public async Task Deserialize()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<GeographyPoint>("d85882623634623332");

        var expected = GeographyPoint.Create(32.0, 64.0);

        result.Should().Be(expected);
    }
}
