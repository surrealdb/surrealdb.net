using System.Globalization;

namespace SurrealDb.Net.Models;

/// <summary>
/// The fixed-width number kinds of SurrealDB 4.0.
/// </summary>
public enum SizedNumberKind
{
    I8,
    U8,
    I16,
    U16,
    I32,
    U32,
    U64,
    F16,
    F32,
}

/// <summary>
/// A SurrealDB 4.0 fixed-width number: <c>i8</c>, <c>u8</c>, <c>i16</c>, <c>u16</c>, <c>i32</c>,
/// <c>u32</c>, <c>u64</c>, <c>f16</c> or <c>f32</c>.<br /><br />
///
/// .NET numeric types such as <see cref="int"/> or <see cref="float"/> are always sent as a
/// SurrealDB <c>int</c>, <c>float</c> or <c>decimal</c>. Use <see cref="SizedNumber"/> to send
/// one of the sized kinds instead.<br /><br />
///
/// The sized kinds need the <c>cbor.2</c> wire format, which the client negotiates with servers
/// that support it. On a connection that uses plain <c>cbor</c>, a <see cref="SizedNumber"/> is
/// sent widened to an <c>int</c>, <c>float</c> or <c>decimal</c>, and the server sends sized
/// numbers widened too, so they can only be read as .NET numeric types.
/// </summary>
public readonly struct SizedNumber : IEquatable<SizedNumber>
{
    private const double HALF_MAX_VALUE = 65504;
    private const double HALF_MIN_NORMAL_VALUE = 6.103515625e-05;
    private const double HALF_SUBNORMAL_SCALE = 16777216; // 2^24

    private readonly ulong _bits;

    /// <summary>
    /// The kind of this number.
    /// </summary>
    public SizedNumberKind Kind { get; }

    /// <summary>
    /// Whether this number is one of the integer kinds.
    /// </summary>
    public bool IsInteger => Kind < SizedNumberKind.F16;

    /// <summary>
    /// Whether this number is one of the floating-point kinds.
    /// </summary>
    public bool IsFloat => Kind >= SizedNumberKind.F16;

    private SizedNumber(SizedNumberKind kind, ulong bits)
    {
        Kind = kind;
        _bits = bits;
    }

    public static SizedNumber I8(sbyte value) => new(SizedNumberKind.I8, unchecked((ulong)value));

    public static SizedNumber U8(byte value) => new(SizedNumberKind.U8, value);

    public static SizedNumber I16(short value) => new(SizedNumberKind.I16, unchecked((ulong)value));

    public static SizedNumber U16(ushort value) => new(SizedNumberKind.U16, value);

    public static SizedNumber I32(int value) => new(SizedNumberKind.I32, unchecked((ulong)value));

    public static SizedNumber U32(uint value) => new(SizedNumberKind.U32, value);

    public static SizedNumber U64(ulong value) => new(SizedNumberKind.U64, value);

    /// <summary>
    /// Creates an <c>f16</c> number.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The value is not exactly representable as a half-precision float.
    /// </exception>
    public static SizedNumber F16(double value)
    {
        if (!FitsHalf(value))
        {
            throw new ArgumentOutOfRangeException(
                nameof(value),
                value,
                "The value cannot be losslessly represented as an f16."
            );
        }

        return new(SizedNumberKind.F16, unchecked((ulong)BitConverter.DoubleToInt64Bits(value)));
    }

#if NET5_0_OR_GREATER
    public static SizedNumber F16(Half value) => F16((double)value);
#endif

    public static SizedNumber F32(float value) =>
        new(SizedNumberKind.F32, unchecked((ulong)BitConverter.DoubleToInt64Bits(value)));

    /// <summary>
    /// Returns the value of an integer kind as an <see cref="long"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">This number is a floating-point kind.</exception>
    /// <exception cref="OverflowException">This <c>u64</c> is larger than <see cref="long.MaxValue"/>.</exception>
    public long ToInt64()
    {
        EnsureInteger();
        return Kind == SizedNumberKind.U64 ? checked((long)_bits) : unchecked((long)_bits);
    }

    /// <summary>
    /// Returns the value of an integer kind as an <see cref="ulong"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">This number is a floating-point kind.</exception>
    /// <exception cref="OverflowException">This number is negative.</exception>
    public ulong ToUInt64()
    {
        EnsureInteger();
        return Kind == SizedNumberKind.U64 ? _bits : checked((ulong)unchecked((long)_bits));
    }

    /// <summary>
    /// Returns the value as a <see cref="double"/>.
    /// </summary>
    public double ToDouble()
    {
        if (IsFloat)
        {
            return BitConverter.Int64BitsToDouble(unchecked((long)_bits));
        }

        return Kind == SizedNumberKind.U64 ? _bits : unchecked((long)_bits);
    }

    public bool Equals(SizedNumber other)
    {
        if (Kind != other.Kind)
        {
            return false;
        }

        if (IsFloat && double.IsNaN(ToDouble()))
        {
            return double.IsNaN(other.ToDouble());
        }

        return _bits == other._bits;
    }

    public override bool Equals(object? obj)
    {
        return obj is SizedNumber other && Equals(other);
    }

    public override int GetHashCode()
    {
        return IsFloat && double.IsNaN(ToDouble())
            ? HashCode.Combine(Kind, double.NaN)
            : HashCode.Combine(Kind, _bits);
    }

    public static bool operator ==(SizedNumber left, SizedNumber right) => left.Equals(right);

    public static bool operator !=(SizedNumber left, SizedNumber right) => !left.Equals(right);

    /// <summary>
    /// Returns the number in SurrealQL notation, such as <c>5u8</c> or <c>1.5f32</c>.
    /// </summary>
    public override string ToString()
    {
        string suffix = Kind.ToString().ToLowerInvariant();

        if (IsFloat)
        {
            double value = ToDouble();

            if (double.IsNaN(value))
            {
                return $"<{suffix}>NaN";
            }
            if (double.IsInfinity(value))
            {
                return value > 0 ? $"<{suffix}>Infinity" : $"<{suffix}>-Infinity";
            }

            return value.ToString("R", CultureInfo.InvariantCulture) + suffix;
        }

        string number =
            Kind == SizedNumberKind.U64
                ? _bits.ToString(CultureInfo.InvariantCulture)
                : unchecked((long)_bits).ToString(CultureInfo.InvariantCulture);

        return number + suffix;
    }

    internal static bool FitsHalf(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value == 0)
        {
            return true;
        }

        double magnitude = Math.Abs(value);

        if (magnitude > HALF_MAX_VALUE)
        {
            return false;
        }

        if (magnitude < HALF_MIN_NORMAL_VALUE)
        {
            // Subnormal halves are the multiples of 2^-24
            double scaled = magnitude * HALF_SUBNORMAL_SCALE;
            return scaled == Math.Floor(scaled);
        }

        // A normal half keeps 10 of the 52 fraction bits of a double
        const long DROPPED_FRACTION_BITS = (1L << 42) - 1;
        return (BitConverter.DoubleToInt64Bits(magnitude) & DROPPED_FRACTION_BITS) == 0;
    }

    internal static bool FitsSingle(double value)
    {
        return double.IsNaN(value) || (double)(float)value == value;
    }

    private void EnsureInteger()
    {
        if (!IsInteger)
        {
            throw new InvalidOperationException($"A {Kind} number is not an integer.");
        }
    }
}
