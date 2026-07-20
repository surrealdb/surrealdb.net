using System.Collections.Concurrent;
using System.Reflection;
using SurrealDb.Net.Attributes;
using SurrealDb.Net.Internals.Queryable.Expressions.Surreal;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Queryable;

internal sealed class GraphEdgeMetadata
{
    public PropertyInfo InProperty { get; }

    public PropertyInfo OutProperty { get; }

    public GraphEdgeMetadata(PropertyInfo inProperty, PropertyInfo outProperty)
    {
        InProperty = inProperty;
        OutProperty = outProperty;
    }

    public PropertyInfo GetEndpointProperty(GraphDirection direction)
    {
        return direction == GraphDirection.Out ? OutProperty : InProperty;
    }

    public bool TryGetEndpointDirection(MemberInfo member, out GraphDirection direction)
    {
        if (IsSameMember(member, InProperty))
        {
            direction = GraphDirection.In;
            return true;
        }

        if (IsSameMember(member, OutProperty))
        {
            direction = GraphDirection.Out;
            return true;
        }

        direction = default;
        return false;
    }

    private static bool IsSameMember(MemberInfo left, MemberInfo right)
    {
        return left.Module == right.Module && left.MetadataToken == right.MetadataToken;
    }
}

internal static class GraphEdgeMetadataResolver
{
    private static readonly ConcurrentDictionary<Type, GraphEdgeMetadata> MetadataCache = new();
    private static readonly ConcurrentDictionary<PropertyInfo, bool> NavigationPropertyCache =
        new();

    public static GraphEdgeMetadata Get(Type edgeType)
    {
        return MetadataCache.GetOrAdd(edgeType, Create);
    }

    public static bool IsNavigationProperty(PropertyInfo property)
    {
        return NavigationPropertyCache.GetOrAdd(
            property,
            static candidate =>
                candidate.GetCustomAttribute<SurrealInAttribute>(inherit: true) is not null
                || candidate.GetCustomAttribute<SurrealOutAttribute>(inherit: true) is not null
        );
    }

    private static GraphEdgeMetadata Create(Type edgeType)
    {
        var properties = edgeType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        var inProperties = properties
            .Where(property =>
                property.GetCustomAttribute<SurrealInAttribute>(inherit: true) is not null
            )
            .ToArray();
        var outProperties = properties
            .Where(property =>
                property.GetCustomAttribute<SurrealOutAttribute>(inherit: true) is not null
            )
            .ToArray();

        var ambiguousProperties = inProperties.Intersect(outProperties).ToArray();
        if (ambiguousProperties.Length > 0)
        {
            throw new NotSupportedException(
                $"Graph endpoint property '{edgeType.Name}.{ambiguousProperties[0].Name}' cannot be marked with both [{nameof(SurrealInAttribute)}] and [{nameof(SurrealOutAttribute)}]."
            );
        }

        if (inProperties.Length != 1 || outProperties.Length != 1)
        {
            throw new NotSupportedException(
                $"Edge type '{edgeType.Name}' must declare exactly one public property marked with [{nameof(SurrealInAttribute)}] and exactly one public property marked with [{nameof(SurrealOutAttribute)}]."
            );
        }

        ValidateEndpointType(edgeType, inProperties[0]);
        ValidateEndpointType(edgeType, outProperties[0]);

        return new GraphEdgeMetadata(inProperties[0], outProperties[0]);
    }

    private static void ValidateEndpointType(Type edgeType, PropertyInfo property)
    {
        if (!typeof(IRecord).IsAssignableFrom(property.PropertyType))
        {
            throw new NotSupportedException(
                $"Graph endpoint property '{edgeType.Name}.{property.Name}' must implement {nameof(IRecord)}."
            );
        }
    }
}
