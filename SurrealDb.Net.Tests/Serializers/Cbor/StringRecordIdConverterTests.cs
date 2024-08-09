namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class StringRecordIdConverterTests : BaseCborConverterTests
{
    [Fact]
    public async Task Serialize()
    {
        string result = await SerializeCborBinaryAsHexaAsync(new StringRecordId("table:id"));

        result.Should().Be("c8687461626c653a6964");
    }

    [Fact]
    public async Task DeserializeShouldThrowException()
    {
        var act = async () =>
            await DeserializeCborBinaryAsHexaAsync<StringRecordId>("c8687461626c653a6964");

        await act.Should()
            .ThrowAsync<NotSupportedException>()
            .WithMessage("Cannot read StringRecordId from cbor...");
    }
}
