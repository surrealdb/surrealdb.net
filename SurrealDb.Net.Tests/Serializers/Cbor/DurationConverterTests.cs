namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class DurationConverterTests : BaseCborConverterTests
{
    [Fact]
    public async Task Serialize()
    {
        var value = new Duration("38w1d28m3s58ms255µs");

        string result = await SerializeCborBinaryAsHexaAsync(value);

        result.Should().Be("ce821a016007131a0378e698");
    }

    [Fact]
    public async Task Deserialize()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<Duration>("ce821a016007131a0378e698");

        var expected = new Duration("38w1d28m3s58ms255µs");

        result.Should().Be(expected);
    }
}
