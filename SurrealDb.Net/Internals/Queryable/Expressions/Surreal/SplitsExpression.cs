using System.Collections.Immutable;

namespace SurrealDb.Net.Internals.Queryable.Expressions.Surreal;

internal sealed class SplitsExpression : SurrealExpression
{
    public ImmutableArray<IdiomExpression> Fields { get; }

    private SplitsExpression() { }
}
