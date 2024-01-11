using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Dahomey.Cbor.Attributes;
using Dahomey.Cbor.Serialization.Conventions;

namespace SurrealDb.Net.Internals.Cbor;

internal sealed class SurrealDbCborNamingConvention : INamingConvention
{
    public string GetPropertyName(MemberInfo member)
    {
        var cborPropertyAttribute = member.GetCustomAttribute<CborPropertyAttribute>();
        if (
            cborPropertyAttribute is not null
            && !string.IsNullOrEmpty(cborPropertyAttribute.PropertyName)
        )
        {
            return cborPropertyAttribute.PropertyName;
        }

        var columnAttribute = member.GetCustomAttribute<ColumnAttribute>();
        if (columnAttribute is not null && !string.IsNullOrEmpty(columnAttribute.Name))
        {
            return columnAttribute.Name;
        }

        return member.Name;
    }
}
