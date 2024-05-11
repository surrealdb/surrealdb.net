using System.Numerics;

namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class Vector4ConverterTests : BaseCborConverterTests
{
    [Fact]
    public async Task Serialize()
    {
        var value = new Vector4(14, 32, 21, 18);

        string result = await SerializeCborBinaryAsHexaAsync(value);

        result.Should().Be("84f94b00f95000f94d40f94c80");
    }

    [Fact]
    public async Task Deserialize()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<Vector4>("84f94b00f95000f94d40f94c80");

        var expected = new Vector4(14, 32, 21, 18);

        result.Should().Be(expected);
    }
}
