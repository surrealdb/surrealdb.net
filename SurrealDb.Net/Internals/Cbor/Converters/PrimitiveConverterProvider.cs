using Dahomey.Cbor;
using Dahomey.Cbor.Serialization.Converters;
using Dahomey.Cbor.Serialization.Converters.Providers;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal sealed class PrimitiveConverterProvider : CborConverterProviderBase
{
    private readonly bool _cbor2;

    public PrimitiveConverterProvider(bool cbor2)
    {
        _cbor2 = cbor2;
    }

    public override ICborConverter? GetConverter(Type type, CborOptions options)
    {
        if (type == typeof(sbyte))
        {
            return NumberConverters.SByte;
        }
        if (type == typeof(byte))
        {
            return NumberConverters.Byte;
        }
        if (type == typeof(short))
        {
            return NumberConverters.Int16;
        }
        if (type == typeof(ushort))
        {
            return NumberConverters.UInt16;
        }
        if (type == typeof(int))
        {
            return NumberConverters.Int32;
        }
        if (type == typeof(uint))
        {
            return NumberConverters.UInt32;
        }
        if (type == typeof(long))
        {
            return NumberConverters.Int64;
        }
        if (type == typeof(ulong))
        {
            return NumberConverters.UInt64;
        }
        if (type == typeof(float))
        {
            return NumberConverters.Single;
        }
        if (type == typeof(double))
        {
            return NumberConverters.Double;
        }
#if NET5_0_OR_GREATER
        if (type == typeof(Half))
        {
            return NumberConverters.Half;
        }
#endif
        if (type == typeof(SizedNumber))
        {
            return new SizedNumberConverter(_cbor2);
        }
        if (type == typeof(decimal))
        {
            return new DecimalConverter();
        }
        if (type == typeof(DateTime))
        {
            return new DateTimeConverter();
        }
        if (type == typeof(DateTimeOffset))
        {
            return new DateTimeOffsetConverter();
        }
        if (type == typeof(TimeSpan))
        {
            return new TimeSpanConverter();
        }
        if (type == typeof(Guid))
        {
            return new GuidConverter();
        }
        if (type == typeof(Duration))
        {
            return new DurationConverter();
        }
        if (type == typeof(RecordIdOfString))
        {
            return new RecordIdOfStringConverter();
        }
        if (type == typeof(RecordId))
        {
            return new RecordIdConverter(options);
        }
        if (type == typeof(None))
        {
            return new NoneConverter();
        }
        if (type == typeof(StringRecordId))
        {
            return new StringRecordIdConverter();
        }
        if (type == typeof(Table))
        {
            return new TableConverter();
        }

#if NET6_0_OR_GREATER
        if (type == typeof(DateOnly))
        {
            return new DateOnlyConverter();
        }
        if (type == typeof(TimeOnly))
        {
            return new TimeOnlyConverter();
        }
#endif
        if (type == typeof(Future))
        {
            return new FutureConverter();
        }
        if (type == typeof(SurrealFile))
        {
            return new SurrealFileConverter();
        }

        return null;
    }
}
