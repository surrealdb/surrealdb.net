using System.Text.Json;
using System.Text.Json.Serialization;
using SurrealDb.Net.Models;
#if NET7_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace SurrealDb.Net.Json.Internals;

internal sealed class RecordIdJsonConverter : JsonConverter<RecordId>
{
    private readonly string _table;

    public RecordIdJsonConverter(string table)
    {
        _table = table;
    }

    public override RecordId? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (
            typeToConvert.Namespace == "SurrealDb.Net.Models"
            && typeToConvert.Name == "RecordIdOf`1"
        )
        {
            var idType = typeToConvert.GenericTypeArguments[0];
            if (idType == typeof(int))
            {
                var id = reader.GetInt32();
                return new RecordIdOf<int>(_table, id);
            }

            // TODO : Generic reader for any id type
        }

        {
            var id = reader.GetString();
            if (id is not null)
            {
                return new RecordIdOfString(_table, id);
            }
        }

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
        WriteRecordId(writer, value, options);
    }

#if NET7_0_OR_GREATER
    [RequiresDynamicCode(
        "Requires reflection for JSON serialization of potential objects/arrays record id"
    )]
    [RequiresUnreferencedCode(
        "Requires reflection for JSON serialization of potential objects/arrays record id"
    )]
#endif
    internal static void WriteRecordId(
        Utf8JsonWriter writer,
        RecordId value,
        JsonSerializerOptions options
    )
    {
        if (value is RecordIdOfString recordIdOfString)
        {
            writer.WriteStringValue(recordIdOfString.Id);
            return;
        }

        if (value is RecordIdOf<int> recordIdOfInt)
        {
            writer.WriteNumberValue(recordIdOfInt.Id);
            return;
        }

        var type = value.GetType();
        if (type.Namespace == "SurrealDb.Net.Models" && type.Name == "RecordIdOf`1")
        {
            var idType = type.GenericTypeArguments[0];
            writer.WriteRawValue(
                JsonSerializer.Serialize(value.DeserializeId(idType), idType, options)
            );
            return;
        }

        throw new NotImplementedException();
    }
}
