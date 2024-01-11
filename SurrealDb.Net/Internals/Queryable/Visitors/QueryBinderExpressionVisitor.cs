using System.Linq.Expressions;
using System.Reflection;
using SurrealDb.Net.Internals.Helpers;
using SurrealDb.Net.Internals.Queryable.Expressions;
#if NET7_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace SurrealDb.Net.Internals.Queryable.Visitors;

internal class QueryBinderExpressionVisitor : ExpressionVisitor
{
    public ColumnProjectorExpressionVisitor ColumnProjector { get; }
    public Dictionary<ParameterExpression, Expression> Map { get; } = [];

    private int _aliasCount;
    private readonly Dictionary<Expression, GroupByInfo> _groupByMap = [];
    private Expression? _currentGroupElement;

    internal QueryBinderExpressionVisitor()
    {
        ColumnProjector = new(CanBeColumn);
    }

    internal Expression Bind(Expression expression)
    {
        //Map = [];
        return Visit(expression);
    }

#if NET7_0_OR_GREATER
    [UnconditionalSuppressMessage(
        "AOT",
        "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.",
        Justification = "<Pending>"
    )]
#endif
    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (
            node.Method.DeclaringType == typeof(System.Linq.Queryable)
            || node.Method.DeclaringType == typeof(Enumerable)
        )
        {
            return node.Method.Name switch
            {
                nameof(Enumerable.Select) => BindSelect(
                    node.Type,
                    node.Arguments[0],
                    (LambdaExpression)StripQuotes(node.Arguments[1])
                ),
                nameof(Enumerable.Where) => BindWhere(
                    node.Type,
                    node.Arguments[0],
                    (LambdaExpression)StripQuotes(node.Arguments[1])
                ),
                nameof(Enumerable.GroupBy) => node.Arguments.Count switch
                {
                    2 => BindGroup(
                        node.Arguments[0],
                        (LambdaExpression)StripQuotes(node.Arguments[1]),
                        null,
                        null
                    ),
                    3 => BindGroup(
                        node.Arguments[0],
                        (LambdaExpression)StripQuotes(node.Arguments[1]),
                        (LambdaExpression)StripQuotes(node.Arguments[2]),
                        null
                    ),
                    4 => BindGroup(
                        node.Arguments[0],
                        (LambdaExpression)StripQuotes(node.Arguments[1]),
                        (LambdaExpression)StripQuotes(node.Arguments[2]),
                        (LambdaExpression)StripQuotes(node.Arguments[3])
                    ),
                    _ => throw new NotSupportedException(
                        string.Format("The method '{0}' is not supported", node.Method.Name)
                    ),
                },
                nameof(Enumerable.Count)
                or nameof(Enumerable.Min)
                or nameof(Enumerable.Max)
                or nameof(Enumerable.Sum)
                or nameof(Enumerable.Average) => node.Arguments.Count switch
                {
                    1 => BindAggregate(node.Arguments[0], node.Method, null, true), // TODO : isRoot?
                    2 => BindAggregate(
                        node.Arguments[0],
                        node.Method,
                        (LambdaExpression)StripQuotes(node.Arguments[1]),
                        true
                    ), // TODO : isRoot?
                    _ => throw new NotSupportedException(
                        string.Format("The method '{0}' is not supported", node.Method.Name)
                    ),
                },
                nameof(Enumerable.OrderBy) => BindOrder(
                    node.Arguments[0],
                    (LambdaExpression)StripQuotes(node.Arguments[1]),
                    OrderType.Ascending
                ),
                nameof(Enumerable.OrderByDescending) => BindOrder(
                    node.Arguments[0],
                    (LambdaExpression)StripQuotes(node.Arguments[1]),
                    OrderType.Descending
                ),
                nameof(Enumerable.ThenBy) => BindOrder(
                    node.Arguments[0],
                    (LambdaExpression)StripQuotes(node.Arguments[1]),
                    OrderType.Ascending,
                    true
                ),
                nameof(Enumerable.ThenByDescending) => BindOrder(
                    node.Arguments[0],
                    (LambdaExpression)StripQuotes(node.Arguments[1]),
                    OrderType.Descending,
                    true
                ),
                nameof(Enumerable.Take) => BindTake(node.Arguments[0], node.Arguments[1]),
                nameof(Enumerable.Skip) => BindSkip(node.Arguments[0], node.Arguments[1]),
                _ => throw new NotSupportedException(
                    string.Format("The method '{0}' is not supported", node.Method.Name)
                ),
            };
        }

