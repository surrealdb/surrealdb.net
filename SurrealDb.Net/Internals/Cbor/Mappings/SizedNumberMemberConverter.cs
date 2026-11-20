using Dahomey.Cbor;
using Dahomey.Cbor.Attributes;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using SurrealDb.Net.Internals.Cbor.Converters;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Cbor.Mappings;

/// <summary>
/// Reads a <see cref="SizedNumber"/> member straight from the reader, so its tag is still there,
/// and leaves everything else to <paramref name="innerConverter"/>.
/// </summary>
internal sealed class SizedNumberMemberConverter(
    IMemberConverter innerConverter,
    SizedNumberConverter converter,
    bool isNullable
) : IMemberConverter
{
    public ReadOnlySpan<byte> MemberName => innerConverter.MemberName;
    public int? MemberIndex => innerConverter.MemberIndex;
    public bool IgnoreIfDefault => innerConverter.IgnoreIfDefault;
    public RequirementPolicy RequirementPolicy => innerConverter.RequirementPolicy;

    public void Read(ref CborReader reader, object obj)
    {
        innerConverter.Set(obj, Read(ref reader));
    }

    public object Read(ref CborReader reader)
    {
        if (reader.TryReadSemanticTag(out ulong tag))
        {
            return converter.ReadTagged(tag, ref reader);
        }

        if (isNullable && reader.GetCurrentDataItemType() == CborDataItemType.Null)
        {
            reader.ReadNull();
            return null!;
        }

        throw SizedNumberConverter.MissingTag();
    }

    public void Write(ref CborWriter writer, object obj)
    {
        innerConverter.Write(ref writer, obj);
    }

    public void Set(object obj, object value)
    {
        innerConverter.Set(obj, value);
    }

    public bool ShouldSerialize(object obj, Type declaredType, CborOptions options)
    {
        return innerConverter.ShouldSerialize(obj, declaredType, options);
    }
}
