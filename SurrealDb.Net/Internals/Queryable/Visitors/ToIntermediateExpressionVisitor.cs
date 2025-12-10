using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Dahomey.Cbor.Util;
using SurrealDb.Net.Internals.Extensions;
using SurrealDb.Net.Internals.Queryable.Expressions.Intermediate;
using SurrealDb.Net.Internals.Queryable.Expressions.Intermediate.Projectors;

namespace SurrealDb.Net.Internals.Queryable.Visitors;

internal sealed class ToIntermediateExpressionVisitor : ExpressionVisitor
{
    private readonly Dictionary<ParameterExpression, Expression> _sourceExpressionParameters = [];
    private int _numberOfNamedValues = 0;

    internal (
        Expression Expression,
        int NumberOfNamedValues,
        Dictionary<ParameterExpression, Expression> SourceExpressionParameters
    ) Bind(Expression expression)
    {
        var output = Visit(expression);
        return (output, _numberOfNamedValues, _sourceExpressionParameters);
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        if (node.Value is ISurrealDbQueryable surrealQueryable)
        {
            return MapSurrealDbQueryable(surrealQueryable);
        }

        return base.VisitConstant(node);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        // TODO
        if (
            node is
            { Expression: ConstantExpression constantExpression, Member.DeclaringType: not null }
        )
        {
            if (
                node.Member is PropertyInfo propertyInfo
                && propertyInfo.GetValue(constantExpression.Value)
                    is ISurrealDbQueryable surrealQueryable
            )
            {
                return MapSurrealDbQueryable(surrealQueryable);
            }

            if (IsExternalParameter(node.Member.DeclaringType))
            {
                object? value = constantExpression
                    .Value?.GetType()
                    .GetField(node.Member.Name)
                    ?.GetValue(constantExpression.Value);

                _numberOfNamedValues++;
                return new NamedValueExpression(node.Type, node.Member.Name, value);
            }
        }

        var innerExpression = Visit(node.Expression);
        if (innerExpression != node.Expression)
        {
            return Expression.MakeMemberAccess(innerExpression, node.Member);
        }

        return node;
    }

    private static bool IsExternalParameter(Type type)
    {
        return type.IsSealed
            && type.BaseType == typeof(object)
            && !type.IsPublic
            && type.IsDefined(typeof(CompilerGeneratedAttribute), inherit: false);
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return node;
    }

    protected override Expression VisitLambda<T>(Expression<T> node)
    {
        var returnedExpression = base.VisitLambda(node);
        return node == returnedExpression
            ? new LambdaIntermediateExpression(node)
            : returnedExpression;
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        // TODO
        if (node.Method.DeclaringType == typeof(System.Linq.Queryable))
        {
            return BindQueryableMethodCall(node);
        }

        return base.VisitMethodCall(node);
    }

