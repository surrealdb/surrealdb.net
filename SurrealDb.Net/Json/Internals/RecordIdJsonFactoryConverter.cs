using System.Text.Json;
using System.Text.Json.Serialization;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Json.Internals;

internal sealed class RecordIdJsonFactoryConverter : JsonConverterFactory
{
    private readonly string _table;

    public RecordIdJsonFactoryConverter(string table)
    {
        _table = table;
    }

    public override bool CanConvert(Type typeToConvert)
    {
        if (typeToConvert == typeof(RecordId))
            return true;

        if (typeToConvert == typeof(RecordIdOfString))
            return true;

        if (
            string.Equals(typeToConvert.Namespace, "SurrealDb.Net.Models", StringComparison.Ordinal)
            && string.Equals(typeToConvert.Name, "RecordIdOf`1", StringComparison.Ordinal)
        )
            return true;

        return false;
    }

    public override JsonConverter? CreateConverter(
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        return new RecordIdJsonConverter(_table);
    }
}
