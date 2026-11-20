using SurrealDb.Net.Internals.Cbor;
using SurrealDb.Net.Internals.Cbor.Converters;

namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class ExportOptionsConverterTests : BaseCborConverterTests
{
    [Test]
    public void ShouldBeRegistered()
    {
        SurrealDbCborOptions
            .Default.Value.Registry.ConverterRegistry.Lookup<ExportOptions>()
            .Should()
            .BeOfType<ExportOptionsConverter>();
    }

    [Test]
    public void SerializeDefaultOptions()
    {
        var value = new ExportOptions();

        var result = SerializeAsHexa(value);

        result.Should().Be("a0");
        result.Should().Be(SerializeWithReflectionAsHexa(value));
    }

    [Test]
    public void SerializeSomeOptions()
    {
        var value = new ExportOptions
        {
            Users = false,
            Tables = true,
            Records = false,
        };

        var result = SerializeAsHexa(value);

        // {"users": false, "tables": true, "records": false}
        result.Should().Be("a3657573657273f4667461626c6573f5677265636f726473f4");
        result.Should().Be(SerializeWithReflectionAsHexa(value));
    }

    [Test]
    public void SerializeAllOptions()
    {
        var value = new ExportOptions
        {
            Users = true,
            Accesses = true,
            Params = true,
            Functions = true,
            Versions = true,
            Tables = true,
            Records = true,
        };

        var result = SerializeAsHexa(value);

        result.Should().Be(SerializeWithReflectionAsHexa(value));
    }

    [Test]
    public async Task Deserialize()
    {
        var value = new ExportOptions
        {
            Users = true,
            Functions = false,
            Records = true,
        };

        var result = await DeserializeCborBinaryAsHexaAsync<ExportOptions>(SerializeAsHexa(value));

        result.Should().BeEquivalentTo(value);
    }
}
