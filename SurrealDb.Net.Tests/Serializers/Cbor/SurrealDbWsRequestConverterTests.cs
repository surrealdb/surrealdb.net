using System.Collections.Immutable;
using SurrealDb.Net.Internals.Cbor;
using SurrealDb.Net.Internals.Cbor.Converters;
using SurrealDb.Net.Internals.Ws;

namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class SurrealDbWsRequestConverterTests : BaseCborConverterTests
{
    [Test]
    public void ShouldBeRegistered()
    {
        SurrealDbCborOptions
            .Default.Value.Registry.ConverterRegistry.Lookup<SurrealDbWsRequest>()
            .Should()
            .BeOfType<SurrealDbWsRequestConverter>();
    }

    [Test]
    public void SerializeWithoutOptionalProperties()
    {
        var value = new SurrealDbWsRequest { Id = "1", Method = "ping" };

        var result = SerializeAsHexa(value);

        // {"id": "1", "method": "ping"}
        result.Should().Be("a26269646131666d6574686f646470696e67");
        result.Should().Be(SerializeWithReflectionAsHexa(value));
    }

    [Test]
    public void SerializeWithAllProperties()
    {
        var value = new SurrealDbWsRequest
        {
            Id = "abc",
            Method = "query",
            Parameters =
            [
                "SELECT * FROM $id",
                new Dictionary<string, object?> { { "id", RecordId.From("post", 1) } },
            ],
            SessionId = new Guid("7a5fd2a1-6a28-4b47-9b1e-35d7e9c6c0b6"),
            TransactionId = new Guid("0b6fd34c-6c4b-4a6e-8a8d-6f16c1bc3c47"),
        };

        var result = SerializeAsHexa(value);

        result.Should().Be(SerializeWithReflectionAsHexa(value));
    }

    [Test]
    public void SerializeEmptyParameters()
    {
        var value = new SurrealDbWsRequest
        {
            Id = "1",
            Method = "query",
            Parameters = ["RETURN 1", ImmutableDictionary<string, object?>.Empty],
        };

        var result = SerializeAsHexa(value);

        // {"id": "1", "method": "query", "params": ["RETURN 1", {}]}
        result
            .Should()
            .Be("a36269646131666d6574686f6465717565727966706172616d73826852455455524e2031a0");
    }

    [Test]
    public async Task ShouldNotDeserialize()
    {
        var func = async () =>
            await DeserializeCborBinaryAsHexaAsync<SurrealDbWsRequest>(
                "a26269646131666d6574686f646470696e67"
            );

        await func.Should().ThrowAsync<NotSupportedException>();
    }
}
