using SurrealDb.Net.Internals.Cbor;
using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class SurrealDbResultConverterTests : BaseCborConverterTests
{
    [Fact]
    public void CannotSerialize()
    {
        ISurrealDbResult value = new SurrealDbOkResult(
            TimeSpan.Zero,
            "OK",
            new([0xc6, 0xf6]),
            SurrealDbCborOptions.Default
        );

        Func<Task> act = () => SerializeCborBinaryAsHexaAsync(value);

        act.Should().ThrowAsync<NotImplementedException>();
    }

    [Fact]
    public async Task DeserializeResultWithNoResult()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<ISurrealDbResult>(
            "a366726573756c74c6f666737461747573624f4b6474696d6566363630c2b573"
        );

        result.Should().BeOfType<SurrealDbOkResult>();

        var okResult = (SurrealDbOkResult)result;
        okResult.Status.Should().Be("OK");
        okResult.IsOk.Should().BeTrue();

        okResult.GetValue<string>().Should().BeNull();
    }
}
