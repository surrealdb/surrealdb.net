using System.Linq.Expressions;

namespace SurrealDb.Net.Internals.Queryable.Visitors;

internal class SubtreeEvaluatorExpressionVisitor : ExpressionVisitor
{
    public HashSet<Expression> Candidates { get; }

    internal SubtreeEvaluatorExpressionVisitor(HashSet<Expression> candidates)
    {
        Candidates = candidates;
    }

    internal Expression Eval(Expression node)
    {
        return Visit(node)!;
    }

    public override Expression? Visit(Expression? node)
    {
        if (node == null)
        {
            return null;
        }

        if (Candidates.Contains(node))
        {
            return Evaluate(node);
        }

        return base.Visit(node);
    }

    private Expression Evaluate(Expression node)
    {
        if (node.NodeType == ExpressionType.Constant)
        {
            return node;
        }

        var lambda = Expression.Lambda(node);
        var fn = lambda.Compile();

        return Expression.Constant(fn.DynamicInvoke(null), node.Type);
    }
}
