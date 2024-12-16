namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class RecordIdConverterTests : BaseCborConverterTests
{
    [Fact]
    public async Task Serialize()
    {
        var value = RecordId.From("post", "first");

        var result = await SerializeCborBinaryAsHexaAsync(value);

        result.Should().Be("c88264706f7374656669727374");
    }

    [Fact]
    public async Task ShouldFailToDeserializeFromStringRecordId()
    {
        var func = async () =>
            await DeserializeCborBinaryAsHexaAsync<StringRecordId>("c86a706f73743a6669727374");

        await func.Should()
            .ThrowAsync<NotSupportedException>()
            .WithMessage("Cannot read StringRecordId from cbor...");
    }

    [Fact]
    public async Task DeserializeFromArrayWithStringId()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<RecordId>("c88264706f7374656669727374");

        var expected = new RecordIdOfString("post", "first");

        result.Should().Be(expected);
    }

    [Fact]
    public async Task DeserializeFromArrayWithIntegerId()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<RecordId>("c88264706f7374193039");

        var expected = new RecordIdOf<int>("post", 12345);

        result.Should().Be(expected);
    }
}
