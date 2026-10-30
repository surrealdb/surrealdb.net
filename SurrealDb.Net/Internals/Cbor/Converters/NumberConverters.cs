using System.Globalization;
using Dahomey.Cbor.Serialization;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal static class NumberConverters
{
    public static readonly NumberConverter<sbyte> SByte = new(
        (ref CborReader reader) => reader.ReadSByte(),
        (ref CborWriter writer, sbyte value) => writer.WriteInt64(value),
        sized => checked((sbyte)sized.ToInt64()),
        value => checked((sbyte)ToWhole(value))
    );

    public static readonly NumberConverter<byte> Byte = new(
        (ref CborReader reader) => reader.ReadByte(),
        (ref CborWriter writer, byte value) => writer.WriteUInt64(value),
        sized => checked((byte)sized.ToUInt64()),
        value => checked((byte)ToWhole(value))
    );

    public static readonly NumberConverter<short> Int16 = new(
        (ref CborReader reader) => reader.ReadInt16(),
        (ref CborWriter writer, short value) => writer.WriteInt64(value),
        sized => checked((short)sized.ToInt64()),
        value => checked((short)ToWhole(value))
    );

    public static readonly NumberConverter<ushort> UInt16 = new(
        (ref CborReader reader) => reader.ReadUInt16(),
        (ref CborWriter writer, ushort value) => writer.WriteUInt64(value),
        sized => checked((ushort)sized.ToUInt64()),
        value => checked((ushort)ToWhole(value))
    );

    public static readonly NumberConverter<int> Int32 = new(
        (ref CborReader reader) => reader.ReadInt32(),
        (ref CborWriter writer, int value) => writer.WriteInt64(value),
        sized => checked((int)sized.ToInt64()),
        value => checked((int)ToWhole(value))
    );

    public static readonly NumberConverter<uint> UInt32 = new(
        (ref CborReader reader) => reader.ReadUInt32(),
        (ref CborWriter writer, uint value) => writer.WriteUInt64(value),
        sized => checked((uint)sized.ToUInt64()),
        value => checked((uint)ToWhole(value))
    );

    public static readonly NumberConverter<long> Int64 = new(
        (ref CborReader reader) => reader.ReadInt64(),
        (ref CborWriter writer, long value) => writer.WriteInt64(value),
        sized => sized.ToInt64(),
        value => checked((long)ToWhole(value))
    );

    /// <summary>
    /// SurrealDB integers are signed 64-bit, and a larger CBOR integer is refused, so a
    /// <see cref="ulong"/> above <see cref="long.MaxValue"/> is sent as a decimal (tag 10).
    /// </summary>
    public static readonly NumberConverter<ulong> UInt64 = new(
        (ref CborReader reader) => reader.ReadUInt64(),
        WriteUInt64,
        sized => sized.ToUInt64(),
        value => checked((ulong)ToWhole(value))
    );

    public static readonly NumberConverter<float> Single = new(
        (ref CborReader reader) => reader.ReadSingle(),
        (ref CborWriter writer, float value) => writer.WriteSingle(value),
        sized => (float)sized.ToDouble(),
        value => (float)value
    );

    public static readonly NumberConverter<double> Double = new(
        (ref CborReader reader) => reader.ReadDouble(),
        (ref CborWriter writer, double value) => writer.WriteDouble(value),
        sized => sized.ToDouble(),
        value => (double)value
    );

#if NET5_0_OR_GREATER
    public static readonly NumberConverter<Half> Half = new(
        (ref CborReader reader) => (Half)reader.ReadDouble(),
        (ref CborWriter writer, Half value) => writer.WriteDouble((double)value),
        sized => (Half)sized.ToDouble(),
        value => (Half)(double)value
    );
#endif

    private static void WriteUInt64(ref CborWriter writer, ulong value)
    {
        if (value <= long.MaxValue)
        {
            writer.WriteUInt64(value);
            return;
        }

        writer.WriteSemanticTag(CborTagConstants.TAG_STRING_DECIMAL);
        writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
    }

    private static decimal ToWhole(decimal value)
    {
        if (value != decimal.Truncate(value))
        {
            throw new OverflowException("The decimal is not a whole number.");
        }

        return value;
    }
}
