namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class RecordIdConverterTests : BaseCborConverterTests
{
    [Test]
    public async Task Serialize()
    {
        var value = RecordId.From("post", "first");

        var result = await SerializeCborBinaryAsHexaAsync(value);

        result.Should().Be("c88264706f7374656669727374");
    }

    [Test]
    public async Task ShouldFailToDeserializeFromStringRecordId()
    {
        var func = async () =>
            await DeserializeCborBinaryAsHexaAsync<StringRecordId>("c86a706f73743a6669727374");

        await func.Should()
            .ThrowAsync<NotSupportedException>()
            .WithMessage("Cannot read StringRecordId from cbor...");
    }

    [Test]
    public async Task DeserializeFromArrayWithStringId()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<RecordId>("c88264706f7374656669727374");

        var expected = new RecordIdOfString("post", "first");

        result.Should().Be(expected);
    }

    [Test]
    public async Task DeserializeFromArrayWithIntegerId()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<RecordId>("c88264706f7374193039");

        var expected = new RecordIdOf<int>("post", 12345);

        result.Should().Be(expected);
    }

    [Test]
    public async Task DeserializeFromNull()
    {
        // CBOR: f6 = null
        var result = await DeserializeCborBinaryAsHexaAsync<RecordId>("f6");

        result.Should().BeNull();
    }

    /// <summary>
    /// Regression test for: RecordIdConverter.Read used a switch expression where the Null
    /// arm returned <c>default!</c> without consuming the F6 (null) byte, leaving the reader
    /// mispositioned. If the bug is present, deserializing the element following a null RecordId
    /// in a sequence throws or returns a wrong value.
    /// </summary>
    [Test]
    public async Task DeserializeFromNullInsideSequence()
    {
        // CBOR: array(2) [ null, uint(42) ]
        // 82       = array of length 2
        // f6       = null  →  null RecordId
        // 18 2a    = uint(42)
        var result = await DeserializeCborBinaryAsHexaAsync<(RecordId?, int)>("82f6182a");

        result.Item1.Should().BeNull();
        result.Item2.Should().Be(42);
    }

    /// <summary>
    /// Regression test for the exact production scenario: SurrealDB NONE (Tag(6)+null = C6 F6)
    /// appears as the first element of a tuple array ID. GetCurrentDataItemType() consumes the
    /// semantic tag and peeks at null, but without an explicit ReadNull() the F6 byte remains
    /// unconsumed and is read by the next tuple element's converter, silently losing the value.
    /// </summary>
    [Test]
    public async Task DeserializeFromTaggedNullInsideSequence()
    {
        // CBOR: array(2) [ tag(6) null, "hello" ]
        // 82          = array of length 2
        // c6 f6       = tag(6) + null  →  NONE, read as null RecordId
        // 65 68656c6c6f = text(5) "hello"
        var result = await DeserializeCborBinaryAsHexaAsync<(RecordId?, string)>(
            "82c6f66568656c6c6f"
        );

        result.Item1.Should().BeNull();
        result.Item2.Should().Be("hello");
    }

    /// <summary>
    /// Confirms the non-null path works inside a tuple: a tagged RecordId array followed by
    /// another element should deserialize both correctly.
    /// </summary>
    [Test]
    public async Task DeserializeFromRecordIdInsideSequence()
    {
        // CBOR: array(2) [ tag(8) array(2) ["post", "first"], uint(42) ]
        // 82                               = array of length 2
        // c8 82 64 706f7374 65 6669727374  = tag(8) + array(2) [ "post", "first" ]
        // 18 2a                            = uint(42)
        var result = await DeserializeCborBinaryAsHexaAsync<(RecordId, int)>(
            "82c88264706f7374656669727374182a"
        );

        result.Item1.Should().Be(new RecordIdOfString("post", "first"));
        result.Item2.Should().Be(42);
    }
}
