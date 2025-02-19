using Microsoft.Spatial;

namespace SurrealDb.Net.Tests.Serializers.Cbor.Spatial;

public class GeographyMultiPointConverterTests : BaseCborConverterTests
{
    [Test]
    public async Task Serialize()
    {
        var factory = GeographyFactory.MultiPoint();

        factory.Point(0, 0);
        factory.Point(1, 1);
        factory.Point(1, 0);
        factory.Point(0, 0);

        var value = factory.Build();

        string result = await SerializeCborBinaryAsHexaAsync(value);

        result
            .Should()
            .Be("d85b84d85882f90000f90000d85882f93c00f93c00d85882f90000f93c00d85882f90000f90000");
    }

    [Test]
    public async Task Deserialize()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<GeographyMultiPoint>(
            "d85b84d85882f90000f90000d85882f93c00f93c00d85882f90000f93c00d85882f90000f90000"
        );

        var factory = GeographyFactory.MultiPoint();

        factory.Point(0, 0);
        factory.Point(1, 1);
        factory.Point(1, 0);
        factory.Point(0, 0);

        var expected = factory.Build();

        result.Should().Be(expected);
    }
}
