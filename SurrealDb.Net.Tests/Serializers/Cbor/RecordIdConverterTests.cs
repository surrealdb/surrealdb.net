namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class RecordIdConverterTests : BaseCborConverterTests
{
    [Fact]
    public async Task Serialize()
    {
        var value = RecordId.From("post", "first");

        var result = await SerializeCborBinaryAsHexaAsync(value);

        result.Should().Be("c86a706f73743a6669727374");
    }

    [Fact]
    public async Task DeserializeFromString()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<RecordId>("c86a706f73743a6669727374");

        var expected = new RecordId("post:first");

        result.Should().Be(expected);
    }

    [Fact]
    public async Task DeserializeFromArrayWithStringId()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<RecordId>("ca8264706f7374656669727374");

        var expected = new RecordId("post:first");

        result.Should().Be(expected);
    }

    [Fact]
    public async Task DeserializeFromArrayWithIntegerId()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<RecordId>("ca8264706f7374193039");

        var expected = new RecordId("post:12345");

        result.Should().Be(expected);
    }
}
