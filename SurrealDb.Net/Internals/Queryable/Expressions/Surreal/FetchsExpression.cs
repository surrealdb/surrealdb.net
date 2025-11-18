using System.Collections.Immutable;

namespace SurrealDb.Net.Internals.Queryable.Expressions.Surreal;

internal sealed class FetchsExpression : SurrealExpression
{
    public ImmutableArray<ValueExpression> Fields { get; }

    private FetchsExpression() { }
}
