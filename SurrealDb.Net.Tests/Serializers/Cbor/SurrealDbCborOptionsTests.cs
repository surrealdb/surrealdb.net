using Dahomey.Cbor;
using SurrealDb.Net.Internals.Cbor;

namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class SurrealDbCborOptionsTests
{
    [Test]
    public void DefaultShouldBeCreatedOnce()
    {
        SurrealDbCborOptions.Default.Value.Should().BeSameAs(SurrealDbCborOptions.Default.Value);
    }

    [Test]
    public void ShouldReturnDefaultWhenNotConfigured()
    {
        SurrealDbCborOptions
            .GetCborSerializerOptions(null)
            .Should()
            .BeSameAs(SurrealDbCborOptions.Default.Value);
    }

    [Test]
    public void ShouldNotConfigureDefault()
    {
        CborOptions? configured = null;

        var result = SurrealDbCborOptions.GetCborSerializerOptions(options => configured = options);

        result.Should().BeSameAs(configured);
        result.Should().NotBeSameAs(SurrealDbCborOptions.Default.Value);
    }
}
