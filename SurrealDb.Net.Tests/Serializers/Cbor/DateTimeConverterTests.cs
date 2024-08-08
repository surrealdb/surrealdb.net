namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class DateTimeConverterTests : BaseCborConverterTests
{
    [Fact]
    public async Task Serialize()
    {
        string result = await SerializeCborBinaryAsHexaAsync(
            DateTime.Parse("2024-03-24T13:30:26.1623225Z").ToUniversalTime()
        );

        result.Should().Be("cc821a66002af21a09acd844");
    }

    [Fact]
    public async Task Deserialize()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<DateTime>("cc821a66002af21a09acd844");

        var expected = DateTime.Parse("2024-03-24T13:30:26.1623225Z").ToUniversalTime();

        result.Should().Be(expected);
    }
}
