using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations.Schema;
using System.Reflection;
using SurrealDb.Net.Internals.Extensions;

namespace SurrealDb.Net.Internals.Queryable.Expressions.Surreal;

internal sealed class FieldsExpression : SurrealExpression
{
    private readonly bool _single;

    public ImmutableArray<FieldExpression> Fields { get; private init; }

    public bool IsSingleValue => _single && Fields is [SingleFieldExpression];

    public FieldsExpression(ImmutableArray<FieldExpression> fields)
    {
        Fields = fields;
    }

    private FieldsExpression(bool single = false)
    {
        _single = single;
    }

    public static FieldsExpression Single(ValueExpression value, bool single)
    {
        // 💡 Alias not required here
        return new FieldsExpression(single)
        {
            Fields = [new SingleFieldExpression(value, alias: null)],
        };
    }

    public static FieldsExpression ForType(Type returnType)
    {
        var properties = returnType
            .GetProperties()
            .Where(property => property is { CanRead: true, CanWrite: true })
            .ToArray();

        // Keep historical alphabetical order for simple models,
        // but preserve declaration order when deconstruct fields are involved.
        if (!properties.Any(IsDeconstructProperty))
        {
            properties = properties
                .OrderBy(property => ReflectionExtensions.GetDatabaseFieldName(property).Item1)
                .ToArray();
        }

        var fields = properties
            .Select(property =>
            {
                var (fieldName, fromAttribute) = ReflectionExtensions.GetDatabaseFieldName(
                    property
                );

                var propertyType = property.PropertyType;

                ImmutableArray<string> GetDeconstructFields(Type type)
                {
                    return type.GetProperties()
                        .Where(p => p is { CanRead: true, CanWrite: true })
                        .Select(p =>
                        {
                            var (nestedFieldName, _) = ReflectionExtensions.GetDatabaseFieldName(p);
                            return nestedFieldName;
                        })
                        .OrderBy(x => x)
                        .ToImmutableArray();
                }

                // Check if this property is a nested collection/array
                var isCollection =
                    propertyType.IsArray
                    || (
                        propertyType.IsGenericType
                        && typeof(System.Collections.IEnumerable).IsAssignableFrom(propertyType)
                    );

                if (isCollection)
                {
                    // Get the element type from the collection
                    var elementType = propertyType.IsArray
                        ? propertyType.GetElementType()!
                        : propertyType.GetGenericArguments()[0];

                    // Only create deconstruct for SurrealDB entities in collections
                    if (IsSurrealDbEntity(elementType))
                    {
                        // Get the nested fields for deconstruction
                        var nestedFields = GetDeconstructFields(elementType);

                        if (nestedFields.Length > 0)
                        {
                            // Create a deconstruct part expression for nested fields
                            var deconstructPart = new DeconstructPartExpression(
                                fieldName,
                                nestedFields
                            );
                            // No alias needed for deconstruct fields - use the database field name directly
                            return new SingleFieldExpression(
                                new IdiomValueExpression(new IdiomExpression([deconstructPart])),
                                null
                            );
                        }
                    }
                }
                else if (IsSurrealDbEntity(propertyType))
                {
                    // Handle single SurrealDB entity properties (not collections)
                    var nestedFields = GetDeconstructFields(propertyType);

                    if (nestedFields.Length > 0)
                    {
                        // Create a deconstruct part expression for nested fields
                        var deconstructPart = new DeconstructPartExpression(
                            fieldName,
                            nestedFields
                        );
                        // No alias needed for deconstruct fields - use the database field name directly
                        return new SingleFieldExpression(
                            new IdiomValueExpression(new IdiomExpression([deconstructPart])),
                            null
                        );
                    }
                }

                // Default behavior for primitive types or non-SurrealDB types
                if (fromAttribute)
                {
                    return new SingleFieldExpression(
                        new IdiomValueExpression(
                            new IdiomExpression([new FieldPartExpression(fieldName)])
                        ),
                        null
                    );
                }

                var aliasExpr =
                    property.Name != fieldName
                        ? new IdiomExpression([new FieldPartExpression(property.Name)])
                        : null;

                return new SingleFieldExpression(
                    new IdiomValueExpression(
                        new IdiomExpression([new FieldPartExpression(fieldName)])
                    ),
                    aliasExpr
                );
            })
            .ToImmutableArray<FieldExpression>();

        return new FieldsExpression(fields);

        static bool IsSurrealDbEntity(Type type)
        {
            return type.IsClass && type.GetCustomAttribute(typeof(TableAttribute)) != null;
        }

        static bool IsDeconstructProperty(PropertyInfo property)
        {
            var propertyType = property.PropertyType;
            var isCollection =
                propertyType.IsArray
                || (
                    propertyType.IsGenericType
                    && typeof(System.Collections.IEnumerable).IsAssignableFrom(propertyType)
                );

            if (isCollection)
            {
                // Get the element type from the collection
                var elementType = propertyType.IsArray
                    ? propertyType.GetElementType()!
                    : propertyType.GetGenericArguments()[0];

                return IsSurrealDbEntity(elementType);
            }

            return IsSurrealDbEntity(propertyType);
        }
    }

    public static FieldsExpression From(ObjectValueExpression objectValueExpression)
    {
        var fields = objectValueExpression
            .Fields.OrderBy(x => x.Key)
            .Select(x =>
            {
                var (fieldName, valueExpression) = x;

                // 💡 Alias only needed when property does not exist in origin type
                var aliasExpression = new IdiomExpression([new FieldPartExpression(fieldName)]);
                bool isAliasNeeded =
                    valueExpression is not IdiomValueExpression idiomValueExpression
                    || !idiomValueExpression.Idiom.IsSame(aliasExpression);

                return new SingleFieldExpression(
                    valueExpression,
                    isAliasNeeded ? aliasExpression : null
                );
            })
            .ToImmutableArray<FieldExpression>();

        return new FieldsExpression { Fields = fields };
    }
}
