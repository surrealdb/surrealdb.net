#if NET8_0_OR_GREATER
namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class GuidConverterTests : BaseCborConverterTests
{
    [Fact]
    public async Task Serialize()
    {
        var value = Guid.Parse("00010203-0405-0607-0809-0a0b0c0d0e0f");

        string result = await SerializeCborBinaryAsHexaAsync(value);

        result.Should().Be("d82550000102030405060708090a0b0c0d0e0f");
    }

    [Fact]
    public async Task Deserialize()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<Guid>(
            "d82550000102030405060708090a0b0c0d0e0f"
        );

        result.Should().Be(Guid.Parse("00010203-0405-0607-0809-0a0b0c0d0e0f"));
    }
}
#endif
