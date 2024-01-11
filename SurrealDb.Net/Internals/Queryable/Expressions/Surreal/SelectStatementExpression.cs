using System.Collections.Immutable;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Queryable.Expressions.Surreal;

internal sealed class SelectStatementExpression : SurrealExpression
{
    public FieldsExpression Fields { get; }
    public ImmutableArray<IdiomExpression>? Omit { get; } // OMIT is useless in this context
    public bool Only { get; }
    public WhatExpression What { get; }
    public IndexingExpression? With { get; }
    public ConditionsExpression? Cond { get; }
    public SplitsExpression? Splits { get; }
    public GroupingExpression? Group { get; }
    public OrderingExpression? Order { get; }
    public LimitExpression? Limit { get; }
    public StartExpression? Start { get; }
    public FetchsExpression? Fetchs { get; }
    public VersionExpression? Version { get; }
    public Duration? Timeout { get; }
    public bool Parallel { get; }
    public ExplainExpression? Explain { get; }
    public bool Tempfiles { get; }

    public SelectStatementExpression(
        FieldsExpression fields,
        WhatExpression what,
        ConditionsExpression? cond,
        GroupingExpression? group,
        OrderingExpression? order,
        LimitExpression? limit,
        StartExpression? start,
        bool only
    )
    {
        Fields = fields;
        What = what;
        Cond = cond;
        Group = group;
        Order = order;
        Limit = limit;
        Start = start;
        Only = only;
    }
}
