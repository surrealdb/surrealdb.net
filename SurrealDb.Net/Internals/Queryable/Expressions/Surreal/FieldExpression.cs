using SurrealDb.Net.Internals.Extensions;

namespace SurrealDb.Net.Internals.Queryable.Expressions.Surreal;

internal abstract class FieldExpression : SurrealExpression;

internal sealed class AllFieldExpression : FieldExpression;

internal sealed class SingleFieldExpression : FieldExpression
{
    public ValueExpression Expression { get; }
    public IdiomExpression? Alias { get; }

    public SingleFieldExpression(ValueExpression expression, IdiomExpression? alias)
    {
        Expression = expression;
        Alias = alias;
    }

    public FieldExpression WithAlias(string? alias)
    {
        var newAlias = string.IsNullOrWhiteSpace(alias) ? null : alias.ToFieldIdiom();
        return new SingleFieldExpression(Expression, newAlias);
    }

    public FieldExpression WithoutAlias()
    {
        return Alias is null ? this : new SingleFieldExpression(Expression, null);
    }
}