    private IntermediateExpression BindQueryableMethodCall(MethodCallExpression node)
    {
        if (string.Equals(node.Method.Name, nameof(Enumerable.Select), StringComparison.Ordinal))
        {
            return BindSelect(
                node.Type,
                node.Arguments[0],
                (LambdaExpression)StripQuotes(node.Arguments[1])
            );
        }
        if (
            string.Equals(node.Method.Name, nameof(Enumerable.SelectMany), StringComparison.Ordinal)
            && node.Arguments.Count == 2
        )
        {
            return BindSelectMany(
                node.Type,
                node.Arguments[0],
                (LambdaExpression)StripQuotes(node.Arguments[1]),
                null
            );
        }
        if (string.Equals(node.Method.Name, nameof(Enumerable.Where), StringComparison.Ordinal))
        {
            return BindWhere(
                node.Type,
                node.Arguments[0],
                (LambdaExpression)StripQuotes(node.Arguments[1])
            );
        }
        if (
            string.Equals(node.Method.Name, nameof(Enumerable.GroupBy), StringComparison.Ordinal)
            && node.Arguments.Count == 2
        )
        {
            return BindGroup(
                node.Type,
                node.Arguments[0],
                (LambdaExpression)StripQuotes(node.Arguments[1])
            );
        }
        if (string.Equals(node.Method.Name, nameof(Enumerable.OrderBy), StringComparison.Ordinal))
        {
            return BindOrder(
                node.Arguments[0],
                (LambdaExpression)StripQuotes(node.Arguments[1]),
                OrderType.Ascending
            );
        }
        if (
            string.Equals(
                node.Method.Name,
                nameof(Enumerable.OrderByDescending),
                StringComparison.Ordinal
            )
        )
        {
            return BindOrder(
                node.Arguments[0],
                (LambdaExpression)StripQuotes(node.Arguments[1]),
                OrderType.Descending
            );
        }
        if (string.Equals(node.Method.Name, nameof(Enumerable.ThenBy), StringComparison.Ordinal))
        {
            return BindOrder(
                node.Arguments[0],
                (LambdaExpression)StripQuotes(node.Arguments[1]),
                OrderType.Ascending,
                chainable: true
            );
        }
        if (
            string.Equals(
                node.Method.Name,
                nameof(Enumerable.ThenByDescending),
                StringComparison.Ordinal
            )
        )
        {
            return BindOrder(
                node.Arguments[0],
                (LambdaExpression)StripQuotes(node.Arguments[1]),
                OrderType.Descending,
                chainable: true
            );
        }
        if (string.Equals(node.Method.Name, nameof(Enumerable.Take), StringComparison.Ordinal))
        {
            return BindTake(node.Arguments[0], node.Arguments[1]);
        }
        if (string.Equals(node.Method.Name, nameof(Enumerable.Skip), StringComparison.Ordinal))
        {
            return BindSkip(node.Arguments[0], node.Arguments[1]);
        }
        // Aggregations
        if (string.Equals(node.Method.Name, nameof(Enumerable.Count), StringComparison.Ordinal))
        {
            return BindCount<int>(
                node.Type,
                node.Arguments[0],
                node.Arguments.Count >= 2 ? (LambdaExpression)StripQuotes(node.Arguments[1]) : null
            );
        }
        if (string.Equals(node.Method.Name, nameof(Enumerable.LongCount), StringComparison.Ordinal))
        {
            return BindCount<long>(
                node.Type,
                node.Arguments[0],
                node.Arguments.Count >= 2 ? (LambdaExpression)StripQuotes(node.Arguments[1]) : null
            );
        }
        if (
            string.Equals(node.Method.Name, nameof(Enumerable.Sum), StringComparison.Ordinal)
            || string.Equals(node.Method.Name, nameof(Enumerable.Min), StringComparison.Ordinal)
            || string.Equals(node.Method.Name, nameof(Enumerable.Max), StringComparison.Ordinal)
            || string.Equals(node.Method.Name, nameof(Enumerable.Average), StringComparison.Ordinal)
        )
        {
            return BindAggregation(
                node.Method.Name,
                node.Type,
                node.Arguments[0],
                node.Arguments.Count >= 2 ? (LambdaExpression)StripQuotes(node.Arguments[1]) : null
            );
        }
        // Custom conditions
        if (string.Equals(node.Method.Name, nameof(Enumerable.All), StringComparison.Ordinal))
        {
            return BindAll(
                node.Type,
                node.Arguments[0],
                (LambdaExpression)StripQuotes(node.Arguments[1])
            );
        }
        if (string.Equals(node.Method.Name, nameof(Enumerable.Any), StringComparison.Ordinal))
        {
            return BindAny(
                node.Type,
                node.Arguments[0],
                node.Arguments.Count >= 2 ? (LambdaExpression)StripQuotes(node.Arguments[1]) : null
            );
        }
        if (
            string.Equals(node.Method.Name, nameof(Enumerable.Contains), StringComparison.Ordinal)
            && node.Arguments.Count == 2
        )
        {
            return BindContains(node.Type, node.Arguments[0], node.Arguments[1]);
        }
        if (
            string.Equals(node.Method.Name, nameof(Enumerable.Distinct), StringComparison.Ordinal)
            && node.Arguments.Count == 1
        )
        {
            return BindDistinct(node.Type, node.Arguments[0]);
        }
        if (
            string.Equals(node.Method.Name, nameof(Enumerable.ElementAt), StringComparison.Ordinal)
            || string.Equals(
                node.Method.Name,
                nameof(Enumerable.ElementAtOrDefault),
                StringComparison.Ordinal
            )
        )
        {
            bool shouldReturnDefault = node.Method.Name.EndsWith(
                "OrDefault",
                StringComparison.Ordinal
            );
            return BindElementAt(
                node.Type,
                node.Arguments[0],
                node.Arguments[1],
                shouldReturnDefault
            );
        }
        if (
            string.Equals(node.Method.Name, nameof(Enumerable.First), StringComparison.Ordinal)
            || string.Equals(
                node.Method.Name,
                nameof(Enumerable.FirstOrDefault),
                StringComparison.Ordinal
            )
        )
        {
            bool shouldReturnDefault = node.Method.Name.EndsWith(
                "OrDefault",
                StringComparison.Ordinal
            );
            return BindFirst(
                node.Type,
                node.Arguments[0],
                node.Arguments.Count >= 2 ? (LambdaExpression)StripQuotes(node.Arguments[1]) : null,
                shouldReturnDefault
            );
        }
        if (
            string.Equals(node.Method.Name, nameof(Enumerable.Last), StringComparison.Ordinal)
            || string.Equals(
                node.Method.Name,
                nameof(Enumerable.LastOrDefault),
                StringComparison.Ordinal
            )
        )
        {
            bool shouldReturnDefault = node.Method.Name.EndsWith(
                "OrDefault",
                StringComparison.Ordinal
            );
            return BindLast(
                node.Type,
                node.Arguments[0],
                node.Arguments.Count >= 2 ? (LambdaExpression)StripQuotes(node.Arguments[1]) : null,
                shouldReturnDefault
            );
        }
        if (string.Equals(node.Method.Name, nameof(Enumerable.Single), StringComparison.Ordinal))
        {
            return BindSingle(
                node.Type,
                node.Arguments[0],
                node.Arguments.Count >= 2 ? (LambdaExpression)StripQuotes(node.Arguments[1]) : null
            );
        }

        throw new NotSupportedException($"The method '{node.Method.Name}' is not supported.");
    }

