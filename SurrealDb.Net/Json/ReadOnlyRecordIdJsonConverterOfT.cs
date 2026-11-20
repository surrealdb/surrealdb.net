using System.Text.Json;
using System.Text.Json.Serialization;
using SurrealDb.Net.Models;
#if NET8_0_OR_GREATER
using System.Text.Json.Serialization.Metadata;
#endif

namespace SurrealDb.Net.Json;

public class ReadOnlyRecordIdJsonConverter<T> : JsonConverter<RecordIdOf<T>>
{
    public override RecordIdOf<T>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        throw new NotImplementedException();
    }

    public override void Write(
        Utf8JsonWriter writer,
        RecordIdOf<T> value,
        JsonSerializerOptions options
    )
    {
#if NET8_0_OR_GREATER
        // Resolves the id type through the options (source-generated or reflection-based),
        // so that the write does not require reflection under Native AOT.
        var typeInfo = (JsonTypeInfo<T>)options.GetTypeInfo(typeof(T));
        JsonSerializer.Serialize(writer, value.DeserializeId<T>(), typeInfo);
#else
        JsonSerializer.Serialize(writer, value.DeserializeId<T>(), options);
#endif
    }
}
