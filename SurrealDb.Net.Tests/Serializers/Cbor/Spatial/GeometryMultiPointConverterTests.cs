using Microsoft.Spatial;

namespace SurrealDb.Net.Tests.Serializers.Cbor.Spatial;

public class GeometryMultiPointConverterTests : BaseCborConverterTests
{
    [Fact]
    public async Task Serialize()
    {
        var factory = GeometryFactory.MultiPoint();

        factory.Point(0, 0);
        factory.Point(1, 1);
        factory.Point(1, 0);
        factory.Point(0, 0);

        var value = factory.Build();

        string result = await SerializeCborBinaryAsHexaAsync(value);

        result.Should().Be("d85b84d8588261306130d8588261316131d8588261316130d8588261306130");
    }

    [Fact]
    public async Task Deserialize()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<GeometryMultiPoint>(
            "d85b84d8588261306130d8588261316131d8588261316130d8588261306130"
        );

        var factory = GeometryFactory.MultiPoint();

        factory.Point(0, 0);
        factory.Point(1, 1);
        factory.Point(1, 0);
        factory.Point(0, 0);

        var expected = factory.Build();

        result.Should().Be(expected);
    }
}
