using Dahomey.Cbor;
using Dahomey.Cbor.Serialization.Converters;
using Dahomey.Cbor.Serialization.Converters.Providers;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal class PrimitiveConverterProvider : CborConverterProviderBase
{
    public override ICborConverter? GetConverter(Type type, CborOptions options)
    {
        if (type == typeof(decimal))
        {
            return new DecimalConverter();
        }
        if (type == typeof(DateTime))
        {
            return new DateTimeConverter();
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
        if (type == typeof(Thing))
        {
            return new ThingConverter();
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

        return null;
    }
}
