using System.Text.Json.Serialization;
using SurrealDb.Net.Json.Internals;

namespace SurrealDb.Net.Json;

public sealed class RecordIdJsonConverterAttribute : JsonConverterAttribute
{
    public string Table { get; private set; }

    public RecordIdJsonConverterAttribute(string table)
    {
        Table = table;
    }

    public override JsonConverter? CreateConverter(Type typeToConvert)
    {
        return new RecordIdJsonFactoryConverter(Table);
    }
}
