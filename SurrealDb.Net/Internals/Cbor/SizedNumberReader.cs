using System.Globalization;
using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Cbor;

internal static class SizedNumberReader
{
    public static bool IsSizedTag(ulong tag)
    {
        return tag is >= CborTagConstants.TAG_I8 and <= CborTagConstants.TAG_F32;
    }

    public static ulong GetTag(SizedNumberKind kind)
    {
        return CborTagConstants.TAG_I8 + (ulong)kind;
    }

    /// <summary>
    /// Reads the item under a sized-number tag, checking it the way the server does: the integer
    /// tags need an integer within the kind, and the float tags a float the kind holds exactly.
    /// </summary>
    public static SizedNumber Read(ulong tag, ref CborReader reader)
    {
        var kind = (SizedNumberKind)(tag - CborTagConstants.TAG_I8);
        var itemType = reader.GetCurrentDataItemType();

        if (kind is SizedNumberKind.F16 or SizedNumberKind.F32)
        {
            if (itemType is not (CborDataItemType.Single or CborDataItemType.Double))
            {
                throw new CborException($"CBOR tag {tag} needs a float");
            }

            double value = reader.ReadDouble();

            if (kind == SizedNumberKind.F16)
            {
                if (!SizedNumber.FitsHalf(value))
                {
                    throw new CborException(
                        $"float {value.ToString("R", CultureInfo.InvariantCulture)} cannot be losslessly represented as f16"
                    );
                }

                return SizedNumber.F16(value);
            }

            if (!SizedNumber.FitsSingle(value))
            {
                throw new CborException(
                    $"float {value.ToString("R", CultureInfo.InvariantCulture)} cannot be losslessly represented as f32"
                );
            }

            return SizedNumber.F32((float)value);
        }

        if (itemType is not (CborDataItemType.Signed or CborDataItemType.Unsigned))
        {
            throw new CborException($"CBOR tag {tag} needs an integer");
        }

        if (kind == SizedNumberKind.U64)
        {
            if (itemType == CborDataItemType.Signed)
            {
                throw OutOfRange(reader.ReadInt64(), kind);
            }

            return SizedNumber.U64(reader.ReadUInt64());
        }

        if (itemType == CborDataItemType.Unsigned)
        {
            ulong unsigned = reader.ReadUInt64();
            if (unsigned > long.MaxValue)
            {
                throw new CborException(
                    $"integer {unsigned.ToString(CultureInfo.InvariantCulture)} is outside the {GetRangeName(kind)} range"
                );
            }

            return FromInt64((long)unsigned, kind);
        }

        return FromInt64(reader.ReadInt64(), kind);
    }

    private static SizedNumber FromInt64(long value, SizedNumberKind kind)
    {
        return kind switch
        {
            SizedNumberKind.I8 when value is >= sbyte.MinValue and <= sbyte.MaxValue =>
                SizedNumber.I8((sbyte)value),
            SizedNumberKind.U8 when value is >= byte.MinValue and <= byte.MaxValue =>
                SizedNumber.U8((byte)value),
            SizedNumberKind.I16 when value is >= short.MinValue and <= short.MaxValue =>
                SizedNumber.I16((short)value),
            SizedNumberKind.U16 when value is >= ushort.MinValue and <= ushort.MaxValue =>
                SizedNumber.U16((ushort)value),
            SizedNumberKind.I32 when value is >= int.MinValue and <= int.MaxValue =>
                SizedNumber.I32((int)value),
            SizedNumberKind.U32 when value is >= uint.MinValue and <= uint.MaxValue =>
                SizedNumber.U32((uint)value),
            _ => throw OutOfRange(value, kind),
        };
    }

    private static CborException OutOfRange(long value, SizedNumberKind kind)
    {
        return new CborException(
            $"integer {value.ToString(CultureInfo.InvariantCulture)} is outside the {GetRangeName(kind)} range"
        );
    }

    private static string GetRangeName(SizedNumberKind kind)
    {
        return kind switch
        {
            SizedNumberKind.I8 => "8-bit signed",
            SizedNumberKind.U8 => "8-bit unsigned",
            SizedNumberKind.I16 => "16-bit signed",
            SizedNumberKind.U16 => "16-bit unsigned",
            SizedNumberKind.I32 => "32-bit signed",
            SizedNumberKind.U32 => "32-bit unsigned",
            _ => "64-bit unsigned",
        };
    }
}
