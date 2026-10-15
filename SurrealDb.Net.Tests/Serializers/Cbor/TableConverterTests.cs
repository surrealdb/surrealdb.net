namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class TableConverterTests : BaseCborConverterTests
{
    [Test]
    public async Task Serialize()
    {
        string result = await SerializeCborBinaryAsHexaAsync(new Table("user"));

        result.Should().Be("c76475736572");
    }

    [Test]
    public async Task Deserialize()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<Table>("c76475736572");

        result.Should().Be(new Table("user"));
    }

    [Test]
    public async Task DeserializeAsString()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<string>("c76475736572");

        result.Should().Be("user");
    }

    [Test]
    public void ShouldRequireName()
    {
        var act = () => new Table(string.Empty);

        act.Should().Throw<ArgumentException>();
    }
}
