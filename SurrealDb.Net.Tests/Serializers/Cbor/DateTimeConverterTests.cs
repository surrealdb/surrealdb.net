using System.Buffers;
using Dahomey.Cbor;
using FsCheck.Xunit;
using SurrealDb.Net.Internals.Cbor;
using Xunit.Abstractions;

namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class DateTimeConverterTests : BaseCborConverterTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public DateTimeConverterTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task Serialize()
    {
        string result = await SerializeCborBinaryAsHexaAsync(
            DateTime.Parse("2024-03-24T13:30:26.1623225Z").ToUniversalTime()
        );

        result.Should().Be("cc821a66002af21a09acd844");
    }

    [Fact]
    public async Task Deserialize()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<DateTime>("cc821a66002af21a09acd844");

        var expected = DateTime.Parse("2024-03-24T13:30:26.1623225Z").ToUniversalTime();

        result.Should().Be(expected);
    }

    [Fact]
    public void ShouldSerializeAndDeserializeDateTime()
    {
        var expected = DateTime.Parse("1913-06-03T14:38:30.1640000Z").ToUniversalTime();

        var serialized = SerializeToCborBinary(expected);
        var deserialized = DeserializeCborBinary<DateTime>(serialized);

        deserialized.Should().Be(expected);
    }

    [Property(DisplayName = "When given a datetime it should serialize and deserialize correctly")]
    public bool DateTimeSerialization(DateTime expected)
    {
        var utc = expected.ToUniversalTime();

        var serialized = SerializeToCborBinary(utc);
        var deserialized = DeserializeCborBinary<DateTime>(serialized);

        return deserialized.Equals(utc);
    }
}
