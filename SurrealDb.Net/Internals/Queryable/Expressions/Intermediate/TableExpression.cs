using System.Linq.Expressions;

namespace SurrealDb.Net.Internals.Queryable.Expressions.Intermediate;

internal sealed class TableExpression : IntermediateExpression
{
    public string TableName { get; }

    public TableExpression(string tableName)
    {
        TableName = tableName;
    }

    protected override Expression VisitChildren(ExpressionVisitor visitor)
    {
        return this;
    }
}
