using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Dahomey.Cbor.Serialization.Conventions;

namespace SurrealDb.Net.Internals.Cbor;

internal sealed class SurrealDbCborNamingConvention : INamingConvention
{
    public string GetPropertyName(MemberInfo member)
    {
        var columnAttribute = member.GetCustomAttribute<ColumnAttribute>();
        return columnAttribute is not null && !string.IsNullOrEmpty(columnAttribute.Name)
            ? columnAttribute.Name
            : member.Name;
    }
}
