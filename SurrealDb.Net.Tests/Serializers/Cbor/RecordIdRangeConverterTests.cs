namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class RecordIdRangeConverterTests : BaseCborConverterTests
{
    [Fact]
    public async Task Serialize()
    {
        var value = new RecordIdRange<int, string>(
            "table",
            RangeBound.Inclusive(0),
            RangeBound.Exclusive("x")
        );

        string result = await SerializeCborBinaryAsHexaAsync(value);

        result.Should().Be("c882657461626c65d83182d83200d8336178");
    }

    [Fact]
    public async Task Deserialize()
    {
        var act = async () =>
            await DeserializeCborBinaryAsHexaAsync<RecordIdRange<int, string>>(
                "c882657461626c65d83182d83200d8336178"
            );

        await act.Should()
            .ThrowAsync<NotSupportedException>()
            .WithMessage("Cannot read RecordIdRange from cbor...");
    }
}
