using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;

namespace SurrealDb.Net.Internals.Cbor.Converters;

/// <summary>
/// Reads an arbitrary CBOR map into Dictionary[string, object?], or null if the value is null.
/// Nested maps become Dictionary, arrays become List[object?], primitives stay as-is.
/// Used for RPC error details so any server shape is accepted without throwing.
/// </summary>
internal sealed class CborMapToDictionaryConverter : CborConverterBase<Dictionary<string, object?>?>
{
    /// <summary>Reads a CBOR value (null or map) into Dictionary? without registering a converter.</summary>
    internal static Dictionary<string, object?>? ReadNullableMap(ref CborReader reader)
    {
        if (reader.GetCurrentDataItemType() == CborDataItemType.Null)
        {
            reader.ReadNull();
            return null;
        }

        if (reader.GetCurrentDataItemType() != CborDataItemType.Map)
        {
            reader.SkipDataItem();
            return null;
        }

        reader.ReadBeginMap();
        int remainingItemCount = reader.ReadSize();
        var dict = new Dictionary<string, object?>();

        while (reader.MoveNextMapItem(ref remainingItemCount))
        {
            string? key = reader.ReadString();
            if (key is not null)
            {
                object? value = ReadCborValueIntoObject(ref reader);
                dict[key] = value;
            }
            else
            {
                reader.SkipDataItem();
            }
        }

        return dict;
    }

    public override Dictionary<string, object?>? Read(ref CborReader reader)
    {
        return ReadNullableMap(ref reader);
    }

    public override void Write(ref CborWriter writer, Dictionary<string, object?>? value)
    {
        throw new NotSupportedException("Cannot write Dictionary<string, object?> back to CBOR.");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static object? ReadCborValueIntoObject(ref CborReader reader)
    {
        var itemType = reader.GetCurrentDataItemType();

        return itemType switch
        {
            CborDataItemType.Null => reader.ReadNull(),
            CborDataItemType.Boolean => reader.ReadBoolean(),
            CborDataItemType.String => reader.ReadString(),
            CborDataItemType.Signed => reader.ReadInt64(),
            CborDataItemType.Unsigned => reader.ReadUInt64(),
            CborDataItemType.Single => reader.ReadSingle(),
            CborDataItemType.Double => reader.ReadDouble(),
            CborDataItemType.Map => ReadMapIntoDictionary(ref reader),
            CborDataItemType.Array => ReadArrayIntoList(ref reader),
            _ => SkipAndReturnNull(ref reader),
        };
    }

    private static Dictionary<string, object?> ReadMapIntoDictionary(ref CborReader reader)
    {
        reader.ReadBeginMap();
        int remainingItemCount = reader.ReadSize();
        var dict = new Dictionary<string, object?>();

        while (reader.MoveNextMapItem(ref remainingItemCount))
        {
            string? key = reader.ReadString();
            if (key is not null)
            {
                object? value = ReadCborValueIntoObject(ref reader);
                dict[key] = value;
            }
            else
            {
                reader.SkipDataItem();
            }
        }

        return dict;
    }

    private static List<object?> ReadArrayIntoList(ref CborReader reader)
    {
        reader.ReadBeginArray();
        int size = reader.ReadSize();
        var list = new List<object?>(size);

        for (int i = 0; i < size; i++)
        {
            list.Add(ReadCborValueIntoObject(ref reader));
        }

        return list;
    }

    private static object? SkipAndReturnNull(ref CborReader reader)
    {
        reader.SkipDataItem();
        return null;
    }
}
