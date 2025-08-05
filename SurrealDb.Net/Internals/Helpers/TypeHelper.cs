using System.Linq.Expressions;
using System.Reflection;
#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace SurrealDb.Net.Internals.Helpers;

internal static class TypeHelper
{
    /// <summary>
    /// Finds the type's implemented <see cref="IEnumerable{T}"/> type.
    /// </summary>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("Calls System.Type.MakeGenericType(params Type[])")]
#endif
    public static Type? FindIEnumerable(
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
#endif
        Type type)
    {
        if (type is null || type == typeof(string))
            return null;

        if (type.IsArray)
            return typeof(IEnumerable<>).MakeGenericType(type.GetElementType()!);

        var typeInfo = type.GetTypeInfo();
        if (typeInfo.IsGenericType)
        {
            foreach (var arg in typeInfo.GenericTypeArguments)
            {
                var ienum = typeof(IEnumerable<>).MakeGenericType(arg);
                if (ienum.GetTypeInfo().IsAssignableFrom(typeInfo))
                {
                    return ienum;
                }
            }
        }

        foreach (var ienum in typeInfo.ImplementedInterfaces.Select(FindIEnumerable))
        {
            if (ienum is not null)
                return ienum;
        }

        if (typeInfo.BaseType is not null && typeInfo.BaseType != typeof(object))
        {
            return FindIEnumerable(typeInfo.BaseType);
        }

        return null;
    }

    /// <summary>
    /// Returns true if the type is a sequence type.
    /// </summary>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("Calls SurrealDb.Net.Internals.Helpers.TypeHelper.FindIEnumerable(Type)")]
#endif
    public static bool IsSequenceType(
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
#endif
        Type type)
    {
        return FindIEnumerable(type) is not null;
    }

    /// <summary>
    /// Gets the constructed <see cref="IEnumerable{T}"/> for the given element type.
    /// </summary>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("Calls System.Type.MakeGenericType(params Type[])")]
#endif
    public static Type GetSequenceType(Type elementType)
    {
        return typeof(IEnumerable<>).MakeGenericType(elementType);
    }

    /// <summary>
    /// Gets the element type given the sequence type.
    /// If the type is not a sequence, returns the type itself.
    /// </summary>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("Calls SurrealDb.Net.Internals.Helpers.TypeHelper.FindIEnumerable(Type)")]
#endif
    public static Type GetElementType(
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
#endif
        Type sequenceType)
    {
        var ienum = FindIEnumerable(sequenceType);
        if (ienum is null)
            return sequenceType;
        return ienum.GetTypeInfo().GenericTypeArguments[0];
    }

    /// <summary>
    /// Returns true if the type is a <see cref="Nullable{T}"/>.
    /// </summary>
    public static bool IsNullableType(Type type)
    {
        return type is not null
            && type.GetTypeInfo().IsGenericType
            && type.GetGenericTypeDefinition() == typeof(Nullable<>);
    }

    /// <summary>
    /// Returns true if the type can be assigned the value null.
    /// </summary>
    public static bool IsNullAssignable(Type type)
    {
        return !type.GetTypeInfo().IsValueType || IsNullableType(type);
    }

    /// <summary>
    /// Gets the underlying type if the specified type is a <see cref="Nullable{T}"/>,
    /// otherwise just returns given type.
    /// </summary>
    public static Type GetNonNullableType(Type type)
    {
        if (IsNullableType(type))
        {
            return type.GetTypeInfo().GenericTypeArguments[0];
        }

        return type;
    }

    /// <summary>
    /// Gets a null-assignable variation of the type.
    /// Returns a <see cref="Nullable{T}"/> type if the given type is a value type.
    /// </summary>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode("Calls System.Type.MakeGenericType(params Type[])")]
#endif
    public static Type GetNullAssignableType(Type type)
    {
        if (!IsNullAssignable(type))
        {
            return typeof(Nullable<>).MakeGenericType(type);
        }

        return type;
    }

