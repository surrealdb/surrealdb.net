using Microsoft.Spatial;

namespace SurrealDb.Net.Tests.Serializers.Cbor.Spatial;

public class GeometryCollectionConverterTests : BaseCborConverterTests
{
    private readonly GeometryCollection _geometryCollection;

    public GeometryCollectionConverterTests()
    {
        var factory = GeometryFactory.Collection();

        factory.LineString(0, 0);
        factory.LineTo(1, 1);
        factory.LineTo(1, 0);
        factory.LineTo(0, 0);

        factory.MultiPoint();
        factory.Point(0, 0);
        factory.Point(1, 1);
        factory.Point(1, 0);
        factory.Point(0, 0);

        factory.Polygon();

        factory.Ring(0, 0);
        factory.LineTo(1, 1);
        factory.LineTo(1, 0);
        factory.LineTo(0, 0);

        factory.Ring(4, 4);
        factory.LineTo(0, 0);
        factory.LineTo(2, 2);
        factory.LineTo(4, 4);

        factory.MultiPolygon();

        factory.Polygon();

        factory.Ring(0, 0);
        factory.LineTo(1, 1);
        factory.LineTo(1, 0);
        factory.LineTo(0, 0);

        factory.Ring(4, 4);
        factory.LineTo(0, 0);
        factory.LineTo(2, 2);
        factory.LineTo(4, 4);

        _geometryCollection = factory.Build();
    }

    [Fact]
    public async Task Serialize()
    {
        string result = await SerializeCborBinaryAsHexaAsync(_geometryCollection);

        result
            .Should()
            .Be(
                "d85e84d85984d8588261306130d8588261316131d8588261316130d8588261306130d85b84d8588261306130d8588261316131d8588261316130d8588261306130d85a82d85984d8588261306130d8588261316131d8588261316130d8588261306130d85984d8588261346134d8588261306130d8588261326132d8588261346134d85d81d85a82d85984d8588261306130d8588261316131d8588261316130d8588261306130d85984d8588261346134d8588261306130d8588261326132d8588261346134"
            );
    }

    [Fact]
    public async Task Deserialize()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<GeometryCollection>(
            "d85e84d85984d8588261306130d8588261316131d8588261316130d8588261306130d85b84d8588261306130d8588261316131d8588261316130d8588261306130d85a82d85984d8588261306130d8588261316131d8588261316130d8588261306130d85984d8588261346134d8588261306130d8588261326132d8588261346134d85d81d85a82d85984d8588261306130d8588261316131d8588261316130d8588261306130d85984d8588261346134d8588261306130d8588261326132d8588261346134"
        );

        result.Should().Be(_geometryCollection);
    }
}
