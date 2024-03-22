using System.Numerics;

namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class Vector3ConverterTests : BaseCborConverterTests
{
    [Fact]
    public async Task Serialize()
    {
        var value = new Vector3(14, 32, 21);

        string result = await SerializeCborBinaryAsHexaAsync(value);

        result.Should().Be("83f94b00f95000f94d40");
    }

    [Fact]
    public async Task Deserialize()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<Vector3>("83f94b00f95000f94d40");

        var expected = new Vector3(14, 32, 21);

        result.Should().Be(expected);
    }
}
