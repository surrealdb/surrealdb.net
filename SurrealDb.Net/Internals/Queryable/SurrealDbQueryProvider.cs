using System.Linq.Expressions;
using Semver;
using SurrealDb.Net.Internals.Queryable.Visitors;

namespace SurrealDb.Net.Internals.Queryable;

public sealed class SurrealDbQueryProvider<T> : ISurrealDbQueryProvider, IAsyncQueryProvider
{
    private readonly Guid? _sessionId;
    private readonly Guid? _transactionId;
    private readonly WeakReference<ISurrealDbEngine> _surrealDbEngine;

    public SurrealDbQueryProvider(
        ISurrealDbEngine surrealDbEngine,
        Guid? sessionId,
        Guid? transactionId
    )
    {
        _surrealDbEngine = new WeakReference<ISurrealDbEngine>(surrealDbEngine);
        _sessionId = sessionId;
        _transactionId = transactionId;
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
        if (!_surrealDbEngine.TryGetTarget(out var engine))
        {
            throw new Exception("SurrealDB instance has been disposed.");
        }

        if (
            expression.NodeType == ExpressionType.Constant
            && expression.Type == typeof(SurrealDbQueryable<T>)
        )
        {
            var constantExpression = (ConstantExpression)expression;
            var surrealQueryable = (SurrealDbQueryable<T>)constantExpression.Value!;

            var result = await engine
                .SelectAll<T>(
                    surrealQueryable.FromTable,
                    _sessionId,
                    _transactionId,
                    cancellationToken
                )
                .ConfigureAwait(false);
            return (TResult)result;
        }

        await engine.EnsureVersionIsSetAsync(cancellationToken).ConfigureAwait(false);

        var (query, parameters) = Translate(
            expression,
            engine.CachedVersion
                ?? throw new NullReferenceException(
                    "Cannot detect version of the inner SurrealDB engine."
                )
        );

        {
            var result = await engine
                .RawQuery(query, parameters, _sessionId, _transactionId, cancellationToken)
                .ConfigureAwait(false);
            result.EnsureAllOks();

            return result.GetValue<TResult>(0)!;
        }
    }

    public (string Query, IReadOnlyDictionary<string, object?> Parameters) Translate(
        Expression expression,
        SemVersion version
    )
    {
        var (intermediateExpression, numberOfNamedValues, sourceExpressionParameters) =
            new ToIntermediateExpressionVisitor().Bind(expression);
        var surrealExpressionResult = new SurrealExpressionVisitor(
            sourceExpressionParameters,
            numberOfNamedValues,
            version
        ).Bind(intermediateExpression);
        var surrealExpression = (Expressions.Surreal.SurrealExpression)
            surrealExpressionResult.Expression;
        // TODO : Check and replace "always true" operations
        // TODO : Check and replace SELECT FROM record ID if operation "== RecordId"
        int approximatedQueryLength = new ApproximateQueryLengthExpressionVisitor().Approximate(
            surrealExpression
        );
        string query = new QueryGeneratorExpressionVisitor().Translate(
            surrealExpression,
            approximatedQueryLength
        );

        return (query, surrealExpressionResult.Parameters);
    }
}
