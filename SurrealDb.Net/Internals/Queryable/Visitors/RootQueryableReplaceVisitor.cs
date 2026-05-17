using System.Linq.Expressions;

namespace SurrealDb.Net.Internals.Queryable.Visitors;

internal sealed class RootQueryableReplaceVisitor<T>(
    SurrealDbQueryable<T> sourceQueryable,
    IQueryable<T> replacementQueryable
) : ExpressionVisitor
{
    protected override Expression VisitConstant(ConstantExpression node)
    {
        if (ReferenceEquals(node.Value, sourceQueryable))
        {
            return Expression.Constant(replacementQueryable, typeof(IQueryable<T>));
        }

        return base.VisitConstant(node);
    }
}
