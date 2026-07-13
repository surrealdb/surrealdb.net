using System.Linq;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters.Mappings;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Cbor.Mappings;

/// <summary>
/// Decorates another <see cref="IObjectMappingConvention"/> to guarantee that, for any concrete
/// type implementing <see cref="IRecord"/>, the <c>Id</c> member is always serialized under the
/// literal CBOR property name "id" and omitted from the payload when left unset.
/// </summary>
/// <remarks>
/// C# does not propagate attributes declared on an interface member (here,
/// <see cref="IRecord.Id"/>'s <c>[CborProperty("id")]</c> / <c>[CborIgnoreIfDefault]</c>) onto a
/// class's own property when that property only implicitly implements the interface member.
/// Without this convention, a class implementing <see cref="IRecord"/> directly (rather than
/// inheriting <see cref="Record"/>) would have its <c>Id</c> property serialized under the
/// literal member name "Id", riding along in the CBOR payload alongside the RPC's own "id"
/// argument and producing a duplicate field on the server (see GitHub issue #212).
/// </remarks>
internal sealed class RecordIdObjectMappingConvention : IObjectMappingConvention
{
    private readonly IObjectMappingConvention _innerConvention;

    public RecordIdObjectMappingConvention(IObjectMappingConvention innerConvention)
    {
        _innerConvention = innerConvention;
    }

    public void Apply<T>(SerializationRegistry registry, ObjectMapping<T> objectMapping)
    {
        _innerConvention.Apply(registry, objectMapping);

        if (!typeof(IRecord).IsAssignableFrom(typeof(T)))
        {
            return;
        }

        var idMemberMapping = objectMapping
            .MemberMappings.OfType<MemberMapping<T>>()
            .FirstOrDefault(memberMapping =>
                memberMapping.MemberInfo.Name == nameof(IRecord.Id)
                && memberMapping.MemberType == typeof(RecordId)
            );

        idMemberMapping?.SetMemberName("id").SetIngoreIfDefault(true);
    }
}
