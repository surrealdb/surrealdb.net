using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Internals.Extensions;
using SystemTextJsonPatch;
using SystemTextJsonPatch.Operations;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal class JsonPatchDocumentConverter : CborConverterBase<JsonPatchDocument>
{
    private readonly CborOptions _options;

    public JsonPatchDocumentConverter(CborOptions options)
    {
        _options = options;
    }

    public override JsonPatchDocument Read(ref CborReader reader)
    {
        if (reader.ReadNull())
        {
            return default!;
        }

        reader.ReadBeginArray();

        int size = reader.ReadSize();

        var operations = new List<Operation>(size);

        for (int i = 0; i < size; i++)
        {
            reader.ReadBeginMap();

            int remainingItemCount = reader.ReadSize();

            string? op = null;
            string? path = null;
            string? from = null;
            object? value = null;

            while (reader.MoveNextMapItem(ref remainingItemCount))
            {
                ReadOnlySpan<byte> key = reader.ReadRawString();

                if (key.SequenceEqual("op"u8))
                {
                    op = reader.ReadString();
                    continue;
                }

                if (key.SequenceEqual("path"u8))
                {
                    path = reader.ReadString();
                    continue;
                }

                if (key.SequenceEqual("from"u8))
                {
                    from = reader.ReadString();
                    continue;
                }

                if (key.SequenceEqual("value"u8))
                {
                    // TODO : Handle Semantic Tag
                    var itemType = reader.GetCurrentDataItemType();

                    value = itemType switch
                    {
                        CborDataItemType.Null => reader.ReadNull(),
                        CborDataItemType.Boolean => reader.ReadBoolean(),
                        CborDataItemType.String => reader.ReadString(),
                        CborDataItemType.Signed or CborDataItemType.Unsigned =>
                            reader.ReadDecimal(),
                        _ => reader.ReadDataItemAsMemory(),
                    };
                    continue;
                }

                reader.SkipDataItem();
            }

            if (op is null)
            {
                throw new CborException("Property 'op' is required.");
            }

            var operation = new Operation(op, path, from, value);
            operations.Add(operation);
        }

        var document = new JsonPatchDocument();
        document.Operations.AddRange(operations);

        return document;
    }

    public override void Write(ref CborWriter writer, JsonPatchDocument value)
    {
        if (value is null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteBeginArray(value.Operations.Count);

        foreach (var operation in value.Operations)
        {
            const int jsonPatchPropertiesCount = 4;

            writer.WriteBeginMap(jsonPatchPropertiesCount);

            writer.WriteString("op"u8);
            writer.WriteString(operation.Op!);

            writer.WriteString("path"u8);
            if (operation.Path is null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteString(operation.Path);
            }

            writer.WriteString("from"u8);
            if (operation.From is null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteString(operation.From);
            }

            writer.WriteString("value"u8);
            if (operation.Value is null)
            {
                writer.WriteNull();
            }
            else
            {
                var converter = _options.Registry.ConverterRegistry.Lookup(
                    operation.Value.GetType()
                );
                converter.Write(ref writer, operation.Value);
            }

            writer.WriteEndMap(jsonPatchPropertiesCount);
        }

        writer.WriteEndArray(value.Operations.Count);
    }
}
