using Microsoft.Spatial;

namespace SurrealDb.Net.Tests.Serializers.Cbor.Spatial;

public class GeographyMultiLineStringConverterTests : BaseCborConverterTests
{
    [Fact]
    public async Task Serialize()
    {
        var factory = GeographyFactory.MultiLineString();

        factory.LineString(0, 0);
        factory.LineTo(1, 1);
        factory.LineTo(1, 0);
        factory.LineTo(0, 0);

        var value = factory.Build();

        string result = await SerializeCborBinaryAsHexaAsync(value);

        result.Should().Be("d85c81d85984d8588261306130d8588261316131d8588261306131d8588261306130");
    }

    [Fact]
    public async Task Deserialize()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<GeographyMultiLineString>(
            "d85c81d85984d8588261306130d8588261316131d8588261306131d8588261306130"
        );

        var factory = GeographyFactory.MultiLineString();

        factory.LineString(0, 0);
        factory.LineTo(1, 1);
        factory.LineTo(1, 0);
        factory.LineTo(0, 0);

        var expected = factory.Build();

        result.Should().Be(expected);
    }
}
