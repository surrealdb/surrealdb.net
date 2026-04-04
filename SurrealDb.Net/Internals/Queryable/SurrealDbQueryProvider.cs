using System.Collections;
using System.Linq.Expressions;
using Dahomey.Cbor;
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

        if (
            TryGetAnonymousEnumerableElementType(typeof(TResult), out _)
            && TryFindSourceQueryable(expression, out var sourceQueryable)
        )
        {
            await engine.EnsureVersionIsSetAsync(cancellationToken).ConfigureAwait(false);

            var (sourceQuery, sourceParameters) = Translate(
                sourceQueryable!.Expression,
                engine.CachedVersion
                    ?? throw new NullReferenceException(
                        "Cannot detect version of the inner SurrealDB engine."
                    ),
                optimizeSelfProjection: true
            );

            var sourceResponse = await engine
                .RawQuery(
                    sourceQuery,
                    sourceParameters,
                    _sessionId,
                    _transactionId,
                    cancellationToken
                )
                .ConfigureAwait(false);
            sourceResponse.EnsureAllOks();

            var sourceItems = sourceResponse.GetValue<IEnumerable<T>>(0)!;

            var inMemoryQueryable = sourceItems.AsQueryable();
            var rewrittenExpression = new RootQueryableReplaceVisitor(
                sourceQueryable,
                inMemoryQueryable
            ).Visit(expression)!;

            return inMemoryQueryable.Provider.Execute<TResult>(rewrittenExpression)!;
        }

        await engine.EnsureVersionIsSetAsync(cancellationToken).ConfigureAwait(false);

        var (query, parameters) = Translate(
            expression,
            engine.CachedVersion
                ?? throw new NullReferenceException(
                    "Cannot detect version of the inner SurrealDB engine."
                ),
            optimizeSelfProjection: false
        );

        {
            var result = await engine
                .RawQuery(query, parameters, _sessionId, _transactionId, cancellationToken)
                .ConfigureAwait(false);
            result.EnsureAllOks();

            try
            {
                return result.GetValue<TResult>(0)!;
            }
            catch (CborException)
            {
                if (TryFlattenNestedEnumerableResult(result, out TResult? flattenedResult))
                {
                    return flattenedResult!;
                }

                throw;
            }
        }
    }

    private static bool TryFlattenNestedEnumerableResult<TResult>(
        global::SurrealDb.Net.Models.Response.SurrealDbResponse response,
        out TResult? flattenedResult
    )
    {
        flattenedResult = default;

        var resultType = typeof(TResult);
        if (
            !resultType.IsGenericType
            || resultType.GetGenericTypeDefinition() != typeof(IEnumerable<>)
        )
        {
            return false;
        }

        var elementType = resultType.GenericTypeArguments[0];
        var nestedEnumerableType = typeof(IEnumerable<>).MakeGenericType(
            typeof(IEnumerable<>).MakeGenericType(elementType)
        );

        object? nestedValue;
        try
        {
            nestedValue = typeof(global::SurrealDb.Net.Models.Response.SurrealDbResponse)
                .GetMethod(
                    nameof(global::SurrealDb.Net.Models.Response.SurrealDbResponse.GetValue)
                )!
                .MakeGenericMethod(nestedEnumerableType)
                .Invoke(response, [0]);
        }
        catch
        {
            return false;
        }

        if (nestedValue is not IEnumerable outer)
        {
            return false;
        }

        var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType))!;

        foreach (var inner in outer)
        {
            if (inner is not IEnumerable innerEnumerable)
            {
                return false;
            }

            foreach (var item in innerEnumerable)
            {
                list.Add(item);
            }
        }

        flattenedResult = (TResult)list;
        return true;
    }

    public (string Query, IReadOnlyDictionary<string, object?> Parameters) Translate(
        Expression expression,
        SemVersion version
    )
    {
        return Translate(expression, version, optimizeSelfProjection: true);
    }

    private (string Query, IReadOnlyDictionary<string, object?> Parameters) Translate(
        Expression expression,
        SemVersion version,
        bool optimizeSelfProjection
    )
    {
        var (intermediateExpression, numberOfNamedValues, sourceExpressionParameters) =
            new ToIntermediateExpressionVisitor().Bind(expression);
        var surrealExpressionResult = new SurrealExpressionVisitor(
            sourceExpressionParameters,
            numberOfNamedValues,
            version,
            optimizeSelfProjection
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

    private static bool TryGetAnonymousEnumerableElementType(Type resultType, out Type? elementType)
    {
        elementType = null;

        if (
            !resultType.IsGenericType
            || resultType.GetGenericTypeDefinition() != typeof(IEnumerable<>)
        )
        {
            return false;
        }

        var candidate = resultType.GetGenericArguments()[0];
        bool isAnonymous =
            Attribute.IsDefined(
                candidate,
                typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute),
                false
            )
            && candidate.Name.Contains("AnonymousType", StringComparison.Ordinal)
            && candidate.IsGenericType;
        if (!isAnonymous)
        {
            return false;
        }

        elementType = candidate;
        return true;
    }

    private static bool TryFindSourceQueryable(
        Expression expression,
        out SurrealDbQueryable<T>? sourceQueryable
    )
    {
        sourceQueryable = expression switch
        {
            ConstantExpression { Value: SurrealDbQueryable<T> surrealQueryable } =>
                surrealQueryable,
            MethodCallExpression methodCallExpression => methodCallExpression
                .Arguments.Select(arg =>
                    TryFindSourceQueryable(arg, out var innerSourceQueryable)
                        ? innerSourceQueryable
                        : null
                )
                .FirstOrDefault(queryable => queryable is not null),
            _ => null,
        };

        return sourceQueryable is not null;
    }

    private sealed class RootQueryableReplaceVisitor(
        SurrealDbQueryable<T> sourceQueryable,
        IQueryable<T> replacementQueryable
    ) : ExpressionVisitor
    {
        private readonly SurrealDbQueryable<T> _sourceQueryable = sourceQueryable;
        private readonly IQueryable<T> _replacementQueryable = replacementQueryable;

        protected override Expression VisitConstant(ConstantExpression node)
        {
            if (ReferenceEquals(node.Value, _sourceQueryable))
            {
                return Expression.Constant(_replacementQueryable, typeof(IQueryable<T>));
            }

            return base.VisitConstant(node);
        }
    }
}
