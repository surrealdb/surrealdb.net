namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class NoneConverterTests : BaseCborConverterTests
{
    [Test]
    public async Task Serialize()
    {
        var value = new None();

        string result = await SerializeCborBinaryAsHexaAsync(value);

        result.Should().Be("c6f6");
    }

    [Test]
    public async Task Deserialize()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<None>("c6f6");

        result.Should().Be(new None());
    }

    /// <summary>
    /// Regression test for: NoneConverter.Read did not consume the F6 (null) byte after
    /// reading the C6 (tag 6) semantic tag, leaving the reader mispositioned.
    /// If the bug is present, deserializing the element following None in a sequence throws
    /// or returns a wrong value because the reader is still pointing at the unconsumed F6.
    /// </summary>
    [Test]
    public async Task DeserializeInsideSequence()
    {
        // CBOR: array(2) [ tag(6) null, uint(42) ]
        // 82       = array of length 2
        // c6 f6    = tag(6) + null  →  None
        // 18 2a    = uint(42)
        var result = await DeserializeCborBinaryAsHexaAsync<(None, int)>("82c6f6182a");

        result.Item1.Should().Be(new None());
        result.Item2.Should().Be(42);
    }
}
