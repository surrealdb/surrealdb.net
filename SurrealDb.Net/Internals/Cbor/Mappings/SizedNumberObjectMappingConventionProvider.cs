using System.Reflection;
using Dahomey.Cbor.Serialization.Converters.Mappings;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Cbor.Mappings;

/// <summary>
/// Supplies a <see cref="SizedNumberObjectMappingConvention"/> for every type with a
/// <see cref="SizedNumber"/> property or field, keeping the <see cref="RecordIdObjectMappingConvention"/>
/// for types implementing <see cref="IRecord"/>.
/// </summary>
internal sealed class SizedNumberObjectMappingConventionProvider : IObjectMappingConventionProvider
{
    private readonly DefaultObjectMappingConvention _defaultObjectMappingConvention = new();

    public IObjectMappingConvention? GetConvention(Type type)
    {
        const BindingFlags MEMBER_FLAGS = BindingFlags.Instance | BindingFlags.Public;

        bool hasSizedNumberMember =
            type.GetProperties(MEMBER_FLAGS)
                .Any(property =>
                    SizedNumberObjectMappingConvention.IsSizedNumberType(property.PropertyType)
                )
            || type.GetFields(MEMBER_FLAGS)
                .Any(field =>
                    SizedNumberObjectMappingConvention.IsSizedNumberType(field.FieldType)
                );

        if (!hasSizedNumberMember)
        {
            return null;
        }

        IObjectMappingConvention innerConvention = typeof(IRecord).IsAssignableFrom(type)
            ? new RecordIdObjectMappingConvention(_defaultObjectMappingConvention)
            : _defaultObjectMappingConvention;

        return new SizedNumberObjectMappingConvention(innerConvention);
    }
}
