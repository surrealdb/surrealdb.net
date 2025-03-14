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

    [Test]
    public async Task Serialize()
    {
        string result = await SerializeCborBinaryAsHexaAsync(_geometryCollection);

        result
            .Should()
            .Be(
                "d85e84d85984d85882f90000f90000d85882f93c00f93c00d85882f93c00f90000d85882f90000f90000d85b84d85882f90000f90000d85882f93c00f93c00d85882f93c00f90000d85882f90000f90000d85a82d85984d85882f90000f90000d85882f93c00f93c00d85882f93c00f90000d85882f90000f90000d85984d85882f94400f94400d85882f90000f90000d85882f94000f94000d85882f94400f94400d85d81d85a82d85984d85882f90000f90000d85882f93c00f93c00d85882f93c00f90000d85882f90000f90000d85984d85882f94400f94400d85882f90000f90000d85882f94000f94000d85882f94400f94400"
            );
    }

    [Test]
    public async Task Deserialize()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<GeometryCollection>(
            "d85e84d85984d85882f90000f90000d85882f93c00f93c00d85882f93c00f90000d85882f90000f90000d85b84d85882f90000f90000d85882f93c00f93c00d85882f93c00f90000d85882f90000f90000d85a82d85984d85882f90000f90000d85882f93c00f93c00d85882f93c00f90000d85882f90000f90000d85984d85882f94400f94400d85882f90000f90000d85882f94000f94000d85882f94400f94400d85d81d85a82d85984d85882f90000f90000d85882f93c00f93c00d85882f93c00f90000d85882f90000f90000d85984d85882f94400f94400d85882f90000f90000d85882f94000f94000d85882f94400f94400"
        );

        result.Should().Be(_geometryCollection);
    }
}
