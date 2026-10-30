using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class SurrealDbQueryStreamFrameConverterTests : BaseCborConverterTests
{
    [Test]
    public async Task DeserializeBeginFrame()
    {
        // {"stream": "begin", "statements": 2}
        var frame = await DeserializeCborBinaryAsHexaAsync<SurrealDbQueryStreamFrame>(
            "a26673747265616d65626567696e6a73746174656d656e747302"
        );

        frame.Should().BeOfType<SurrealDbQueryStreamBeginFrame>();
        ((SurrealDbQueryStreamBeginFrame)frame).Statements.Should().Be(2);
    }

    [Test]
    public async Task DeserializeUnknownFrameShouldNotThrow()
    {
        // {"index": 0, "stream": "progress"}
        var frame = await DeserializeCborBinaryAsHexaAsync<SurrealDbQueryStreamFrame>(
            "a265696e646578006673747265616d6870726f6772657373"
        );

        frame.Should().BeOfType<SurrealDbQueryStreamUnknownFrame>();
    }
}
