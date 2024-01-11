using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using Dahomey.Cbor.Attributes;

namespace SurrealDb.Net.Internals.Extensions;

internal static class ReflectionExtensions
{
    public static bool IsNullableOfT(this Type type)
    {
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }

    public static bool IsNullableOf(this Type type, Type otherType)
    {
        return type.IsNullableOfT() && type.GetGenericArguments()[0] == otherType;
    }

    public static bool InheritsClass(this Type type, Type classType)
    {
        return type == classType || type.IsSubclassOf(classType);
    }

    public static bool InheritsInterface(this Type type, Type interfaceType)
    {
        return type == interfaceType || type.IsSubclassOf(interfaceType);
    }

    public static Type GetNullableType(this Type type)
    {
        if (type.IsNullableOfT())
        {
            return type;
        }

        return type.IsValueType ? typeof(Nullable<>).MakeGenericType(type) : type;
    }

    public static string GetDatabaseFieldName(MemberInfo property)
    {
        // retrieve from CborPropertyAttribute
        var cborPropertyAttribute = property.GetCustomAttribute(typeof(CborPropertyAttribute));
        if (cborPropertyAttribute is not null)
        {
            var propertyNameAttribute = ((CborPropertyAttribute)cborPropertyAttribute).PropertyName;
            if (!string.IsNullOrWhiteSpace(propertyNameAttribute))
            {
                return propertyNameAttribute;
            }
        }

        // retrieve from ColumnAttribute
        var columnAttribute = property.GetCustomAttribute(typeof(ColumnAttribute));
        if (columnAttribute is not null)
        {
            var nameAttribute = ((ColumnAttribute)columnAttribute).Name;
            if (!string.IsNullOrWhiteSpace(nameAttribute))
            {
                return nameAttribute;
            }
        }

        return property.Name;
    }
}