    private SelectExpression BindSelect(
        Type resultType,
        Expression source,
        LambdaExpression selector
    )
    {
        var sourceSelect = (SelectExpression)Visit(source);

        if (sourceSelect.Type.GetGenericTypeDefinition() == typeof(SurrealGrouping<,>))
        {
            // 💡 Ensures correct mapping coming from a "GroupBy" Linq expression
            var keyPropertyInfo = selector
                .Parameters[0]
                .Type.GetProperty(nameof(SurrealGrouping<string, string>.Key))!;
            var g = Expression.MakeMemberAccess(selector.Parameters[0], keyPropertyInfo);
            var map = new Dictionary<MemberExpression, Expression>
            {
                { g, ((SelectSourceExpression)sourceSelect.Source).Select.Projection },
            };
            var selectorExpression = MemberAccessRebinderExpressionVisitor.Replace(
                map,
                selector.Body
            );

            var innerExpression = Visit(selectorExpression);
            ProjectionExpression? projection = (
                innerExpression as ProjectionExpression
            )?.TrySingle();

            if (projection is null)
            {
                var innerProjectionType = typeof(ValueProjector<>).MakeGenericType(resultType);
                var valuePropertyInfo = innerProjectionType.GetProperty(
                    nameof(ValueProjector<int>.Value)
                )!;

                // 💡 Merge into source select
                var innerProjection = new FieldsProjectionExpression(
                    innerProjectionType,
                    [
                        new FieldProjectionExpression(
                            resultType,
                            innerExpression,
                            nameof(ValueProjector<int>.Value)
                        ),
                    ]
                );
                sourceSelect = sourceSelect.WithSource(
                    sourceSelect.Source.MergeProjections(innerProjection)
                );

                var projectionExpression = Expression.MakeMemberAccess(
                    Expression.New(innerProjectionType),
                    valuePropertyInfo
                );
                projection = ExpressionProjectionExpression.From(resultType, projectionExpression);
            }

            _sourceExpressionParameters.Add(selector.Parameters[0], projection);

            return sourceSelect.WithProjection(projection);
        }
        else
        {
            var innerExpression = Visit(selector.Body);
            var projection = ExpressionProjectionExpression.From(resultType, innerExpression);

            _sourceExpressionParameters.Add(selector.Parameters[0], projection);

            return sourceSelect.WithProjection(projection);
        }
    }