        return base.VisitMethodCall(node);
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        if (IsTable(node.Value))
        {
            var table =
                node.Value as ISurrealDbQueryable
                ?? throw new InvalidOperationException("Invalid source node type");
            // var provider =
            //     table.Provider as ISurrealDbQueryProvider
            //     ?? throw new InvalidOperationException("Invalid provider type");

#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
            var resultType = typeof(IEnumerable<>).MakeGenericType(table.ElementType);
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.

            return new SelectExpression(
                resultType,
                string.Empty,
                new TableExpression(resultType, string.Empty, table.FromTable),
                null,
                [],
                [],
                null,
                null
            );

            //return GetTableProjection(node.Value, node.Type);
        }

        //return base.VisitConstant(node);
        return node;
    }

    protected override Expression VisitParameter(ParameterExpression node)
    {
        return Map?.TryGetValue(node, out var e) == true ? e : node;
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        return node;
    }

    protected override Expression VisitNew(NewExpression node)
    {
        return node;
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        return node;
    }

    //protected override Expression VisitMemberAccess(MemberExpression m)
    //{
    //    var source = Visit(m.Expression);

    //    switch (source.NodeType)
    //    {
    //        case ExpressionType.MemberInit:

    //            var min = (MemberInitExpression)source;

    //            for (int i = 0, n = min.Bindings.Count; i < n; i++)
    //            {
    //                MemberAssignment assign = min.Bindings[i] as MemberAssignment;

    //                if (assign != null && MembersMatch(assign.Member, m.Member))
    //                {
    //                    return assign.Expression;
    //                }
    //            }

    //            break;

    //        case ExpressionType.New:

    //            NewExpression nex = (NewExpression)source;

    //            if (nex.Members != null)
    //            {
    //                for (int i = 0, n = nex.Members.Count; i < n; i++)
    //                {
    //                    if (MembersMatch(nex.Members[i], m.Member))
    //                    {
    //                        return nex.Arguments[i];
    //                    }
    //                }
    //            }

    //            break;
    //    }

    //    if (source == m.Expression)
    //    {
    //        return m;
    //    }

    //    return MakeMemberAccess(source, m.Member);
    //}

    private Expression BindSkip(Expression source, Expression value)
    {
        var sourceSelect = (SelectExpression)Visit(source);

        return new SelectExpression(
            sourceSelect.Type,
            sourceSelect.Alias,
            sourceSelect.From,
            sourceSelect.Where,
            sourceSelect.GroupBy,
            sourceSelect.OrderBy,
            sourceSelect.Limit,
            value
        );
    }

    private Expression BindTake(Expression source, Expression value)
    {
        var sourceSelect = (SelectExpression)Visit(source);

        return new SelectExpression(
            sourceSelect.Type,
            sourceSelect.Alias,
            sourceSelect.From,
            sourceSelect.Where,
            sourceSelect.GroupBy,
            sourceSelect.OrderBy,
            value,
            sourceSelect.Start
        );
    }

    private Expression BindOrder(
        Expression source,
        Expression value,
        OrderType orderType,
        bool chainable = false
    )
    {
        var sourceSelect = (SelectExpression)Visit(source);
        var orderExpression = new OrderExpression(orderType, value);

        return new SelectExpression(
            sourceSelect.Type,
            sourceSelect.Alias,
            sourceSelect.From,
            sourceSelect.Where,
            sourceSelect.GroupBy,
            chainable ? [.. sourceSelect.OrderBy, orderExpression] : [orderExpression],
            sourceSelect.Limit,
            sourceSelect.Start
        );
    }

#if NET7_0_OR_GREATER
    [RequiresDynamicCode("Calls SurrealDb.Net.Internals.Helpers.TypeHelper.GetSequenceType(Type)")]
