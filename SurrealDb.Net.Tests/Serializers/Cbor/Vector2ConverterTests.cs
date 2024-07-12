using System.Numerics;

namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class Vector2ConverterTests : BaseCborConverterTests
{
    [Fact]
    public async Task Serialize()
    {
        var value = new Vector2(14, 32);

        string result = await SerializeCborBinaryAsHexaAsync(value);

        result.Should().Be("82f94b00f95000");
    }

    [Fact]
    public async Task Deserialize()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<Vector2>("82f94b00f95000");

        var expected = new Vector2(14, 32);

        result.Should().Be(expected);
    }
}