    private CustomExpression BindSelectMany(
        Type resultType,
        Expression source,
        LambdaExpression collectionSelector,
        LambdaExpression? resultSelector
    )
    {
        var sourceSelect = (SelectExpression)Visit(source);

        var genericSubqueryType = typeof(FlattenProjector<>).MakeGenericType(
            resultType.GenericTypeArguments[0]
        );
        var arraySubqueryType = genericSubqueryType.MakeArrayType();

        const string memberPropertyName = nameof(FlattenProjector<object>.Values);

        var collectionExpression = Visit(collectionSelector.Body);

        ProjectionExpression innerProjection = new AggregationFieldProjectionExpression(
            genericSubqueryType,
            AggregationType.Flatten,
            collectionExpression is SelectExpression innerSelectExpression
                ? new SubqueryExpression(innerSelectExpression)
                : collectionExpression,
            alias: memberPropertyName
        );
        _sourceExpressionParameters.Add(collectionSelector.Parameters[0], innerProjection);

        var innerQuery = sourceSelect.WithProjection(innerProjection).WithGroupAll();

        // 💡 We simulate the type so it can be interpreted later as an "Array"
        return CreateTopLevelExpression(
            resultType,
            genericSubqueryType,
            memberPropertyName,
            innerQuery,
            arraySubqueryType
        );
    }

    private SelectExpression BindWhere(
        Type resultType,
        Expression source,
        LambdaExpression predicate
    )
    {
        var sourceSelect = (SelectExpression)Visit(source);

        _sourceExpressionParameters.Add(predicate.Parameters[0], sourceSelect.Projection);

        var whereExpression = Visit(predicate.Body);

        return sourceSelect.AppendWhere(whereExpression);
    }

    private SelectExpression BindGroup(
        Type resultType,
        Expression source,
        LambdaExpression keySelector
    )
    {
        var sourceSelect = (SelectExpression)Visit(source);

        var innerParameter = keySelector.Parameters[0];

        var keyExpression = Visit(keySelector.Body);
        var keyProjection = ExpressionProjectionExpression.From(resultType, keyExpression);

        // 💡 Remap lambda parameter in order to bind one with the new "inner" select(or)
        var parentParameter = Expression.Parameter(innerParameter.Type, innerParameter.Name);
        var map = new Dictionary<ParameterExpression, ParameterExpression>
        {
            { innerParameter, parentParameter },
        };
        var reboundBodyExpression = ParameterRebinderExpressionVisitor.Replace(
            map,
            keySelector.Body
        );

        // 💡 Create the subquery to retrieve the "Values" part of a Grouping<K,E>
        var innerSubqueryExpression = sourceSelect.AppendWhere(
            Expression.Equal(keyExpression, reboundBodyExpression)
        );
        var innerSubqueryProjection = innerSubqueryExpression.Projection;

        _sourceExpressionParameters.Add(innerParameter, innerSubqueryProjection);

        var subqueryExpression = new SubqueryExpression(innerSubqueryExpression);

        var resultExpression = Expression.New(
            typeof(SurrealGrouping<,>)
                .MakeGenericType(keyExpression.Type, innerSubqueryProjection.Type)
                .GetConstructors()[0],
            [keyExpression, subqueryExpression]
        );

        // 💡 Apply GROUP BY expression and remove "Single" on the projection
        var innerSourceSelect = sourceSelect
            .WithGroup(ExpressionProjectionExpression.From(resultType, keyExpression).Unsingle())
            .WithProjection(keyProjection.Unsingle());

        // 💡 Create a projection for the "Key" and "Values" part of a Grouping<K,E>
        var projection = new FieldsProjectionExpression(
            resultExpression.Type,
            [
                new FieldProjectionExpression(
                    keyProjection.Type,
                    keyProjection,
                    nameof(SurrealGrouping<string, string>.Key)
                ),
                new FieldProjectionExpression(
                    innerSubqueryProjection.Type,
                    subqueryExpression,
                    nameof(SurrealGrouping<string, string>.Values)
                ),
            ]
        );

        _sourceExpressionParameters.Add(parentParameter, projection);

        return new SelectExpression(resultExpression.Type, innerSourceSelect, projection);
    }

