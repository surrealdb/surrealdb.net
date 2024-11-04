using System.Text.Json;
using SystemTextJsonPatch;
using SystemTextJsonPatch.Operations;

namespace SurrealDb.Net.Tests.Serializers.Cbor;

public class JsonPatchDocumentConverterTests : BaseCborConverterTests
{
    [Fact]
    public async Task Serialize()
    {
        var value = new JsonPatchDocument
        {
            Options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            },
        };
        value.Operations.Add(new Operation("add", "/value", null, new { X = 1, Y = 2 }));

        string result = await SerializeCborBinaryAsHexaAsync(value);

        result
            .Should()
            .Be("81a4626f70636164646470617468662f76616c75656466726f6df66576616c7565a2615801615902");
    }

    [Fact]
    public async Task Deserialize()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<JsonPatchDocument>(
            "81a4626f70636164646470617468662f76616c75656466726f6df66576616c7565a2615801615902"
        );

        var expected = new JsonPatchDocument();
        expected.Operations.Add(
            new Operation(
                "add",
                "/value",
                null,
                new ReadOnlyMemory<byte>(StringToByteArray("a2615801615902"))
            )
        );

        result.Operations.Should().HaveCount(expected.Operations.Count);

        for (int index = 0; index < result.Operations.Count; index++)
        {
            var operation = result.Operations[index];
            var expectedOperation = expected.Operations[index];

            operation.Op.Should().Be(expectedOperation.Op);
            operation.Path.Should().Be(expectedOperation.Path);
            operation.From.Should().Be(expectedOperation.From);

            var operationValue =
                operation.Value is ReadOnlyMemory<byte>
                    ? (ReadOnlyMemory<byte>)operation.Value
                    : default;
            var expectedValue =
                expectedOperation.Value is ReadOnlyMemory<byte>
                    ? (ReadOnlyMemory<byte>)expectedOperation.Value
                    : default;
            operationValue.ToArray().Should().BeEquivalentTo(expectedValue.ToArray());
        }
    }

    [Fact]
    public async Task SerializeWithGenerics()
    {
        var value = new JsonPatchDocument<Post>
        {
            Options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            },
        };
        value.Replace(x => x.Content, "[Edit] Oops");

        string result = await SerializeCborBinaryAsHexaAsync(value);

        result
            .Should()
            .Be(
                "81a4626f70677265706c6163656470617468682f636f6e74656e746466726f6df66576616c75656b5b456469745d204f6f7073"
            );
    }

    [Fact]
    public async Task DeserializeWithGenerics()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<JsonPatchDocument<Post>>(
            "81a4626f70677265706c6163656470617468682f636f6e74656e746466726f6df66576616c75656b5b456469745d204f6f7073"
        );

        var expected = new JsonPatchDocument<Post>
        {
            Options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            },
        };
        expected.Replace(x => x.Content, "[Edit] Oops");

        result.Operations.Should().BeEquivalentTo(expected.Operations);
    }
}