#endif
    private Expression BindGroup(
        Expression source,
        LambdaExpression keySelector,
        LambdaExpression? elementSelector,
        LambdaExpression? resultSelector
    )
    {
        ProjectionExpression projection = VisitSequence(source);
        Map[keySelector.Parameters[0]] = projection.Projector;
        Expression keyExpr = Visit(keySelector.Body);
        Expression elemExpr = projection.Projector;

        if (elementSelector is not null)
        {
            Map[elementSelector.Parameters[0]] = projection.Projector;
            elemExpr = Visit(elementSelector.Body);
        }

        // Use ProjectColumns to get group-by expressions from key expression
        ProjectedColumns keyProjection = ProjectColumns(
            keyExpr,
            projection.Source.Alias,
            projection.Source.Alias
        );
        IEnumerable<Expression> groupExprs = keyProjection.Columns.Select(c => c.Expression);

        // make duplicate of source query as basis of element subquery by visiting the source again
        ProjectionExpression subqueryBasis = VisitSequence(source);

        // recompute key columns for group expressions relative to subquery (need these for doing the correlation predicate)
        Map[keySelector.Parameters[0]] = subqueryBasis.Projector;
        Expression subqueryKey = Visit(keySelector.Body);

        // use same projection trick to get group-by expressions based on subquery
        ProjectedColumns subqueryKeyPC = ProjectColumns(
            subqueryKey,
            subqueryBasis.Source.Alias,
            subqueryBasis.Source.Alias
        );
        IEnumerable<Expression> subqueryGroupExprs = subqueryKeyPC.Columns.Select(c =>
            c.Expression
        );
        Expression subqueryCorrelation = BuildPredicateWithEquals(subqueryGroupExprs, groupExprs)!;

        // compute element based on duplicated subquery
        Expression subqueryElemExpr = subqueryBasis.Projector;
        if (elementSelector is not null)
        {
            Map[elementSelector.Parameters[0]] = subqueryBasis.Projector;
            subqueryElemExpr = Visit(elementSelector.Body);
        }

        // build subquery that projects the desired element
        string elementAlias = GetNextAlias();

        ProjectedColumns elementPC = ProjectColumns(
            subqueryElemExpr,
            elementAlias,
            subqueryBasis.Source.Alias
        );
        ProjectionExpression elementSubquery = new ProjectionExpression(
            new SelectExpression(
                TypeHelper.GetSequenceType(subqueryElemExpr.Type),
                elementAlias,
                elementPC.Columns,
                subqueryBasis.Source,
                subqueryCorrelation,
                [],
                [],
                null,
                null
            ),
            elementPC.Projector
        );

        string alias = GetNextAlias();
        // make it possible to tie aggregates back to this group-by
        GroupByInfo info = new GroupByInfo(alias, elemExpr);
        _groupByMap.Add(elementSubquery, info);
        Expression resultExpr;

        if (resultSelector is not null)
        {
            var saveGroupElement = _currentGroupElement;
            _currentGroupElement = elementSubquery;

            // compute result expression based on key & element-subquery
            Map[resultSelector.Parameters[0]] = keyProjection.Projector;
            Map[resultSelector.Parameters[1]] = elementSubquery;
            resultExpr = Visit(resultSelector.Body);
            _currentGroupElement = saveGroupElement;
        }
        else
        {
            // result must be IGrouping<K,E>
            resultExpr = Expression.New(
                typeof(Grouping<,>)
                    .MakeGenericType(keyExpr.Type, subqueryElemExpr.Type)
                    .GetConstructors()[0],
                [keyExpr, elementSubquery]
            );
        }

        ProjectedColumns pc = ProjectColumns(resultExpr, alias, projection.Source.Alias);

        // make it possible to tie aggregates back to this group-by
        Expression projectedElementSubquery = ((NewExpression)pc.Projector).Arguments[1];
        _groupByMap.Add(projectedElementSubquery, info);

        return new ProjectionExpression(
            new SelectExpression(
                TypeHelper.GetSequenceType(resultExpr.Type),
                alias,
                pc.Columns,
                projection.Source,
                null,
                groupExprs,
                [],
                null,
                null
            ),
            pc.Projector
        );
    }

    //private Expression BindGroup(Expression source, Expression value)
    //{
    //    var sourceSelect = (SelectExpression)Visit(source);

    //    return new SelectExpression(
    //        sourceSelect.Type,
    //        sourceSelect.Alias,
    //        sourceSelect.From,
    //        sourceSelect.Where,
    //        value,
    //        sourceSelect.OrderBy,
    //        sourceSelect.Limit,
    //        sourceSelect.Start
    //    );
    //}

#if NET7_0_OR_GREATER
    [RequiresDynamicCode("Calls System.Type.MakeGenericType(params Type[])")]
