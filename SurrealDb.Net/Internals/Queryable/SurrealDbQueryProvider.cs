using System.Collections.Immutable;
using System.Linq.Expressions;
using SurrealDb.Net.Internals.Queryable.Visitors;
using SelectExpression = SurrealDb.Net.Internals.Queryable.Expressions.SelectExpression;

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

        // TODO : Find a way to avoid Reflection
        // ------------------------------------------------
        //var elementType = expression.Type.GetGenericArguments().First();
        //return (IQueryable)
        //    Activator.CreateInstance(
        //        typeof(SurrealDbQueryable<>).MakeGenericType(elementType),
        //        this,
        //        expression
        //    )!;
        // ------------------------------------------------

        //Type elementType = TypeSystem.GetElementType(expression.Type);
        //try
        //{
        //    return
        //       (IQueryable)Activator.CreateInstance(typeof(Queryable<>).
        //              MakeGenericType(elementType), new object[] { this, expression });
        //}
        //catch (TargetInvocationException e)
        //{
        //    throw e.InnerException;
        //}
    }

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
    {
        //return new Queryable<TElement>(this, expression);
        return new SurrealDbQueryable<TElement>(this, expression);
    }

    public object Execute(Expression expression)
    {
        //return _surrealDbEngine.TryGetTarget(out var engine)
        //    ? engine.SelectAll<T>(_fromTable, default).Result.GetEnumerator()
        //    : Enumerable.Empty<T>().GetEnumerator();

        //if (_surrealDbEngine.TryGetTarget(out var engine))
        //{
        //    return engine.SelectAll<T>(_fromTable, default).Result.GetEnumerator();
        //}

        //throw new Exception("SurrealDB instance has been disposed.");

        throw new NotImplementedException();
    }

    public TResult Execute<TResult>(Expression expression)
    {
        return ExecuteAsync<TResult>(expression, CancellationToken.None).GetAwaiter().GetResult();

        // if (_surrealDbEngine.TryGetTarget(out var engine))
        // {
        //     if (
        //         expression.NodeType == ExpressionType.Constant
        //         && expression.Type == typeof(SurrealDbQueryable<T>)
        //     )
        //     {
        //         string from = string.IsNullOrWhiteSpace(FromTable)
        //             ? expression.Type.GenericTypeArguments[0].Name
        //             : FromTable;
        //
        //         //var selectTask = engine.SelectAll<T>(from, default);
        //         //
        //         //return Task.Run(() => taskCompletionSource.Task).GetAwaiter().GetResult();
        //
        //         // var result = Task.Run(() => engine.SelectAll<T>(from, synchronous: true, default))
        //         //     .GetAwaiter()
        //         //     .GetResult();
        //         // When ready:
        //         var result = engine
        //             .SelectAll<T>(from, synchronous: true, default)
        //             //.ConfigureAwait(false)
        //             .GetAwaiter()
        //             .GetResult();
        //         // var result = Task.Run(async () =>
        //         // {
        //         //     return await engine
        //         //         .SelectAll<T>(from, synchronous: true, default)
        //         //         .ConfigureAwait(false);
        //         // }).Result;
        //         // var cesp = new ConcurrentExclusiveSchedulerPair();
        //         // var result = Task
        //         //     .Factory.StartNew(
        //         //         async () =>
        //         //         {
        //         //             return await engine
        //         //                 .SelectAll<T>(from, synchronous: false, default)
        //         //                 .ConfigureAwait(false);
        //         //         },
        //         //         default,
        //         //         TaskCreationOptions.None,
        //         //         cesp.ExclusiveScheduler
        //         //     )
        //         //     .GetAwaiter()
        //         //     .GetResult()
        //         //     .GetAwaiter()
        //         //     .GetResult();
        //         // .GetAwaiter()
        //         // .GetResult();
        //         return (TResult)result;
        //
        //         // return (TResult)engine.SelectAll<T>(from, default).Result;
        //         // return (TResult)engine.SelectAll<T>(FromTable, default).Result.GetEnumerator();
        //     }
        //
        //     //var query = "SELECT * FROM " + _fromTable;
        //     //var surrealExpression = new SurrealExpressionVisitor().Visit(expression);
        //     //string query = new QueryGeneratorExpressionVisitor().Translate(expression, _fromTable);
        //
        //     {
        //         string query = Translate(expression, FromTable!);
        //         var result = engine
        //             .RawQuery(query, ImmutableDictionary<string, object?>.Empty, default)
        //             .GetAwaiter()
        //             .GetResult();
        //
        //         return result.GetValue<TResult>(0)!;
        //     }
        // }
        //
        // throw new Exception("SurrealDB instance has been disposed.");

        // TODO : check if TResult is IEnumerable
        //return _surrealDbEngine.TryGetTarget(out var engine)
        //    ? (TResult)engine.SelectAll<T>(_fromTable, default).Result.GetEnumerator()
        //    : (TResult)Enumerable.Empty<T>().GetEnumerator();
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

                // string from = string.IsNullOrWhiteSpace(surrealQueryable.FromTable)
                //     ? surrealQueryable.ElementType.Name //.GenericTypeArguments[0].Name
                //     : surrealQueryable.FromTable;

                var result = await engine
                    .SelectAll<T>(surrealQueryable.FromTable, cancellationToken)
                    .ConfigureAwait(false);
                return (TResult)result;
                //return (TResult)result.GetEnumerator();
            }

            //var query = "SELECT * FROM " + _fromTable;
            //var surrealExpression = new SurrealExpressionVisitor().Visit(expression);
            //expression = Evaluator.PartialEval(expression);
            //var projection = (ProjectionExpression)
            //    new QueryBinderExpressionVisitor().Bind(expression);
            //string query = new QueryGeneratorExpressionVisitor().Translate(
            //    projection.Source,
            //    _fromTable
            //);
            var (query, parameters) = Translate(expression);

            {
                var result = await engine
                    .RawQuery(query, parameters, cancellationToken)
                    .ConfigureAwait(false);
                result.EnsureAllOks();

                return result.GetValue<TResult>(0)!;
            }
        }

        //if (_surrealDbEngine.TryGetTarget(out var engine))
        //{
        //    if (
        //        expression.NodeType == ExpressionType.Constant
        //        && expression.Type == typeof(SurrealDbQueryable<T>)
        //    )
        //    {
        //        return (TResult).Result.GetEnumerator();
        //    }

        //    var query = "SELECT * FROM " + _fromTable;
        //    var result = engine
        //        .Query(query, ImmutableDictionary<string, object>.Empty, default)
        //        .Result;

        //    return result.GetValue<TResult>(0)!;
        //}

        throw new Exception("SurrealDB instance has been disposed.");
    }

    public (string Query, IReadOnlyDictionary<string, object?> Parameters) Translate(
        Expression expression
    )
    // internal static (string Query, IReadOnlyDictionary<string, object?> Parameters) Translate(
    //     Expression expression
    // )
    {
        //var evaluatedExpression = Evaluator.PartialEval(expression);
        //var selectExpression = (SelectExpression)
        //    new QueryBinderExpressionVisitor().Bind(evaluatedExpression);

        const bool useNewExpressionVisitor = true;

        if (useNewExpressionVisitor)
#pragma warning disable CS0162 // Unreachable code detected
        {
            var (intermediateExpression, numberOfNamedValues, sourceExpressionParameters) =
                //(SurrealDb.Net.Internals.Queryable.Expressions.Intermediate.IntermediateExpression)
                new ToIntermediateExpressionVisitor().Bind(expression);
            var surrealExpressionResult = new Surreal2ExpressionVisitor(
                sourceExpressionParameters,
                numberOfNamedValues
            ).Bind(intermediateExpression);
            var surrealExpression =
                (SurrealDb.Net.Internals.Queryable.Expressions.Surreal.SurrealExpression)
                    surrealExpressionResult.Expression;
            // TODO : Check and replace "always true" operations
            // TODO : Check and replace SELECT FROM record ID if operation "== RecordId"
            // TODO : Approximate Query size? (based on number of expressions)
            string query = new QueryGenerator2ExpressionVisitor().Translate(surrealExpression);

            return (query, surrealExpressionResult.Parameters);
        }
#pragma warning restore CS0162 // Unreachable code detected

#pragma warning disable CS0162 // Unreachable code detected
        {
            var selectExpression = (SelectExpression)
                new QueryBinderExpressionVisitor().Bind(expression);
            string query = new QueryGeneratorExpressionVisitor().Translate(selectExpression);
            return (query, ImmutableDictionary<string, object?>.Empty);
        }
#pragma warning restore CS0162 // Unreachable code detected
    }
}
