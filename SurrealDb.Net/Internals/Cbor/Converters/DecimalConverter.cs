using System.Globalization;
using Dahomey.Cbor.Serialization;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal class DecimalConverter : Dahomey.Cbor.Serialization.Converters.DecimalConverter
{
    public override void Write(ref CborWriter writer, decimal value)
    {
        writer.WriteSemanticTag(CborTagConstants.TAG_STRING_DECIMAL);
        writer.WriteString(value.ToString(CultureInfo.InvariantCulture));
    }
}