    private SelectExpression BindOrder(
        Expression source,
        LambdaExpression selector,
        OrderType orderType,
        bool chainable = false
    )
    {
        var sourceSelect = (SelectExpression)Visit(source);

        _sourceExpressionParameters.Add(selector.Parameters[0], sourceSelect.Projection);

        var innerExpression = Visit(selector.Body);

        return chainable
            ? sourceSelect.AppendOrder(new OrderByInfo(orderType, innerExpression))
            : sourceSelect.WithOrder(new OrderByInfo(orderType, innerExpression));
    }

    private SelectExpression BindTake(Expression source, Expression value)
    {
        var sourceSelect = (SelectExpression)Visit(source);
        return sourceSelect.WithTake(value);
    }

    private SelectExpression BindSkip(Expression source, Expression value)
    {
        var sourceSelect = (SelectExpression)Visit(source);
        return sourceSelect.WithSkip(value);
    }

    private CustomExpression BindCount<T>(
        Type resultType,
        Expression source,
        LambdaExpression? predicate
    )
        where T : struct
    {
        var sourceSelect = (SelectExpression)Visit(source);

        var innerQuery = sourceSelect
            .WithProjection(
                new CountFieldProjectionExpression(typeof(CountProjector<T>), predicate)
            )
            .WithGroupAll();

        // 💡 We simulate the type so it can be interpreted later as an "Array"
        return CreateTopLevelExpression(
            resultType,
            typeof(CountProjector<T>),
            nameof(CountProjector<T>.count),
            innerQuery,
            typeof(CountProjector<T>[])
        );
    }

    private CustomExpression BindAggregation(
        string methodName,
        Type resultType,
        Expression source,
        LambdaExpression? selector
    )
    {
        var sourceSelect = (SelectExpression)Visit(source);

        var originSubqueryType = methodName switch
        {
            nameof(Enumerable.Sum) => typeof(SumProjector<>),
            nameof(Enumerable.Min) => typeof(MinProjector<>),
            nameof(Enumerable.Max) => typeof(MaxProjector<>),
            nameof(Enumerable.Average) => typeof(AvgProjector<>),
            _ => throw new NotSupportedException(),
        };

        var genericSubqueryType = originSubqueryType.MakeGenericType(resultType);
        var arraySubqueryType = genericSubqueryType.MakeArrayType();

        string memberPropertyName = methodName switch
        {
            nameof(Enumerable.Sum) => nameof(SumProjector<int>.Sum),
            nameof(Enumerable.Min) => nameof(MinProjector<int>.Min),
            nameof(Enumerable.Max) => nameof(MaxProjector<int>.Max),
            nameof(Enumerable.Average) => nameof(AvgProjector<int>.Avg),
            _ => throw new NotSupportedException(),
        };

        var aggregationType = methodName switch
        {
            nameof(Enumerable.Sum) => AggregationType.Sum,
            nameof(Enumerable.Min) => AggregationType.Min,
            nameof(Enumerable.Max) => AggregationType.Max,
            nameof(Enumerable.Average) => AggregationType.Avg,
            _ => throw new NotSupportedException(),
        };

        var innerQuery = sourceSelect
            .WithProjection(
                new AggregationFieldProjectionExpression(
                    genericSubqueryType,
                    aggregationType,
                    selector?.Body ?? sourceSelect.Projection.InnerExpression!,
                    alias: memberPropertyName
                )
            )
            .WithGroupAll();

        // 💡 We simulate the type so it can be interpreted later as an "Array"
        return CreateTopLevelExpression(
            resultType,
            genericSubqueryType,
            memberPropertyName,
            innerQuery,
            arraySubqueryType
        );
    }