    /// <summary>
    /// Gets the <see cref="ConstantExpression"/> for null of the specified type.
    /// </summary>
#if NET7_0_OR_GREATER
    [RequiresDynamicCode(
        "Calls SurrealDb.Net.Internals.Helpers.TypeHelper.GetNullAssignableType(Type)"
    )]
#endif
    public static ConstantExpression GetNullConstant(Type type)
    {
        return Expression.Constant(null, GetNullAssignableType(type));
    }

    /// <summary>
    /// Gets the type of the <see cref="MemberInfo"/>.
    /// </summary>
    public static Type? GetMemberType(MemberInfo mi)
    {
        var fi = mi as FieldInfo;
        if (fi is not null)
            return fi.FieldType;
        var pi = mi as PropertyInfo;
        if (pi is not null)
            return pi.PropertyType;
        var ei = mi as EventInfo;
        if (ei is not null)
            return ei.EventHandlerType;
        var meth = mi as MethodInfo; // property getters really
        if (meth is not null)
            return meth.ReturnType;
        return null;
    }

    /// <summary>
    /// Gets the default value of the specified type.
    /// </summary>
    public static object? GetDefault(
#if NET5_0_OR_GREATER
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
#endif
        Type type)
    {
        bool isNullable = !type.GetTypeInfo().IsValueType || IsNullableType(type);
        if (!isNullable)
            return Activator.CreateInstance(type);
        return null;
    }

    /// <summary>
    /// Returns true if the member is either a read-only field or get-only property.
    /// </summary>
    public static bool IsReadOnly(MemberInfo member)
    {
        var pi = member as PropertyInfo;
        if (pi is not null)
        {
            return !pi.CanWrite || pi.SetMethod == null;
        }

        var fi = member as FieldInfo;
        if (fi is not null)
        {
            return (fi.Attributes & FieldAttributes.InitOnly) != FieldAttributes.PrivateScope;
        }

        return true;
    }

    /// <summary>
    /// Return true if the type is a kind of integer.
    /// </summary>
    public static bool IsInteger(Type type)
    {
        Type nnType = GetNonNullableType(type);

        switch (GetTypeCode(nnType))
        {
            case TypeCode.SByte:
            case TypeCode.Int16:
            case TypeCode.Int32:
            case TypeCode.Int64:
            case TypeCode.Byte:
            case TypeCode.UInt16:
            case TypeCode.UInt32:
            case TypeCode.UInt64:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Gets the <see cref="TypeCode"/> for the specified type.
    /// </summary>
    public static TypeCode GetTypeCode(Type type)
    {
        if (type == typeof(bool))
        {
            return TypeCode.Boolean;
        }
        if (type == typeof(byte))
        {
            return TypeCode.Byte;
        }
        if (type == typeof(sbyte))
        {
            return TypeCode.SByte;
        }
        if (type == typeof(short))
        {
            return TypeCode.Int16;
        }
        if (type == typeof(ushort))
        {
            return TypeCode.UInt16;
        }
        if (type == typeof(int))
        {
            return TypeCode.Int32;
        }
        if (type == typeof(uint))
        {
            return TypeCode.UInt32;
        }
        if (type == typeof(long))
        {
            return TypeCode.Int64;
        }
        if (type == typeof(ulong))
        {
            return TypeCode.UInt64;
        }
        if (type == typeof(float))
        {
            return TypeCode.Single;
        }
        if (type == typeof(double))
        {
            return TypeCode.Double;
        }
        if (type == typeof(decimal))
        {
            return TypeCode.Decimal;
        }
        if (type == typeof(string))
        {
            return TypeCode.String;
        }
        if (type == typeof(char))
        {
            return TypeCode.Char;
        }
        if (type == typeof(DateTime))
        {
            return TypeCode.DateTime;
        }

        return TypeCode.Object;
    }

    /// <summary>
    /// True if the type is assignable from the other type.
    /// </summary>
    public static bool IsAssignableFrom(this Type type, Type otherType)
    {
        return type.GetTypeInfo().IsAssignableFrom(otherType.GetTypeInfo());
    }
}
