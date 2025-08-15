using System.Collections.Immutable;
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
        var fields = returnType
            .GetProperties()
            .Where(property => property is { CanRead: true, CanWrite: true })
            .Select(property =>
            {
                var fieldName = ReflectionExtensions.GetDatabaseFieldName(property);
                return property.Name != fieldName
                    ? (fieldName, alias: property.Name)
                    : (fieldName: property.Name, alias: null);
            })
            .OrderBy(x => x.alias ?? x.fieldName)
            .Select(
                (x) =>
                    new SingleFieldExpression(
                        new IdiomValueExpression(
                            new IdiomExpression([new FieldPartExpression(x.fieldName)])
                        ),
                        x.alias is not null
                            ? new IdiomExpression([new FieldPartExpression(x.alias)])
                            : null
                    )
            )
            .ToImmutableArray<FieldExpression>();

        return new FieldsExpression { Fields = fields };
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
