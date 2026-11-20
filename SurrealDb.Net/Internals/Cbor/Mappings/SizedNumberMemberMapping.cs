using System.Reflection;
using Dahomey.Cbor;
using Dahomey.Cbor.Attributes;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;
using Dahomey.Cbor.Serialization.Converters.Mappings;
using SurrealDb.Net.Internals.Cbor.Converters;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Cbor.Mappings;

/// <summary>
/// Maps a <see cref="SizedNumber"/> member as <paramref name="innerMapping"/> does, but reads it
/// with <see cref="SizedNumberMemberConverter"/>.
/// </summary>
internal sealed class SizedNumberMemberMapping(
    IMemberMapping innerMapping,
    SerializationRegistry registry
) : IMemberMapping
{
    public MemberInfo? MemberInfo => innerMapping.MemberInfo;
    public Type MemberType => innerMapping.MemberType;
    public string MemberName => innerMapping.MemberName!;
    public int? MemberIndex => innerMapping.MemberIndex;
    public ICborConverter? Converter => innerMapping.Converter;
    public bool CanBeDeserialized => innerMapping.CanBeDeserialized;
    public bool CanBeSerialized => innerMapping.CanBeSerialized;
    public object? DefaultValue => innerMapping.DefaultValue;
    public bool IgnoreIfDefault => innerMapping.IgnoreIfDefault;
    public Func<object, bool>? ShouldSerializeMethod => innerMapping.ShouldSerializeMethod;
    public LengthMode LengthMode => innerMapping.LengthMode;
    public RequirementPolicy RequirementPolicy => innerMapping.RequirementPolicy;

    public IMemberConverter GenerateMemberConverter()
    {
        var converter = (SizedNumberConverter)
            registry.ConverterRegistry.Lookup(typeof(SizedNumber));

        return new SizedNumberMemberConverter(
            innerMapping.GenerateMemberConverter(),
            converter,
            MemberType == typeof(SizedNumber?)
        );
    }

    public string? GetMemberNameForConverter(ICborConverter converter)
    {
        return innerMapping.GetMemberNameForConverter(converter);
    }
}
