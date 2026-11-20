namespace SurrealDb.Net.Tests.Serializers.Cbor;

/// <summary>
/// Datetimes, UUIDs and durations can also be sent as strings, with the tags 0, 9 and 13.
/// </summary>
public class StringTagsConverterTests : BaseCborConverterTests
{
    private const string DATETIME_TAG_0 =
        "c0781e323032342d30312d30325430333a30343a30352e3132333435363738395a";
    private const string DATETIME_WITH_OFFSET_TAG_0 =
        "c07819323032342d30312d30325430353a30343a30352b30323a3030";
    private const string UUID_TAG_9 =
        "c9782430303031303230332d303430352d303630372d303830392d306130623063306430653066";
    private const string DURATION_TAG_13 = "cd68316833306d313573";

    [Test]
    public async Task ShouldDeserializeDateTimeFromString()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<DateTime>(DATETIME_TAG_0);

        result.Kind.Should().Be(DateTimeKind.Utc);
        // 💡 Nanoseconds are rounded to the nearest tick, as with the binary datetime
        result.Should().Be(new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc).AddTicks(1234568));
    }

    [Test]
    public async Task ShouldDeserializeDateTimeOffsetFromString()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<DateTimeOffset>(
            DATETIME_WITH_OFFSET_TAG_0
        );

        result.Offset.Should().Be(TimeSpan.Zero);
        result.Should().Be(new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero));
    }

#if NET6_0_OR_GREATER
    [Test]
    public async Task ShouldDeserializeDateOnlyFromString()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<DateOnly>(DATETIME_TAG_0);

        result.Should().Be(new DateOnly(2024, 1, 2));
    }

    [Test]
    public async Task ShouldDeserializeTimeOnlyFromString()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<TimeOnly>(DURATION_TAG_13);

        result.Should().Be(new TimeOnly(1, 30, 15));
    }
#endif

    [Test]
    public async Task ShouldDeserializeGuidFromString()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<Guid>(UUID_TAG_9);

        result.Should().Be(Guid.Parse("00010203-0405-0607-0809-0a0b0c0d0e0f"));
    }

    [Test]
    public async Task ShouldDeserializeDurationFromString()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<Duration>(DURATION_TAG_13);

        result.Should().Be(new Duration("1h30m15s"));
    }

    [Test]
    public async Task ShouldDeserializeTimeSpanFromString()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<TimeSpan>(DURATION_TAG_13);

        result.Should().Be(new TimeSpan(1, 30, 15));
    }
}
