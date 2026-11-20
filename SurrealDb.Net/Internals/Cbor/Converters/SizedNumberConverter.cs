using System.Globalization;
using Dahomey.Cbor;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Cbor.Converters;

/// <summary>
/// Writes a <see cref="SizedNumber"/> under its tag (180 to 188) on a <c>cbor.2</c> connection,
/// and widened to an int, float or decimal (tag 10) on a plain <c>cbor</c> connection.
/// </summary>
internal sealed class SizedNumberConverter : CborConverterBase<SizedNumber>
{
    private readonly bool _cbor2;

    public SizedNumberConverter(bool cbor2)
    {
        _cbor2 = cbor2;
    }

    public override SizedNumber Read(ref CborReader reader)
    {
        return reader.TryReadSemanticTag(out ulong tag)
            ? ReadTagged(tag, ref reader)
            : throw MissingTag();
    }

    /// <summary>
    /// Reads the item under <paramref name="tag"/>, which the caller has already read.
    /// </summary>
    internal SizedNumber ReadTagged(ulong tag, ref CborReader reader)
    {
        if (!SizedNumberReader.IsSizedTag(tag))
        {
            throw MissingTag();
        }

        if (!_cbor2)
        {
            // 💡 Plain cbor refuses the sized-number tags, as the server does
            throw new CborException($"CBOR tag {tag} is not accepted here: use cbor.2");
        }

        return SizedNumberReader.Read(tag, ref reader);
    }

    internal static CborException MissingTag()
    {
        return new CborException(
            "Expected a sized number (CBOR tags 180 to 188), which only a cbor.2 connection carries"
        );
    }

    public override void Write(ref CborWriter writer, SizedNumber value)
    {
        if (_cbor2)
        {
            writer.WriteSemanticTag(SizedNumberReader.GetTag(value.Kind));
        }

        switch (value.Kind)
        {
            case SizedNumberKind.U64:
                ulong unsigned = value.ToUInt64();
                if (_cbor2 || unsigned <= long.MaxValue)
                {
                    writer.WriteUInt64(unsigned);
                }
                else
                {
                    writer.WriteSemanticTag(CborTagConstants.TAG_STRING_DECIMAL);
                    writer.WriteString(unsigned.ToString(CultureInfo.InvariantCulture));
                }
                break;
            case SizedNumberKind.F16:
            case SizedNumberKind.F32:
                writer.WriteDouble(value.ToDouble());
                break;
            default:
                writer.WriteInt64(value.ToInt64());
                break;
        }
    }
}
