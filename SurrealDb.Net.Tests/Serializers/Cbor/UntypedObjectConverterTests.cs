using System.Collections.Immutable;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Internals.Cbor;
using SurrealDb.Net.Internals.Cbor.Converters;

namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class UntypedObjectConverterTests : BaseCborConverterTests
{
    private static string SerializeAsObjectHexa(object? value)
    {
        return SerializeAsHexa(value);
    }

    [Test]
    public void ShouldBeRegistered()
    {
        SurrealDbCborOptions
            .Default.Value.Registry.ConverterRegistry.Lookup<object>()
            .Should()
            .BeOfType<UntypedObjectConverter>();
    }

    [Test]
    public void SerializeNull()
    {
        SerializeAsObjectHexa(null).Should().Be("f6");
    }

    [Test]
    public void SerializePlainObject()
    {
        SerializeAsObjectHexa(new object()).Should().Be("a0");
    }

    [Test]
    public void SerializeWithTheConverterOfTheRuntimeType()
    {
        SerializeAsObjectHexa("hello").Should().Be(SerializeAsHexa("hello"));
        SerializeAsObjectHexa(42).Should().Be(SerializeAsHexa(42));
        SerializeAsObjectHexa(new Duration(86400))
            .Should()
            .Be(SerializeAsHexa(new Duration(86400)));
    }

    [Test]
    public void SerializeRecordIdOfAnyType()
    {
        RecordId[] recordIds =
        [
            RecordId.From("post", "first"),
            RecordId.From("post", 1),
            RecordId.From("post", 9999999999L),
            RecordId.From("post", new Guid("7a5fd2a1-6a28-4b47-9b1e-35d7e9c6c0b6")),
            RecordId.From("post", new[] { "a", "b" }),
        ];

        foreach (var recordId in recordIds)
        {
            SerializeAsObjectHexa(recordId).Should().Be(SerializeAsHexa(recordId));
        }
    }

    [Test]
    public void SerializeRecordIdOfInt()
    {
        // 8(["post", 1])
        SerializeAsObjectHexa(RecordId.From("post", 1)).Should().Be("c88264706f737401");
    }

    [Test]
    public void SerializeDictionary()
    {
        var value = new Dictionary<string, object?>
        {
            { "name", "John" },
            { "age", 42 },
            { "id", RecordId.From("user", "john") },
            { "nothing", null },
        };

        var result = SerializeAsObjectHexa(value);

        var reference = new DictionaryConverter<Dictionary<string, object?>, string, object?>(
            SurrealDbCborOptions.Default.Value
        );
        result.Should().Be(SerializeAsHexa(value, reference));
    }

    [Test]
    public void SerializeImmutableDictionary()
    {
        var value = ImmutableDictionary<string, object?>.Empty.Add("name", "John");

        var result = SerializeAsObjectHexa(value);

        // {"name": "John"}
        result.Should().Be("a1646e616d65644a6f686e");
    }

    [Test]
    public void SerializeEmptyImmutableDictionary()
    {
        SerializeAsObjectHexa(ImmutableDictionary<string, object?>.Empty).Should().Be("a0");
    }

    [Test]
    public void SerializeRecordIds()
    {
        var recordIds = new List<RecordId> { RecordId.From("user", 1), RecordId.From("user", 2) };

        var result = SerializeAsObjectHexa(recordIds);

        // [8(["user", 1]), 8(["user", 2])]
        result.Should().Be("82c882647573657201c882647573657202");
        SerializeAsObjectHexa(recordIds.ToArray()).Should().Be(result);
        SerializeAsObjectHexa(recordIds.Select(x => x)).Should().Be(result);
    }

    [Test]
    public void SerializeObjectArray()
    {
        object?[] value = ["a", 1, null];

        var result = SerializeAsObjectHexa(value);

        // ["a", 1, null]
        result.Should().Be("83616101f6");
    }

    [Test]
    public async Task DeserializeMapIntoPlainObject()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<object>("a0");

        result.Should().NotBeNull().And.BeOfType<object>();
    }
}
