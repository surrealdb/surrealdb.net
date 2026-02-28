using SurrealDb.Net.Internals.Errors;

namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class RpcErrorResponseContentConverterTests : BaseCborConverterTests
{
    [Test]
    public void CannotSerialize()
    {
        var value = new RpcErrorResponseContent { Code = 1, Message = "Error message" };

        Func<Task> act = () => SerializeCborBinaryAsHexaAsync(value);

        act.Should().ThrowAsync<NotImplementedException>();
    }

    [Test]
    public async Task Deserialize()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<RpcErrorResponseContent>(
            "a264636f646504676d6573736167656d4572726f72206d657373616765"
        );

        result.Code.Should().Be(4);
        result.Message.Should().Be("Error message");
    }
}
