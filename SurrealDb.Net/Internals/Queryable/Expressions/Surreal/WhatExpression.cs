using System.Collections.Immutable;

namespace SurrealDb.Net.Internals.Queryable.Expressions.Surreal;

internal sealed class WhatExpression : SurrealExpression
{
    public ImmutableArray<ValueExpression> Values { get; private set; }

    private WhatExpression() { }

    public static WhatExpression From(ValueExpression value)
    {
        return new WhatExpression { Values = [value] };
    }
}
