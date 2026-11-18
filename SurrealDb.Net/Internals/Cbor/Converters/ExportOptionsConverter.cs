using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Cbor.Converters;

/// <summary>
/// Writes the <see cref="ExportOptions"/> sent to the export endpoint without the
/// reflection-based object converter. An option that is not set is omitted.
/// A type derived from <see cref="ExportOptions"/> is written by its own converter.
/// </summary>
internal sealed class ExportOptionsConverter : CborConverterBase<ExportOptions>
{
    private readonly CborConverterRegistry _converters;

    public ExportOptionsConverter(CborOptions options)
    {
        _converters = options.Registry.ConverterRegistry;
    }

    public override ExportOptions Read(ref CborReader reader)
    {
        if (reader.GetCurrentDataItemType() == CborDataItemType.Null)
        {
            reader.ReadNull();
            return null!;
        }

        reader.ReadBeginMap();

        int remainingItemCount = reader.ReadSize();
        var value = new ExportOptions();

        while (reader.MoveNextMapItem(ref remainingItemCount))
        {
            var key = reader.ReadRawString();

            if (key.SequenceEqual("users"u8))
                value.Users = ReadOption(ref reader);
            else if (key.SequenceEqual("accesses"u8))
                value.Accesses = ReadOption(ref reader);
            else if (key.SequenceEqual("params"u8))
                value.Params = ReadOption(ref reader);
            else if (key.SequenceEqual("functions"u8))
                value.Functions = ReadOption(ref reader);
            else if (key.SequenceEqual("versions"u8))
                value.Versions = ReadOption(ref reader);
            else if (key.SequenceEqual("tables"u8))
                value.Tables = ReadOption(ref reader);
            else if (key.SequenceEqual("records"u8))
                value.Records = ReadOption(ref reader);
            else
                reader.ReadDataItem();
        }

        return value;
    }

    private static bool? ReadOption(ref CborReader reader)
    {
        return reader.ReadNull() ? null : reader.ReadBoolean();
    }

    public override void Write(ref CborWriter writer, ExportOptions value)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        var type = value.GetType();
        if (type != typeof(ExportOptions))
        {
            _converters.Lookup(type).Write(ref writer, value);
            return;
        }

        int size =
            CountOption(value.Users)
            + CountOption(value.Accesses)
            + CountOption(value.Params)
            + CountOption(value.Functions)
            + CountOption(value.Versions)
            + CountOption(value.Tables)
            + CountOption(value.Records);

        writer.WriteBeginMap(size);

        WriteOption(ref writer, "users", value.Users);
        WriteOption(ref writer, "accesses", value.Accesses);
        WriteOption(ref writer, "params", value.Params);
        WriteOption(ref writer, "functions", value.Functions);
        WriteOption(ref writer, "versions", value.Versions);
        WriteOption(ref writer, "tables", value.Tables);
        WriteOption(ref writer, "records", value.Records);
    }

    private static int CountOption(bool? option)
    {
        return option.HasValue ? 1 : 0;
    }

    private static void WriteOption(ref CborWriter writer, string name, bool? option)
    {
        if (!option.HasValue)
        {
            return;
        }

        writer.WriteString(name);
        writer.WriteBoolean(option.Value);
    }
}
