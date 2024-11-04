namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class FutureConverterTests : BaseCborConverterTests
{
    [Fact]
    public async Task Serialize()
    {
        var value = new Future("fn::myFunction()");

        string result = await SerializeCborBinaryAsHexaAsync(value);

        result.Should().Be("cf70666e3a3a6d7946756e6374696f6e2829");
    }

    [Fact]
    public async Task Deserialize()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<Future>(
            "cf70666e3a3a6d7946756e6374696f6e2829"
        );

        result.Should().Be(new Future("fn::myFunction()"));
    }
}
