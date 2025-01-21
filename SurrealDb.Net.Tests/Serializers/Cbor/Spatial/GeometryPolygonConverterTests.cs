﻿using Microsoft.Spatial;

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
                "d85a82d85984d8588261306130d8588261316131d8588261316130d8588261306130d85984d8588261346134d8588261306130d8588261326132d8588261346134"
            );
    }

    [Test]
    public async Task Deserialize()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<GeometryPolygon>(
            "d85a82d85984d8588261306130d8588261316131d8588261316130d8588261306130d85984d8588261346134d8588261306130d8588261326132d8588261346134"
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
