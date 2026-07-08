using SurrealDb.Net.Internals.Cbor;
using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class SurrealDbResultConverterTests : BaseCborConverterTests
{
    [Test]
    public void CannotSerialize()
    {
        ISurrealDbResult value = new SurrealDbOkResult(
            TimeSpan.Zero,
            "OK",
            SurrealDbResponseType.Other,
            new([0xc6, 0xf6]),
            SurrealDbCborOptions.Default.Value
        );

        Func<Task> act = () => SerializeCborBinaryAsHexaAsync(value);

        act.Should().ThrowAsync<NotImplementedException>();
    }

    [Test]
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

    // See https://github.com/surrealdb/surrealdb.net/issues/227
    // and https://github.com/surrealdb/surrealdb.net/issues/236
    [Test]
    public async Task DeserializeResultWithEmptyTypeShouldNotThrow()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<ISurrealDbResult>(
            "a466726573756c74c6f666737461747573624f4b6474696d6566363630c2b573647479706560"
        );

        result.Should().BeOfType<SurrealDbOkResult>();

        var okResult = (SurrealDbOkResult)result;
        okResult.Status.Should().Be("OK");
        okResult.IsOk.Should().BeTrue();
        okResult.Type.Should().Be(SurrealDbResponseType.Other);

        okResult.GetValue<string>().Should().BeNull();
    }
}
