using System.Text.Json;
using System.Text.Json.Serialization;
using SurrealDb.Net.Json.Internals;
using SurrealDb.Net.Models;
#if NET7_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

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

#if NET7_0_OR_GREATER
    [RequiresDynamicCode(
        "Requires reflection for JSON serialization of potential objects/arrays record id"
    )]
    [RequiresUnreferencedCode(
        "Requires reflection for JSON serialization of potential objects/arrays record id"
    )]
#endif
#pragma warning disable IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.
#pragma warning disable IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
    public override void Write(Utf8JsonWriter writer, RecordId value, JsonSerializerOptions options)
#pragma warning restore IL3051 // 'RequiresDynamicCodeAttribute' annotations must match across all interface implementations or overrides.
#pragma warning restore IL2046 // 'RequiresUnreferencedCodeAttribute' annotations must match across all interface implementations or overrides.
    {
        RecordIdJsonConverter.WriteRecordId(writer, value, options);
    }
}
