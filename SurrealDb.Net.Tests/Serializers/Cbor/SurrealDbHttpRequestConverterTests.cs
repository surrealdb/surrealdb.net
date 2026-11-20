using SurrealDb.Net.Internals.Cbor;
using SurrealDb.Net.Internals.Cbor.Converters;
using SurrealDb.Net.Internals.Http;

namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class SurrealDbHttpRequestConverterTests : BaseCborConverterTests
{
    [Test]
    public void ShouldBeRegistered()
    {
        SurrealDbCborOptions
            .Default.Value.Registry.ConverterRegistry.Lookup<SurrealDbHttpRequest>()
            .Should()
            .BeOfType<SurrealDbHttpRequestConverter>();
    }

    [Test]
    public void SerializeWithoutOptionalProperties()
    {
        var value = new SurrealDbHttpRequest { Method = "ping" };

        var result = SerializeAsHexa(value);

        // {"method": "ping"}
        result.Should().Be("a1666d6574686f646470696e67");
        result.Should().Be(SerializeWithReflectionAsHexa(value));
    }

    [Test]
    public void SerializeWithAllProperties()
    {
        var value = new SurrealDbHttpRequest
        {
            Method = "upsert",
            Parameters = [RecordId.From("post", 1), new RootAuth { Username = "root" }],
            SessionId = new Guid("7a5fd2a1-6a28-4b47-9b1e-35d7e9c6c0b6"),
        };

        var result = SerializeAsHexa(value);

        result.Should().Be(SerializeWithReflectionAsHexa(value));
    }

    [Test]
    public void SerializeNullParameter()
    {
        var value = new SurrealDbHttpRequest { Method = "use", Parameters = [null, "db"] };

        var result = SerializeAsHexa(value);

        // {"method": "use", "params": [null, "db"]}
        result.Should().Be("a2666d6574686f646375736566706172616d7382f6626462");
    }

    [Test]
    public async Task ShouldNotDeserialize()
    {
        var func = async () =>
            await DeserializeCborBinaryAsHexaAsync<SurrealDbHttpRequest>(
                "a1666d6574686f646470696e67"
            );

        await func.Should().ThrowAsync<NotSupportedException>();
    }
}
