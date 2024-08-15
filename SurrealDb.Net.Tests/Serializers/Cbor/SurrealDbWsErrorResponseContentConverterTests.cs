using SurrealDb.Net.Internals.Ws;

namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class SurrealDbWsErrorResponseContentConverterTests : BaseCborConverterTests
{
    [Fact]
    public void CannotSerialize()
    {
        var value = new SurrealDbWsErrorResponseContent { Code = 1, Message = "Error message" };

        Func<Task> act = () => SerializeCborBinaryAsHexaAsync(value);

        act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task Deserialize()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<SurrealDbWsErrorResponseContent>(
            "a264636f646504676d6573736167656d4572726f72206d657373616765"
        );

        result.Code.Should().Be(4);
        result.Message.Should().Be("Error message");
    }
}
