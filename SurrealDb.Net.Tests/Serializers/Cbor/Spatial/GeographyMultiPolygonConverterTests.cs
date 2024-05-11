using Microsoft.Spatial;

namespace SurrealDb.Net.Tests.Serializers.Cbor.Spatial;

public class GeographyMultiPolygonConverterTests : BaseCborConverterTests
{
    [Fact]
    public async Task Serialize()
    {
        var factory = GeographyFactory.MultiPolygon();

        factory.Polygon();

        factory.Ring(0, 0);
        factory.LineTo(1, 1);
        factory.LineTo(1, 0);
        factory.LineTo(0, 0);

        factory.Ring(4, 4);
        factory.LineTo(0, 0);
        factory.LineTo(2, 2);
        factory.LineTo(4, 4);

        var value = factory.Build();

        string result = await SerializeCborBinaryAsHexaAsync(value);

        result
            .Should()
            .Be(
                "d85d81d85a82d85984d8588261306130d8588261316131d8588261306131d8588261306130d85984d8588261346134d8588261306130d8588261326132d8588261346134"
            );
    }

    [Fact]
    public async Task Deserialize()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<GeographyMultiPolygon>(
            "d85d81d85a82d85984d8588261306130d8588261316131d8588261306131d8588261306130d85984d8588261346134d8588261306130d8588261326132d8588261346134"
        );

        var factory = GeographyFactory.MultiPolygon();

        factory.Polygon();

        factory.Ring(0, 0);
        factory.LineTo(1, 1);
        factory.LineTo(1, 0);
        factory.LineTo(0, 0);

        factory.Ring(4, 4);
        factory.LineTo(0, 0);
        factory.LineTo(2, 2);
        factory.LineTo(4, 4);

        var expected = factory.Build();

        result.Should().Be(expected);
    }
}
