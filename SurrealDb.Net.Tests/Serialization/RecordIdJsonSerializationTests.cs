using System.Text.Json;
using System.Text.Json.Serialization;
using SurrealDb.Net.Json;

namespace SurrealDb.Net.Tests.Serialization;

public class RecordIdJsonSerializationTests
{
    [Test]
    public void ShouldWriteIntegerId()
    {
        var record = new JsonPost { Id = RecordId.From("post", 1) };

        JsonSerializer.Serialize(record).Should().Be("{\"Id\":1}");
    }

    [Test]
    public void ShouldWriteStringId()
    {
        var record = new JsonPost { Id = RecordId.From("post", "first") };

        JsonSerializer.Serialize(record).Should().Be("{\"Id\":\"first\"}");
    }

    [Test]
    public void ShouldWriteOtherIdTypes()
    {
        var record = new JsonPost
        {
            Id = RecordId.From("post", new Guid("7a5fd2a1-6a28-4b47-9b1e-35d7e9c6c0b6")),
        };

        JsonSerializer
            .Serialize(record)
            .Should()
            .Be("{\"Id\":\"7a5fd2a1-6a28-4b47-9b1e-35d7e9c6c0b6\"}");
    }

    [Test]
    public void ShouldWriteOtherIdTypesWithSourceGeneratedMetadata()
    {
        var record = new JsonPost
        {
            Id = RecordId.From("post", new Guid("7a5fd2a1-6a28-4b47-9b1e-35d7e9c6c0b6")),
        };

        JsonSerializer
            .Serialize(record, RecordIdJsonSerializerContext.Default.JsonPost)
            .Should()
            .Be("{\"Id\":\"7a5fd2a1-6a28-4b47-9b1e-35d7e9c6c0b6\"}");
    }

    [Test]
    public void ShouldWriteTypedRecordId()
    {
        var record = new JsonTypedPost { Id = new RecordIdOf<long>("post", 9999999999L) };

        JsonSerializer
            .Serialize(record, RecordIdJsonSerializerContext.Default.JsonTypedPost)
            .Should()
            .Be("{\"Id\":9999999999}");
    }
}

public class JsonPost : SurrealDbRecord;

public class JsonTypedPost
{
    [JsonConverter(typeof(ReadOnlyRecordIdJsonConverter<long>))]
    public RecordIdOf<long>? Id { get; set; }
}

[JsonSerializable(typeof(JsonPost))]
[JsonSerializable(typeof(JsonTypedPost))]
[JsonSerializable(typeof(Guid))]
[JsonSerializable(typeof(long))]
internal partial class RecordIdJsonSerializerContext : JsonSerializerContext;
