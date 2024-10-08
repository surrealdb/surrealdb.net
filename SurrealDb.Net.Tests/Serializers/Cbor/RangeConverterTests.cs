namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class RangeConverterTests : BaseCborConverterTests
{
    [Fact]
    public async Task Serialize()
    {
        var value = new Range<int, string>(RangeBound.Inclusive(0), RangeBound.Exclusive("x"));

        string result = await SerializeCborBinaryAsHexaAsync(value);

        result.Should().Be("d83182d83200d8336178");
    }

    [Fact]
    public async Task Deserialize()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<Range<int, string>>(
            "d83182d83200d8336178"
        );

        result
            .Should()
            .Be(new Range<int, string>(RangeBound.Inclusive(0), RangeBound.Exclusive("x")));
    }
}
