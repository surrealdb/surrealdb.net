using System.Linq.Expressions;

namespace SurrealDb.Net.Internals.Queryable.Visitors;

internal class NominatorExpressionVisitor : ExpressionVisitor
{
    public Func<Expression, bool> FnCanBeEvaluated { get; }
    public HashSet<Expression>? Candidates { get; private set; }
    public bool CannotBeEvaluated { get; private set; }

    internal NominatorExpressionVisitor(Func<Expression, bool> fnCanBeEvaluated)
    {
        FnCanBeEvaluated = fnCanBeEvaluated;
    }

    internal HashSet<Expression> Nominate(Expression expression)
    {
        Candidates = [];
        Visit(expression);

        return Candidates;
    }

    public override Expression? Visit(Expression? expression)
    {
        if (expression != null)
        {
            bool saveCannotBeEvaluated = CannotBeEvaluated;
            CannotBeEvaluated = false;

            base.Visit(expression);

            if (!CannotBeEvaluated)
            {
                if (FnCanBeEvaluated(expression))
                {
                    Candidates?.Add(expression);
                }
                else
                {
                    CannotBeEvaluated = true;
                }
            }

            CannotBeEvaluated |= saveCannotBeEvaluated;
        }

        return expression;
    }
}
