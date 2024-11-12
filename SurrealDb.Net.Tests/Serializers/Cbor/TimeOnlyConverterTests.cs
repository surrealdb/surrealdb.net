#if NET7_0_OR_GREATER
namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class TimeOnlyConverterTests : BaseCborConverterTests
{
    [Fact]
    public async Task Serialize()
    {
        var value = new TimeOnly(0, 28, 3, 58, 255);

        string result = await SerializeCborBinaryAsHexaAsync(value);

        result.Should().Be("ce821906931a0378e698");
    }

    [Fact]
    public async Task Deserialize()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<TimeOnly>("ce821906931a0378e698");

        var expected = new TimeOnly(0, 28, 3, 58, 255);

        result.Should().Be(expected);
    }
}
#endif
