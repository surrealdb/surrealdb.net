using System.Linq.Expressions;
using SurrealDb.Net.Internals.Queryable.Visitors;

namespace SurrealDb.Net.Internals.Queryable;

internal static class Evaluator
{
    private static Expression PartialEval(
        Expression expression,
        Func<Expression, bool> fnCanBeEvaluated
    )
    {
        return new SubtreeEvaluatorExpressionVisitor(
            new NominatorExpressionVisitor(fnCanBeEvaluated).Nominate(expression)
        ).Eval(expression);
    }

    public static Expression PartialEval(Expression expression)
    {
        return PartialEval(expression, CanBeEvaluatedLocally);
    }

    private static bool CanBeEvaluatedLocally(Expression expression)
    {
        return expression.NodeType != ExpressionType.Parameter;
    }
}