#endif
    private Expression BindAggregate(
        Expression source,
        MethodInfo method,
        LambdaExpression? argument,
        bool isRoot
    )
    {
        Type returnType = method.ReturnType;
        AggregateType aggType = GetAggregateType(method.Name);
        bool hasPredicateArg = HasPredicateArg(aggType);

        if (argument is not null && hasPredicateArg)
        {
            // convert query.Count(predicate) into query.Where(predicate).Count()
            source = Expression.Call(
                typeof(System.Linq.Queryable),
                nameof(Enumerable.Where),
                method.GetGenericArguments(),
                source,
                argument
            );
            argument = null;
        }

        ProjectionExpression projection = VisitSequence(source);
        Expression? argExpr = null;

        if (argument is not null)
        {
            Map[argument.Parameters[0]] = projection.Projector;
            argExpr = Visit(argument.Body);
        }
        else if (!hasPredicateArg)
        {
            argExpr = projection.Projector;
        }

        string alias = GetNextAlias();
        var pc = ProjectColumns(projection.Projector, alias, projection.Source.Alias);
        Expression aggExpr = new AggregateExpression(returnType, aggType, argExpr);
        Type selectType = typeof(IEnumerable<>).MakeGenericType(returnType);
        SelectExpression select = new SelectExpression(
            selectType,
            alias,
            [new ColumnDeclaration("", aggExpr)],
            projection.Source,
            null,
            [],
            [],
            null,
            null
        );

        if (isRoot)
        {
            ParameterExpression p = Expression.Parameter(selectType, "p");
            LambdaExpression gator = Expression.Lambda(
                Expression.Call(typeof(Enumerable), nameof(Enumerable.Single), [returnType], p),
                p
            );
            return new ProjectionExpression(
                select,
                //new ColumnExpression(returnType, alias, ""),
                gator
            );
        }

        SubqueryExpression subquery = new SubqueryExpression(returnType, select);

        // if we can find the corresponding group-info we can build a special AggregateSubquery node that will enable us to
        // optimize the aggregate expression later using AggregateRewriter
        //GroupByInfo info;

        if (!hasPredicateArg && _groupByMap.TryGetValue(projection, out var info))
        {
            // use the element expression from the group-by info to rebind the argument so the resulting expression is one that
            // would be legal to add to the columns in the select expression that has the corresponding group-by clause.

            if (argument is not null)
            {
                Map[argument.Parameters[0]] = info.Element;
                argExpr = Visit(argument.Body);
            }
            else
            {
                argExpr = info.Element;
            }

            aggExpr = new AggregateExpression(returnType, aggType, argExpr);

            // check for easy to optimize case.  If the projection that our aggregate is based on is really the 'group' argument from
            // the query.GroupBy(xxx, (key, group) => yyy) method then whatever expression we return here will automatically
            // become part of the select expression that has the group-by clause, so just return the simple aggregate expression.

            if (projection == _currentGroupElement)
                return aggExpr;

            return new AggregateSubqueryExpression(info.Alias, aggExpr, subquery);
        }

        return subquery;
    }

    private static AggregateType GetAggregateType(string methodName)
    {
        return methodName switch
        {
            nameof(Enumerable.Count) => AggregateType.Count,
            nameof(Enumerable.Min) => AggregateType.Min,
            nameof(Enumerable.Max) => AggregateType.Max,
            nameof(Enumerable.Sum) => AggregateType.Sum,
            nameof(Enumerable.Average) => AggregateType.Average,
            _ => throw new Exception(string.Format("Unknown aggregate type: {0}", methodName)),
        };
    }

    private static bool HasPredicateArg(AggregateType aggregateType)
    {
        return aggregateType == AggregateType.Count;
    }

    private Expression BindWhere(Type resultType, Expression source, LambdaExpression predicate)
    {
        var sourceSelect = (SelectExpression)Visit(source);

        Map?.Add(predicate.Parameters[0], sourceSelect);
        var where = Visit(predicate.Body);

        var newWhere = sourceSelect.Where is not null
            ? Expression.AndAlso(sourceSelect.Where, where)
            : where;

        return new SelectExpression(
            resultType,
            string.Empty,
            sourceSelect.From,
            newWhere,
            sourceSelect.GroupBy,
            sourceSelect.OrderBy,
            sourceSelect.Start,
            sourceSelect.Limit
        );

        // ----------------------------------------------------------------
        //var sourceSelect = (SelectExpression)Visit(source);

        //Map?.Add(predicate.Parameters[0], sourceSelect);
        //var where = Visit(predicate.Body);

        //if (TryMergeSelects(sourceSelect, out var mergedSelect))
        //{
        //    return mergedSelect;
        //}

        //return new SelectExpression(resultType, string.Empty, sourceSelect, where);
        // ----------------------------------------------------------------

        //var projection = (ProjectionExpression)Visit(source);

        //Map?.Add(predicate.Parameters[0], projection.Projector);

        //var where = Visit(predicate.Body);

        //string alias = GetNextAlias();

        //var pc = ProjectColumns(projection.Projector, alias, GetExistingAlias(projection.Source));

        //return new ProjectionExpression(
        //    new SelectExpression(resultType, alias, projection.Source, where),
        //    pc.Projector
        //);
    }

    private Expression BindSelect(Type resultType, Expression source, LambdaExpression selector)
    {
        var sourceSelect = (SelectExpression)Visit(source);

        Map?.Add(selector.Parameters[0], sourceSelect);

        var expression = Visit(selector.Body); // TODO : Used to get Columns

        bool canMerge = true; // TODO

        if (canMerge)
        {
            if (sourceSelect.AllFields)
            {
                return new SelectExpression(
                    resultType,
                    string.Empty,
                    sourceSelect.From,
                    sourceSelect.Where,
                    sourceSelect.GroupBy,
                    sourceSelect.OrderBy,
                    sourceSelect.Start,
                    sourceSelect.Limit
                );
            }

            // TODO : Detect columns
            return new SelectExpression(
                resultType,
                string.Empty,
                [],
                sourceSelect.From,
                sourceSelect.Where,
                sourceSelect.GroupBy,
                sourceSelect.OrderBy,
                sourceSelect.Start,
                sourceSelect.Limit
            );
        }

        return new SelectExpression(
            resultType,
            string.Empty,
            [],
            sourceSelect,
            null,
            [],
            [],
            null,
            null
        ); // TODO

        //var projection = (ProjectionExpression)Visit(source);

        //Map?.Add(selector.Parameters[0], projection.Projector);

        //var expression = Visit(selector.Body);

        //string alias = GetNextAlias();

        //var pc = ProjectColumns(expression, alias, GetExistingAlias(projection.Source));

        //return new ProjectionExpression(
        //    new SelectExpression(resultType, alias, pc.Columns, projection.Source, []),
        //    pc.Projector
        //);
    }

    private ProjectionExpression VisitSequence(Expression source)
    {
        return ConvertToSequence(base.Visit(source));
    }

    private ProjectionExpression ConvertToSequence(Expression expr)
    {
        if (
            expr is SurrealExpression surrealExpression
            && surrealExpression.SurrealNodeType == SurrealExpressionType.Projection
        )
        {
            return (ProjectionExpression)expr;
        }

        switch (expr.NodeType)
        {
            //case (ExpressionType)DbExpressionType.Projection:
            //    return (ProjectionExpression)expr;
            case ExpressionType.New:
                NewExpression nex = (NewExpression)expr;
                if (
                    expr.Type.GetTypeInfo().IsGenericType
                    && expr.Type.GetGenericTypeDefinition() == typeof(Grouping<,>)
                )
                {
                    return (ProjectionExpression)nex.Arguments[1];
                }
                goto default;
            //case ExpressionType.MemberAccess:
            //    var bound = BindRelationshipProperty((MemberExpression)expr);
            //    if (bound.NodeType != ExpressionType.MemberAccess)
            //        return ConvertToSequence(bound);
            //    goto default;
            //default:
            //    var n = GetNewExpression(expr);
            //    if (n is not null)
            //    {
            //        expr = n;
            //        goto case ExpressionType.New;
            //    }
            //    throw new Exception(
            //        string.Format("The expression of type '{0}' is not a sequence", expr.Type)
            //    );
            default:
                throw new Exception(
                    string.Format("The expression of type '{0}' is not a sequence", expr.Type)
                );
        }
    }

    //private Expression BindRelationshipProperty(MemberExpression mex)
    //{
    //    EntityExpression ex = mex.Expression as EntityExpression;
    //    if (ex != null && this.mapper.Mapping.IsRelationship(ex.Entity, mex.Member))
    //    {
    //        return this.mapper.GetMemberExpression(mex.Expression, ex.Entity, mex.Member);
    //    }
    //    return mex;
    //}

    private Expression? BuildPredicateWithEquals(
        IEnumerable<Expression> source1,
        IEnumerable<Expression> source2
    )
    {
        IEnumerator<Expression> en1 = source1.GetEnumerator();
        IEnumerator<Expression> en2 = source2.GetEnumerator();
        Expression? result = null;

        while (en1.MoveNext() && en2.MoveNext())
        {
            var compare = Expression.Equal(en1.Current, en2.Current);
            //var compare =
            //    Expression.Or(
            //        new IsNullExpression(en1.Current).And(new IsNullExpression(en2.Current)),
            //        en1.Current.Equal(en2.Current)
            //        );
            result = (result is null) ? compare : Expression.And(result, compare);
        }

        return result;
    }

    private ProjectionExpression GetTableProjection(object? value, Type type)
    {
        var table =
            value as IQueryable ?? throw new InvalidOperationException("Invalid source node type");

        string tableAlias = GetNextAlias();
        string selectAlias = GetNextAlias();

        List<MemberBinding> bindings = [];
        List<ColumnDeclaration> columns = [];

        //#pragma warning disable IL2070 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.
        //        var typeProperties = type.GetProperties().ToArray();
        //#pragma warning restore IL2070 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.

        foreach (var mi in GetMappedMembers(table.ElementType))
        {
            string columnName = GetColumnName(mi);
            var columnType = GetColumnType(mi);

            bindings.Add(
                Expression.Bind(mi, new ColumnExpression(columnType, selectAlias, columnName))
            );

            columns.Add(
                new ColumnDeclaration(
                    columnName,
                    new ColumnExpression(columnType, tableAlias, columnName)
                )
            );
        }

#pragma warning disable IL2072 // Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.
        var projector = Expression.MemberInit(Expression.New(table.ElementType), bindings);
#pragma warning restore IL2072 // Target parameter argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The return value of the source method does not have matching annotations.
#pragma warning disable IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.
        var resultType = typeof(IEnumerable<>).MakeGenericType(table.ElementType);
#pragma warning restore IL3050 // Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.

        return new ProjectionExpression(
            new SelectExpression(
                resultType,
                selectAlias,
                columns,
                new TableExpression(resultType, tableAlias, GetTableName(table)),
                null,
                [],
                [],
                null,
                null
            ),
            projector
        );
    }

    //private static bool IsQuery(Expression expression)
    //{
    //    //var elementType = TypeHelper.GetElementType(expression.Type);
    //    return elementType is not null
    //        && typeof(IQueryable<>).MakeGenericType(elementType).IsAssignableFrom(expression.Type);
    //}

    private static bool IsTable(object? value)
    {
        var q = value as IQueryable;
        return q is not null && q.Expression.NodeType == ExpressionType.Constant;
    }

    private ProjectedColumns ProjectColumns(
        Expression expression,
        string newAlias,
        string existingAlias
    )
    {
        return ColumnProjector.ProjectColumns(expression, newAlias, existingAlias);
    }

    private string GetNextAlias()
    {
        return "t" + _aliasCount++;
    }

    private static bool CanBeColumn(Expression expression)
    {
        return expression is SurrealExpression surrealExpression
            && surrealExpression.SurrealNodeType == SurrealExpressionType.Column;
    }

    private static Expression StripQuotes(Expression e)
    {
        while (e.NodeType == ExpressionType.Quote)
        {
            e = ((UnaryExpression)e).Operand;
        }

        return e;
    }

    private static IEnumerable<MemberInfo> GetMappedMembers(Type rowType)
    {
#pragma warning disable IL2070 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.
        return rowType.GetFields().Cast<MemberInfo>();
#pragma warning restore IL2070 // 'this' argument does not satisfy 'DynamicallyAccessedMembersAttribute' in call to target method. The parameter of method does not have matching annotations.
    }

    private static string GetColumnName(MemberInfo member)
    {
        return member.Name;
    }

    private static Type GetColumnType(MemberInfo member)
    {
        var fi = member as FieldInfo;
        if (fi is not null)
        {
            return fi.FieldType;
        }

        var pi = (PropertyInfo)member;
        return pi.PropertyType;
    }

    private static string GetTableName(object table)
    {
        var tableQuery = (IQueryable)table;
        var rowType = tableQuery.ElementType;

        return rowType.Name;
    }

    private static string GetExistingAlias(Expression node)
    {
        var surrealExpression = node as SurrealExpression;

        if (surrealExpression is null)
        {
            throw new InvalidOperationException(
                string.Format("Invalid source node type '{0}'", node.NodeType)
            );
        }

        return surrealExpression.SurrealNodeType switch
        {
            SurrealExpressionType.Select => ((SelectExpression)node).Alias,
            SurrealExpressionType.Table => ((TableExpression)node).Alias,
            _ => throw new InvalidOperationException(
                string.Format("Invalid source node type '{0}'", surrealExpression.SurrealNodeType)
            ),
        };
    }
}
