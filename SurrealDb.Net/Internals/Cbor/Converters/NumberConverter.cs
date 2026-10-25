using System.Globalization;
using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal delegate T CborValueReader<out T>(ref CborReader reader);

internal delegate void CborValueWriter<in T>(ref CborWriter writer, T value);

/// <summary>
/// Reads a .NET number from a plain CBOR number, and also from a sized number (tags 180 to 188,
/// sent on a <c>cbor.2</c> connection) or a decimal string (tag 10, which carries integers
/// above <see cref="long.MaxValue"/>). Writes it as a plain SurrealDB <c>int</c> or <c>float</c>.
/// </summary>
internal sealed class NumberConverter<T> : CborConverterBase<T>
{
    private readonly CborValueReader<T> _read;
    private readonly CborValueWriter<T> _write;
    private readonly Func<SizedNumber, T> _fromSized;
    private readonly Func<decimal, T> _fromDecimal;

    public NumberConverter(
        CborValueReader<T> read,
        CborValueWriter<T> write,
        Func<SizedNumber, T> fromSized,
        Func<decimal, T> fromDecimal
    )
    {
        _read = read;
        _write = write;
        _fromSized = fromSized;
        _fromDecimal = fromDecimal;
    }

    public override T Read(ref CborReader reader)
    {
        if (reader.TryReadSemanticTag(out ulong tag))
        {
            if (SizedNumberReader.IsSizedTag(tag))
            {
                var sized = SizedNumberReader.Read(tag, ref reader);
                return Convert(sized, _fromSized);
            }

            if (tag == CborTagConstants.TAG_STRING_DECIMAL)
            {
                return ReadDecimalString(ref reader);
            }
        }

        // 💡 Dahomey reads an object member's header, and skips its tag, before calling the
        // member's converter, so a decimal (tag 10) can arrive here as a bare string
        if (reader.GetCurrentDataItemType() == CborDataItemType.String)
        {
            return ReadDecimalString(ref reader);
        }

        return _read(ref reader);
    }

    private T ReadDecimalString(ref CborReader reader)
    {
        var value = decimal.Parse(
            reader.ReadString()!,
            NumberStyles.Float,
            CultureInfo.InvariantCulture
        );
        return Convert(value, _fromDecimal);
    }

    public override void Write(ref CborWriter writer, T value)
    {
        _write(ref writer, value);
    }

    private static T Convert<TSource>(TSource value, Func<TSource, T> convert)
    {
        try
        {
            return convert(value);
        }
        catch (Exception ex) when (ex is OverflowException or InvalidOperationException)
        {
            throw new CborException($"Cannot read {value} as {typeof(T).Name}: {ex.Message}");
        }
    }
}
