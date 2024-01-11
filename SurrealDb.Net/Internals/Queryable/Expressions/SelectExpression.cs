using System.Collections.ObjectModel;
using System.Linq.Expressions;

namespace SurrealDb.Net.Internals.Queryable.Expressions;

internal class SelectExpression : SurrealExpression
{
    public string Alias { get; }
    public bool AllFields { get; }
    public ReadOnlyCollection<ColumnDeclaration> Columns { get; }
    public Expression From { get; }
    public Expression? Where { get; }
    public ReadOnlyCollection<Expression> GroupBy { get; }
    public ReadOnlyCollection<OrderExpression> OrderBy { get; }
    public Expression? Limit { get; }
    public Expression? Start { get; }

    internal SelectExpression(
        Type type,
        string alias,
        Expression from,
        Expression? where,
        IEnumerable<Expression> groupBy,
        IEnumerable<OrderExpression> orderBy,
        Expression? limit,
        Expression? start
    )
        : base(type, SurrealExpressionType.Select)
    {
        Alias = alias;
        AllFields = true;
        Columns = new List<ColumnDeclaration>(0).AsReadOnly(); // TODO : ?
        From = from;
        Where = where;
        GroupBy = groupBy as ReadOnlyCollection<Expression> ?? groupBy.ToList().AsReadOnly();
        OrderBy = orderBy as ReadOnlyCollection<OrderExpression> ?? orderBy.ToList().AsReadOnly();
        Limit = limit;
        Start = start;
    }

    internal SelectExpression(
        Type type,
        string alias,
        IEnumerable<ColumnDeclaration> columns,
        Expression from,
        Expression? where,
        IEnumerable<Expression> groupBy,
        IEnumerable<OrderExpression> orderBy,
        Expression? limit,
        Expression? start
    )
        : base(type, SurrealExpressionType.Select)
    {
        Alias = alias;
        AllFields = false;
        Columns = columns as ReadOnlyCollection<ColumnDeclaration> ?? columns.ToList().AsReadOnly();
        From = from;
        Where = where;
        GroupBy = groupBy as ReadOnlyCollection<Expression> ?? groupBy.ToList().AsReadOnly();
        OrderBy = orderBy as ReadOnlyCollection<OrderExpression> ?? orderBy.ToList().AsReadOnly();
        Limit = limit;
        Start = start;
    }
}