    private CustomExpression BindAll(Type resultType, Expression source, LambdaExpression predicate)
    {
        var sourceSelect = (SelectExpression)Visit(source);

        var whereExpression = Visit(predicate.Body);
        sourceSelect = sourceSelect.AppendWhere(Expression.Not(whereExpression));

        var arraySubqueryType = resultType.MakeArrayType();

        var subqueryExpression = new SubqueryExpression(sourceSelect, arraySubqueryType);

        var countMethodInfo = typeof(Enumerable)
            .GetMethods()
            .First(m => m.Name == nameof(Enumerable.Count) && m.GetParameters().Length == 1)
            .MakeGenericMethod(resultType);
        var indexedSubqueryExpression = Expression.Equal(
            Expression.Call(instance: null, countMethodInfo, [subqueryExpression]),
            Expression.Constant(0)
        );

        return new CustomExpression(indexedSubqueryExpression, resultType);
    }

    private CustomExpression BindAny(
        Type resultType,
        Expression source,
        LambdaExpression? predicate
    )
    {
        var sourceSelect = (SelectExpression)Visit(source);

        if (predicate is not null)
        {
            var whereExpression = Visit(predicate.Body);
            sourceSelect = sourceSelect.AppendWhere(whereExpression);
        }

        var arraySubqueryType = resultType.MakeArrayType();

        var subqueryExpression = new SubqueryExpression(sourceSelect, arraySubqueryType);

        var countMethodInfo = typeof(Enumerable)
            .GetMethods()
            .First(m => m.Name == nameof(Enumerable.Count) && m.GetParameters().Length == 1)
            .MakeGenericMethod(resultType);
        var indexedSubqueryExpression = Expression.GreaterThan(
            Expression.Call(instance: null, countMethodInfo, [subqueryExpression]),
            Expression.Constant(0)
        );

        return new CustomExpression(indexedSubqueryExpression, resultType);
    }

    private CustomExpression BindContains(Type resultType, Expression source, Expression value)
    {
        var sourceSelect = Visit(source);
        var valueExpression = Visit(value);

        var methodInfo = typeof(Enumerable)
            .GetMethods()
            .First(m => m.Name == nameof(Enumerable.Contains) && m.GetParameters().Length == 2)
            .MakeGenericMethod(source.Type.GenericTypeArguments[0]);

        return new CustomExpression(
            Expression.Call(instance: null, methodInfo, [sourceSelect, valueExpression]),
            resultType
        );
    }

    private CustomExpression BindDistinct(Type resultType, Expression source)
    {
        var sourceSelect = (SelectExpression)Visit(source);

        var genericSubqueryType = typeof(DistinctProjector<>).MakeGenericType(
            resultType.GenericTypeArguments[0]
        );
        var arraySubqueryType = genericSubqueryType.MakeArrayType();

        const string memberPropertyName = nameof(DistinctProjector<object>.Values);

        var aggregationType = AggregationType.Distinct;

        var innerQuery = sourceSelect
            .WithProjection(
                new AggregationFieldProjectionExpression(
                    genericSubqueryType,
                    aggregationType,
                    sourceSelect.Projection.InnerExpression!,
                    alias: memberPropertyName
                )
            )
            .WithGroupAll();

        // 💡 We simulate the type so it can be interpreted later as an "Array"
        return CreateTopLevelExpression(
            resultType,
            genericSubqueryType,
            memberPropertyName,
            innerQuery,
            arraySubqueryType
        );
    }

    private CustomExpression BindElementAt(
        Type resultType,
        Expression source,
        Expression indexExpression,
        bool shouldReturnDefault
    )
    {
        var sourceSelect = (SelectExpression)Visit(source);

        var arraySubqueryType = resultType.MakeArrayType();

        var subqueryExpression = new SubqueryExpression(sourceSelect, arraySubqueryType);
        var indexedSubqueryExpression = Expression.ArrayIndex(subqueryExpression, indexExpression);

        return shouldReturnDefault
            ? CreateOrDefaultExpression(resultType, indexedSubqueryExpression)
            : new CustomExpression(indexedSubqueryExpression, resultType);
    }

