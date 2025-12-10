using System.Linq.Expressions;

namespace SurrealDb.Net.Internals.Queryable.Visitors;

internal sealed class MemberAccessRebinderExpressionVisitor : ExpressionVisitor
{
    private readonly Dictionary<MemberExpression, Expression> _map;

    public MemberAccessRebinderExpressionVisitor(Dictionary<MemberExpression, Expression> map)
    {
        _map = map;
    }

    public static Expression Replace(
        Dictionary<MemberExpression, Expression> map,
        Expression expression
    )
    {
        return new MemberAccessRebinderExpressionVisitor(map).Visit(expression);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        var match = _map.FirstOrDefault(kv =>
            kv.Key.Expression == node.Expression && kv.Key.Member == node.Member
        );

        return match.Value ?? base.VisitMember(node);
    }
}
