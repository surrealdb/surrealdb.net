namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class DateTimeOffsetConverterTests : BaseCborConverterTests
{
    [Test]
    public async Task SerializeShouldNormalizeToUtc()
    {
        // Same instant as 2024-03-24T13:30:26.1623225Z (from DateTimeConverterTests tests but with +02:00 offset)
        var value = DateTimeOffset.Parse("2024-03-24T15:30:26.1623225+02:00");

        string result = await SerializeCborBinaryAsHexaAsync(value);

#if NET7_0_OR_GREATER
        result.Should().Be("cc821a66002af21a09acd844");
#else
        result.Should().Be("cc821a66002af21a09acd843");
#endif
    }

    [Test]
    public async Task DeserializeShouldReturnUtcOffset()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<DateTimeOffset>(
            "cc821a66002af21a09acd844"
        );

        var expectedUtc = DateTimeOffset.Parse("2024-03-24T13:30:26.1623225+00:00");
        var expectedLocal = DateTimeOffset.Parse("2024-03-24T15:30:26.1623225+02:00");

        result.Should().Be(expectedUtc);
        result.Should().Be(expectedLocal);
    }
}
