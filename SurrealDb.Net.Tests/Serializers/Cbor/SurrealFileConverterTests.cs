namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class SurrealFileConverterTests : BaseCborConverterTests
{
    [Test]
    public async Task Serialize()
    {
        var value = new SurrealFile("bucket", "/some/key/to/a/file.txt");

        string result = await SerializeCborBinaryAsHexaAsync(value);
        Console.WriteLine(result);

        result.Should().Be("d83782666275636b6574772f736f6d652f6b65792f746f2f612f66696c652e747874");
    }

    [Test]
    public async Task Deserialize()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<SurrealFile>(
            "d83782666275636b6574772f736f6d652f6b65792f746f2f612f66696c652e747874"
        );

        result.Should().Be(new SurrealFile("bucket", "/some/key/to/a/file.txt"));
    }
}
