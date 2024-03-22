namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class DateOnlyConverterTests : BaseCborConverterTests
{
    [Fact]
    public async Task Serialize()
    {
        string result = await SerializeCborBinaryAsHexaAsync(DateOnly.Parse("2024-03-24"));

        result.Should().Be("cc821a65ff6d0000");
    }

    [Fact]
    public async Task Deserialize()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<DateOnly>("cc821a65ff6d0000");

        var expected = DateOnly.Parse("2024-03-24");

        result.Should().Be(expected);
    }
}
