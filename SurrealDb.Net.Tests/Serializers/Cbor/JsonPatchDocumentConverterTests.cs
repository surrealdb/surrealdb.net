using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson.Operations;

namespace SurrealDb.Net.Tests.Serializers.Cbor;

[JsonSerializable(typeof(Post))]
public partial class JsonPatchDocumentConverterTestsJsonContext : JsonSerializerContext;

public class JsonPatchDocumentConverterTests : BaseCborConverterTests
{
    [Test]
    public async Task Serialize()
    {
        var value = new JsonPatchDocument
        {
            SerializerOptions = new JsonSerializerOptions
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

    [Test]
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

            operation.op.Should().Be(expectedOperation.op);
            operation.path.Should().Be(expectedOperation.path);
            operation.from.Should().Be(expectedOperation.from);

            var operationValue =
                operation.value is ReadOnlyMemory<byte>
                    ? (ReadOnlyMemory<byte>)operation.value
                    : default;
            var expectedValue =
                expectedOperation.value is ReadOnlyMemory<byte>
                    ? (ReadOnlyMemory<byte>)expectedOperation.value
                    : default;
            operationValue.ToArray().Should().BeEquivalentTo(expectedValue.ToArray());
        }
    }

    [Test]
    public async Task SerializeWithGenerics()
    {
        var value = new JsonPatchDocument<Post>
        {
            SerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                TypeInfoResolver = JsonPatchDocumentConverterTestsJsonContext.Default,
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

    [Test]
    public async Task DeserializeWithGenerics()
    {
        var result = await DeserializeCborBinaryAsHexaAsync<JsonPatchDocument<Post>>(
            "81a4626f70677265706c6163656470617468682f636f6e74656e746466726f6df66576616c75656b5b456469745d204f6f7073"
        );

        var expected = new JsonPatchDocument<Post>
        {
            SerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                TypeInfoResolver = JsonPatchDocumentConverterTestsJsonContext.Default,
            },
        };
        expected.Replace(x => x.Content, "[Edit] Oops");

        result.Operations.Should().BeEquivalentTo(expected.Operations);
    }
}
