using System.Text.Json;
using System.Text.Json.Serialization;
using SurrealDb.Net.Internals.Cbor;
using SurrealDb.Net.Internals.Cbor.Converters;
using SurrealDb.Net.Models;

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
            typeToConvert.Namespace == ConverterTypeConstants.ModelsNamespace
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

    public override void Write(Utf8JsonWriter writer, RecordId value, JsonSerializerOptions options)
    {
        WriteRecordId(writer, value, options);
    }

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
        if (type.Namespace == ConverterTypeConstants.ModelsNamespace && type.Name == "RecordIdOf`1")
        {
            var idType = type.GenericTypeArguments[0];
#if NET8_0_OR_GREATER
            // Resolves the id type through the options (source-generated or reflection-based),
            // so that the write does not require reflection under Native AOT.
            JsonSerializer.Serialize(
                writer,
                value.DeserializeId(idType),
                options.GetTypeInfo(idType)
            );
#else
            writer.WriteRawValue(
                JsonSerializer.Serialize(value.DeserializeId(idType), idType, options)
            );
#endif
            return;
        }

        throw new NotImplementedException();
    }
}
