using System.Linq;
using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters.Mappings;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Cbor.Mappings;

/// <summary>
/// Decorates another <see cref="IObjectMappingConvention"/> so that a <see cref="SizedNumber"/>
/// member is read through <see cref="SizedNumberMemberMapping"/>.
/// </summary>
/// <remarks>
/// Dahomey reads the header of each member before calling the member's converter, and in doing
/// so skips the member's CBOR tag. A <see cref="SizedNumber"/> needs that tag (180 to 188) to know
/// its kind, so its members must be read without that look-ahead.
/// </remarks>
internal sealed class SizedNumberObjectMappingConvention : IObjectMappingConvention
{
    private readonly IObjectMappingConvention _innerConvention;

    public SizedNumberObjectMappingConvention(IObjectMappingConvention innerConvention)
    {
        _innerConvention = innerConvention;
    }

    public void Apply<T>(SerializationRegistry registry, ObjectMapping<T> objectMapping)
    {
        _innerConvention.Apply(registry, objectMapping);

        var memberMappings = objectMapping.MemberMappings.ToList();

        if (!memberMappings.Any(IsSizedNumberMember))
        {
            return;
        }

        objectMapping.SetMemberMappings(
            memberMappings
                .Select(memberMapping =>
                    IsSizedNumberMember(memberMapping)
                        ? new SizedNumberMemberMapping(memberMapping, registry)
                        : memberMapping
                )
                .ToList()
        );
    }

    internal static bool IsSizedNumberType(Type type)
    {
        return type == typeof(SizedNumber) || type == typeof(SizedNumber?);
    }

    private static bool IsSizedNumberMember(IMemberMapping memberMapping)
    {
        return IsSizedNumberType(memberMapping.MemberType);
    }
}
