using Microsoft.Spatial;

namespace SurrealDb.Net.Tests.Serializers.Cbor.Spatial;

public class GeometryPointConverterTests : BaseCborConverterTests
{
    [Fact]
    public async Task Serialize()
    {
        var value = GeometryPoint.Create(32.0, 64.0);

        string result = await SerializeCborBinaryAsHexaAsync(value);

        result.Should().Be("d85882623332623634");
    }

    [Fact]
    public async Task Deserialize()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<GeometryPoint>("d85882623332623634");

        var expected = GeometryPoint.Create(32.0, 64.0);

        result.Should().Be(expected);
    }
}
