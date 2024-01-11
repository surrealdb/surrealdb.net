using System.Linq.Expressions;

namespace SurrealDb.Net.Internals.Queryable.Expressions.Intermediate;

// internal abstract class TableExpression : IntermediateExpression;

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
//
// internal sealed class UnknownTableExpression : TableExpression;
