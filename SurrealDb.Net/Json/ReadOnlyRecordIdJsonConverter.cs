using System.Text.Json;
using System.Text.Json.Serialization;
using SurrealDb.Net.Json.Internals;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Json;

public class ReadOnlyRecordIdJsonConverter : JsonConverter<RecordId>
{
    public override RecordId? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }

    public override void Write(Utf8JsonWriter writer, RecordId value, JsonSerializerOptions options)
    {
        RecordIdJsonConverter.WriteRecordId(writer, value, options);
    }
}
