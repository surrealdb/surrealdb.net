using Microsoft.Spatial;

namespace SurrealDb.Net.Tests.Serializers.Cbor.Spatial;

public class GeometryPolygonConverterTests : BaseCborConverterTests
{
    [Test]
    public async Task Serialize()
    {
        var factory = GeometryFactory.Polygon();

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
                "d85a82d85984d85882f90000f90000d85882f93c00f93c00d85882f93c00f90000d85882f90000f90000d85984d85882f94400f94400d85882f90000f90000d85882f94000f94000d85882f94400f94400"
            );
    }

    [Test]
    public async Task Deserialize()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<GeometryPolygon>(
            "d85a82d85984d85882f90000f90000d85882f93c00f93c00d85882f93c00f90000d85882f90000f90000d85984d85882f94400f94400d85882f90000f90000d85882f94000f94000d85882f94400f94400"
        );

        var factory = GeometryFactory.Polygon();

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
