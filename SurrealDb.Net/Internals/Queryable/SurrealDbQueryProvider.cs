using System.Linq.Expressions;
using SurrealDb.Net.Internals.Queryable.Visitors;

namespace SurrealDb.Net.Internals.Queryable;

// TODO : remove generic T?

public sealed class SurrealDbQueryProvider<T> : ISurrealDbQueryProvider, IAsyncQueryProvider
{
    private readonly WeakReference<ISurrealDbEngine> _surrealDbEngine;

    public SurrealDbQueryProvider(ISurrealDbEngine surrealDbEngine)
    {
        _surrealDbEngine = new WeakReference<ISurrealDbEngine>(surrealDbEngine);
    }

    public IQueryable CreateQuery(Expression expression)
    {
        throw new NotSupportedException(
            $"Non-generic method '{nameof(CreateQuery)}' is not supported. Please use the generic version."
        );
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        return new SurrealDbQueryable<TElement>(this, expression);
    }

    public object Execute(Expression expression)
    {
        throw new NotImplementedException();
    }

    public TResult Execute<TResult>(Expression expression)
    {
        return ExecuteAsync<TResult>(expression, CancellationToken.None).GetAwaiter().GetResult();
    }

    public async Task<TResult> ExecuteAsync<TResult>(
        Expression expression,
        CancellationToken cancellationToken
    )
    {
        if (_surrealDbEngine.TryGetTarget(out var engine))
        {
            if (
                expression.NodeType == ExpressionType.Constant
                && expression.Type == typeof(SurrealDbQueryable<T>)
            )
            {
                var constantExpression = (ConstantExpression)expression;
                var surrealQueryable = (SurrealDbQueryable<T>)constantExpression.Value!;

                var result = await engine
                    .SelectAll<T>(surrealQueryable.FromTable, cancellationToken)
                    .ConfigureAwait(false);
                return (TResult)result;
            }

            var (query, parameters) = Translate(expression);

            {
                var result = await engine
                    .RawQuery(query, parameters, cancellationToken)
                    .ConfigureAwait(false);
                result.EnsureAllOks();

                return result.GetValue<TResult>(0)!;
            }
        }

        throw new Exception("SurrealDB instance has been disposed.");
    }

    public (string Query, IReadOnlyDictionary<string, object?> Parameters) Translate(
        Expression expression
    )
    {
        var (intermediateExpression, numberOfNamedValues, sourceExpressionParameters) =
            new ToIntermediateExpressionVisitor().Bind(expression);
        var surrealExpressionResult = new SurrealExpressionVisitor(
            sourceExpressionParameters,
            numberOfNamedValues
        ).Bind(intermediateExpression);
        var surrealExpression = (Expressions.Surreal.SurrealExpression)
            surrealExpressionResult.Expression;
        // TODO : Check and replace "always true" operations
        // TODO : Check and replace SELECT FROM record ID if operation "== RecordId"
        // TODO : Approximate Query size? (based on number of expressions)
        string query = new QueryGeneratorExpressionVisitor().Translate(surrealExpression);

        return (query, surrealExpressionResult.Parameters);
    }
}
