using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Internals.Extensions;
#if NET10_0_OR_GREATER
using Microsoft.AspNetCore.JsonPatch.SystemTextJson;
using Microsoft.AspNetCore.JsonPatch.SystemTextJson.Operations;
#else
using SystemTextJsonPatch;
using SystemTextJsonPatch.Operations;
#endif

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
                    var valueDataItem = reader.ReadDataItem();

                    // TODO : MoveToPreviousDataItem method?
                    var valueReader = new CborReader(valueDataItem);
                    var itemType = valueReader.GetCurrentDataItemType();

                    value = itemType switch
                    {
                        CborDataItemType.Null => valueReader.ReadNull(),
                        CborDataItemType.Boolean => valueReader.ReadBoolean(),
                        CborDataItemType.String => valueReader.ReadString(),
                        CborDataItemType.Signed or CborDataItemType.Unsigned =>
                            valueReader.ReadDecimal(),
                        _ => valueDataItem.ToMemory(),
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

    public override void Write(ref CborWriter writer, JsonPatchDocument document)
    {
        if (document is null)
        {
            writer.WriteNull();
            return;
        }

        writer.WriteBeginArray(document.Operations.Count);

        foreach (var operation in document.Operations)
        {
#if NET10_0_OR_GREATER
            var op = operation.op;
            var path = operation.path;
            var from = operation.from;
            var value = operation.value;
#else
            var op = operation.Op;
            var path = operation.Path;
            var from = operation.From;
            var value = operation.Value;
#endif
            const int jsonPatchPropertiesCount = 4;

            writer.WriteBeginMap(jsonPatchPropertiesCount);

            writer.WriteString("op"u8);
            writer.WriteString(op!);

            writer.WriteString("path"u8);
            if (path is null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteString(path);
            }

            writer.WriteString("from"u8);
            if (from is null)
            {
                writer.WriteNull();
            }
            else
            {
                writer.WriteString(from);
            }

            writer.WriteString("value"u8);
            if (value is null)
            {
                writer.WriteNull();
            }
            else
            {
                var converter = _options.Registry.ConverterRegistry.Lookup(value.GetType());
                converter.Write(ref writer, value);
            }

            writer.WriteEndMap(jsonPatchPropertiesCount);
        }

        writer.WriteEndArray(document.Operations.Count);
    }
}