    private CustomExpression BindFirst(
        Type resultType,
        Expression source,
        LambdaExpression? predicate,
        bool shouldReturnDefault
    )
    {
        var sourceSelect = (SelectExpression)Visit(source);

        if (predicate is not null)
        {
            var whereExpression = Visit(predicate.Body);
            sourceSelect = sourceSelect.AppendWhere(whereExpression);
        }

        var arraySubqueryType = resultType.MakeArrayType();

        var subqueryExpression = new SubqueryExpression(sourceSelect, arraySubqueryType);
        var indexedSubqueryExpression = Expression.ArrayIndex(
            subqueryExpression,
            Expression.Constant(0)
        );

        return shouldReturnDefault
            ? CreateOrDefaultExpression(resultType, indexedSubqueryExpression)
            : new CustomExpression(indexedSubqueryExpression, resultType);
    }

    private CustomExpression BindLast(
        Type resultType,
        Expression source,
        LambdaExpression? predicate,
        bool shouldReturnDefault
    )
    {
        var sourceSelect = (SelectExpression)Visit(source);

        if (predicate is not null)
        {
            var whereExpression = Visit(predicate.Body);
            sourceSelect = sourceSelect.AppendWhere(whereExpression);
        }

        var arraySubqueryType = resultType.MakeArrayType();

        var subqueryExpression = new SubqueryExpression(sourceSelect, arraySubqueryType);

        var lastMethodInfo = typeof(Enumerable)
            .GetMethods()
            .First(m => m.Name == nameof(Enumerable.LastOrDefault) && m.GetParameters().Length == 1)
            .MakeGenericMethod(resultType);
        var indexedSubqueryExpression = Expression.Call(
            instance: null,
            lastMethodInfo,
            [subqueryExpression]
        );

        return shouldReturnDefault
            ? CreateOrDefaultExpression(resultType, indexedSubqueryExpression)
            : new CustomExpression(indexedSubqueryExpression, resultType);
    }

    private SelectExpression BindSingle(
        Type resultType,
        Expression source,
        LambdaExpression? predicate
    )
    {
        var sourceSelect = (SelectExpression)Visit(source);

        if (predicate is not null)
        {
            var whereExpression = Visit(predicate.Body);
            sourceSelect = sourceSelect.AppendWhere(whereExpression);
        }

        return sourceSelect.WithSingleValue();
    }

    private static CustomExpression CreateOrDefaultExpression(
        Type resultType,
        Expression indexedSubqueryExpression
    )
    {
        var nullableResultType = resultType.GetNullableType();

        return new CustomExpression(
            Expression.Coalesce(
                new CustomExpression(indexedSubqueryExpression, nullableResultType),
                Expression.Constant(resultType.GetDefaultValue())
            ),
            resultType
        );
    }

    private static CustomExpression CreateTopLevelExpression(
        Type resultType,
        Type topLevelProjectionType,
        string exportedPropertyName,
        SelectExpression subqueryInnerQuery,
        Type subqueryType
    )
    {
        var subqueryExpression = new SubqueryExpression(subqueryInnerQuery, subqueryType);
        var memberInfo = topLevelProjectionType.GetProperty(exportedPropertyName)!;

        return new CustomExpression(
            Expression.MakeMemberAccess(
                Expression.ArrayIndex(subqueryExpression, Expression.Constant(0)),
                memberInfo
            ),
            resultType
        );
    }

    private static Expression StripQuotes(Expression node)
    {
        Expression result = node;

        while (result.NodeType == ExpressionType.Quote)
        {
            result = ((UnaryExpression)result).Operand;
        }

        return result;
    }

    private static SelectExpression MapSurrealDbQueryable(ISurrealDbQueryable surrealQueryable)
    {
        return new SelectExpression(
            surrealQueryable.EnumerableElementType,
            new TableExpression(surrealQueryable.FromTable),
            ExpressionProjectionExpression.All(surrealQueryable.ElementType)
        );
    }
}
