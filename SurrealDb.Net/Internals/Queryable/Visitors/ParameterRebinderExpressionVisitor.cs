using System.Linq.Expressions;

namespace SurrealDb.Net.Internals.Queryable.Visitors;

internal sealed class ParameterRebinderExpressionVisitor : ExpressionVisitor
{
    private readonly Dictionary<ParameterExpression, ParameterExpression> _map;

    public ParameterRebinderExpressionVisitor(
        Dictionary<ParameterExpression, ParameterExpression> map
    )
    {
        _map = map;
    }

    public static Expression Replace(
        Dictionary<ParameterExpression, ParameterExpression> map,
        Expression expression
    )
    {
        return new ParameterRebinderExpressionVisitor(map).Visit(expression);
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return _map.TryGetValue(node, out var replacement)
            ? replacement
            : base.VisitParameter(node);
    }
}
