using System.Collections;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq.Expressions;
using System.Numerics;
using System.Text.RegularExpressions;
using Dahomey.Cbor.Util;
using Microsoft.Spatial;
using SurrealDb.Net.Internals.Constants;
using SurrealDb.Net.Internals.Extensions;
using SurrealDb.Net.Internals.Queryable.Expressions.Intermediate;
using SurrealDb.Net.Internals.Queryable.Expressions.Surreal;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Queryable.Visitors;

internal sealed class SurrealExpressionVisitor : ExpressionVisitor
{
    private readonly Dictionary<ParameterExpression, Expression> _sourceExpressionParameters;
    private readonly Dictionary<string, object?> _parameters;
    private readonly Dictionary<string, object?> _recordIdParameters = [];

    private int _currentNestedSelectLevel = 0;
    private readonly Dictionary<ParameterExpression, int> _parametersNestedLevels = [];

    public SurrealExpressionVisitor(
        Dictionary<ParameterExpression, Expression> sourceExpressionParameters,
        int numberOfNamedValues
    )
    {
        _sourceExpressionParameters = sourceExpressionParameters;
        _parameters = new(numberOfNamedValues);
    }

    internal (Expression Expression, IReadOnlyDictionary<string, object?> Parameters) Bind(
        Expression expression
    )
    {
        var resultExpression = Visit(expression)!;

        IReadOnlyDictionary<string, object?> parameters =
            _recordIdParameters.Count <= 0
                ? _parameters
                : _parameters
                    .Concat(_recordIdParameters)
                    .ToImmutableDictionary(x => x.Key, x => x.Value);

        return (resultExpression, parameters);
    }

    public override Expression? Visit(Expression? node)
    {
        if (node is null)
        {
            return null;
        }

        var (parameter, _) = _sourceExpressionParameters.FirstOrDefault(kv => kv.Value == node);
        if (parameter is not null)
        {
            _parametersNestedLevels.TryAdd(parameter, _currentNestedSelectLevel);
        }

        if (node is IntermediateExpression intermediateExpression)
        {
            return intermediateExpression switch
            {
                SelectExpression selectExpression => BindSelect(selectExpression),
                ProjectionExpression projectionExpression => BindFields(projectionExpression),
                FieldProjectionExpression fieldProjectionExpression => BindField(
                    fieldProjectionExpression
                ),
                SourceExpression sourceExpression => BindSource(sourceExpression),
                TableExpression tableExpression => BindTable(tableExpression),
                WhereExpression whereExpression => BindWhere(whereExpression),
                GroupsExpression groupsExpression => BindGroup(groupsExpression),
                OrdersExpression ordersExpression => BindOrder(ordersExpression),
                TakeExpression takeExpression => BindTake(takeExpression),
                SkipExpression skipExpression => BindSkip(skipExpression),
                NamedValueExpression namedValueExpression => BindParameter(namedValueExpression),
                SubqueryExpression subqueryExpression => BindSubquery(subqueryExpression),
                CustomExpression customExpression => BindCustom(customExpression),
                LambdaIntermediateExpression lambdaExpression => BindLambda(lambdaExpression),
                _ => base.Visit(node),
            };
        }

        return base.Visit(node);
    }

    private SelectStatementExpression BindSelect(SelectExpression selectExpression)
    {
        var fields = (FieldsExpression)Visit(selectExpression.Projection)!;
        var what = (WhatExpression)Visit(selectExpression.Source)!;
        var cond = (ConditionsExpression?)Visit(selectExpression.Where);
        var grouping = (GroupingExpression?)Visit(selectExpression.Groups);
        var ordering = (OrderingExpression?)Visit(selectExpression.Orders);
        var limit = (LimitExpression?)Visit(selectExpression.Take);
        var start = (StartExpression?)Visit(selectExpression.Skip);

        return new SelectStatementExpression(
            fields,
            what,
            cond,
            grouping,
            ordering,
            limit,
            start,
            selectExpression.SingleValue
        );
    }

    private FieldsExpression BindFields(ProjectionExpression projectionExpression)
    {
        if (projectionExpression is ExpressionProjectionExpression expressionProjectionExpression)
        {
            var innerExpression = expressionProjectionExpression.Expression;
            if (innerExpression is null)
            {
                return FieldsExpression.ForType(projectionExpression.Type);
            }

            var valueExpression = Visit(innerExpression)?.ToValue();
            if (valueExpression is ObjectValueExpression objectValueExpression)
            {
                // TODO : Check if projection can be merged?
                return FieldsExpression.From(objectValueExpression);
            }
            if (valueExpression is not null)
            {
                return FieldsExpression.Single(valueExpression, projectionExpression.CanBeSingle);
            }
            throw new NotSupportedException(
                $"The projection of '{innerExpression.Type}' is not supported."
            );
        }

        if (projectionExpression is FieldsProjectionExpression fieldsProjectionExpression)
        {
            var fields = fieldsProjectionExpression
                .Fields.SelectMany(f =>
                {
                    var innerExpression = Visit(f);
                    return innerExpression switch
                    {
                        FieldsExpression fieldsExpression => fieldsExpression.Fields,
                        FieldExpression fieldExpression => [fieldExpression],
                        _ => throw new NotSupportedException(
                            $"The projection of '{f.Expression?.Type}' is not supported."
                        ),
                    };
                })
                .ToImmutableArray();

            return new FieldsExpression(fields);
        }

        if (projectionExpression is CountFieldProjectionExpression countFieldProjectionExpression)
        {
            ValueExpression? predicateValueExpression = null;
            if (countFieldProjectionExpression.Predicate is not null)
            {
                var predicateExpression = Visit(countFieldProjectionExpression.Predicate.Body)
                    ?.ToValue();

                predicateValueExpression =
                    predicateExpression
                    ?? throw new InvalidCastException(
                        $"Failed to convert '{countFieldProjectionExpression.Predicate.Type}' into a value expression."
                    );
            }

            ImmutableArray<ValueExpression> functionParams = predicateValueExpression is null
                ? []
                : [predicateValueExpression];

            return new FieldsExpression(
                [
                    new SingleFieldExpression(
                        new FunctionValueExpression("count", functionParams),
                        null
                    ),
                ]
            );
        }

        if (
            projectionExpression
            is AggregationFieldProjectionExpression aggregationFieldProjectionExpression
        )
        {
            var selectorExpression = Visit(aggregationFieldProjectionExpression.Selector)
                ?.ToValue();
            if (selectorExpression is null)
            {
                throw new InvalidCastException(
                    $"Failed to convert '{aggregationFieldProjectionExpression.Selector.Type}' into a value expression."
                );
            }

            string functionName = aggregationFieldProjectionExpression.AggregationType switch
            {
                AggregationType.Sum => "math::sum",
                AggregationType.Min => "math::min",
                AggregationType.Max => "math::max",
                AggregationType.Avg => "math::mean",
                AggregationType.Flatten => "array::flatten",
                AggregationType.Distinct => "array::distinct",
                _ => throw new NotSupportedException(),
            };

            return new FieldsExpression(
                [
                    new SingleFieldExpression(
                        new FunctionValueExpression(functionName, [selectorExpression]),
                        aggregationFieldProjectionExpression.Alias.ToFieldIdiom()
                    ),
                ]
            );
        }

        throw new NotSupportedException(
            $"The projection of '{projectionExpression.Type}' is not supported."
        );
    }

    private FieldExpression BindField(FieldProjectionExpression fieldProjectionExpression)
    {
        var innerExpression = Visit(fieldProjectionExpression.Expression);
        if (innerExpression is FieldsExpression { Fields: [SingleFieldExpression fieldExpression] })
        {
            if (
                (fieldExpression.Alias is null && fieldProjectionExpression.Alias is not null)
                || (fieldExpression.Alias is not null && fieldProjectionExpression.Alias is null)
                || !fieldExpression.Alias!.IsSame(fieldProjectionExpression.Alias!.ToFieldIdiom())
            )
            {
                return fieldExpression.WithAlias(fieldProjectionExpression.Alias);
            }
            return fieldExpression;
        }

        var valueExpression = innerExpression?.ToValue();
        if (valueExpression is not null)
        {
            var alias = string.IsNullOrWhiteSpace(fieldProjectionExpression.Alias)
                ? null
                : new IdiomExpression([new FieldPartExpression(fieldProjectionExpression.Alias)]);
            return new SingleFieldExpression(valueExpression, alias);
        }

        throw new NotSupportedException(
            $"Cannot convert field of type '{fieldProjectionExpression.Type}'."
        );
    }

    private WhatExpression BindSource(SourceExpression sourceExpression)
    {
        ValueExpression value = sourceExpression switch
        {
            TableSourceExpression tableSourceExpression => (TableValueExpression)
                Visit(tableSourceExpression.Table)!,
            SelectSourceExpression selectSourceExpression => ToSubqueryValueExpression(
                selectSourceExpression
            ),
            _ => throw new InvalidCastException(
                $"Failed to convert source expression of type '{sourceExpression.Type.Name}' to WhatExpression."
            ),
        };
        return WhatExpression.From(value);

        SubqueryValueExpression ToSubqueryValueExpression(
            SelectSourceExpression selectSourceExpression
        )
        {
            _currentNestedSelectLevel++;
            var returnedExpression = new SubqueryValueExpression(
                (SurrealExpression)Visit(selectSourceExpression.Select)!
            );
            _currentNestedSelectLevel--;
            return returnedExpression;
        }
    }

    private static TableValueExpression BindTable(TableExpression tableExpression)
    {
        return new TableValueExpression(tableExpression.TableName);
    }

    private ConditionsExpression BindWhere(WhereExpression whereExpression)
    {
        var valueExpression = Visit(whereExpression.Expression)?.ToValue();
        if (valueExpression is null)
        {
            throw new InvalidCastException("The inner WHERE expression is not a value expression.");
        }

        return new ConditionsExpression(valueExpression);
    }

    private GroupingExpression BindGroup(GroupsExpression groupsExpression)
    {
        if (groupsExpression.Expression is null)
        {
            return new GroupingExpression([]);
        }

        var evaluatedExpression = Visit(groupsExpression.Expression);
        if (evaluatedExpression is FieldsExpression fieldsExpression)
        {
            return new GroupingExpression(
                [
                    .. fieldsExpression.Fields.Select(f =>
                    {
                        if (f is SingleFieldExpression singleFieldExpression)
                        {
                            return singleFieldExpression.Expression.ToIdiom();
                        }

                        return new IdiomExpression(
                            [new StartPartExpression(new StringValueExpression("*"))]
                        );
                    }),
                ]
            );
        }

        var idiomExpression = evaluatedExpression?.ToIdiom();
        if (idiomExpression is null)
        {
            throw new InvalidCastException(
                "The inner GROUP BY expression is not an idiom expression."
            );
        }

        return new GroupingExpression([idiomExpression]);
    }

    private ListOrderingExpression BindOrder(OrdersExpression ordersExpression)
    {
        var orders = ordersExpression
            .Infos.Select(info =>
            {
                var valueExpression = Visit(info.Expression)?.ToValue();
                if (valueExpression is not IdiomValueExpression idiomValue)
                {
                    throw new InvalidCastException(
                        "The inner ORDER BY expression is not a value expression."
                    );
                }

                return new SurrealOrder(idiomValue.Idiom, info.OrderType);
            })
            .ToImmutableArray();

        return new ListOrderingExpression(orders);
    }

    private LimitExpression BindTake(TakeExpression takeExpression)
    {
        var valueExpression = Visit(takeExpression.Expression)?.ToValue();
        if (valueExpression is null)
        {
            throw new InvalidCastException("The inner LIMIT expression is not a value expression.");
        }

        return new LimitExpression(valueExpression);
    }

    private StartExpression BindSkip(SkipExpression skipExpression)
    {
        var valueExpression = Visit(skipExpression.Expression)?.ToValue();
        if (valueExpression is null)
        {
            throw new InvalidCastException("The inner START expression is not a value expression.");
        }

        return new StartExpression(valueExpression);
    }

    private ParameterValueExpression BindParameter(NamedValueExpression namedValueExpression)
    {
        static bool IsReservedParamName(string name)
        {
            string[] reservedParamNames = ["access", "auth", "token", "session"];
            return reservedParamNames.Contains(name);
        }

        var parameterName = IsReservedParamName(namedValueExpression.Name)
            ? $"_{namedValueExpression.Name}"
            : namedValueExpression.Name;

        _parameters[parameterName] = namedValueExpression.Value;

        return new ParameterValueExpression(parameterName);
    }

    private SubqueryValueExpression BindSubquery(SubqueryExpression subqueryExpression)
    {
        _currentNestedSelectLevel++;
        var innerSelect = (SelectStatementExpression)Visit(subqueryExpression.Select)!;
        _currentNestedSelectLevel--;
        return new SubqueryValueExpression(innerSelect);
    }

    private Expression? BindCustom(CustomExpression customExpression)
    {
        return Visit(customExpression.Expression);
    }

    private Expression? BindLambda(LambdaIntermediateExpression lambdaExpression)
    {
        return Visit(lambdaExpression.Expression.Body);
    }

    protected override Expression VisitUnary(UnaryExpression node)
    {
        // List of UnaryExpression: https://learn.microsoft.com/en-us/dotnet/api/system.linq.expressions.unaryexpression?view=net-8.0

        ValueExpression ExtractOperandValueExpression()
        {
            var operandExpression = Visit(node.Operand)?.ToValue();
            if (operandExpression is null)
            {
                throw new InvalidCastException("The unary operand is not a value expression.");
            }

            return operandExpression;
        }

        switch (node.NodeType)
        {
            case ExpressionType.Convert:
                // TODO : Cast expression
                var fromType = node.Operand.Type;
                var toType = node.Type;

                // Ignore nullable to non-nullable cast
                // Ignore non-nullable to nullable cast
                if (
                    fromType == toType
                    || fromType.IsNullableOf(toType)
                    || toType.IsNullableOf(fromType)
                )
                {
                    return ExtractOperandValueExpression();
                }

                if (toType == typeof(bool))
                {
                    return new CastValueExpression("bool", ExtractOperandValueExpression());
                }
                if (
                    toType == typeof(sbyte)
                    || toType == typeof(byte)
                    || toType == typeof(short)
                    || toType == typeof(ushort)
                    || toType == typeof(int)
                    || toType == typeof(uint)
                    || toType == typeof(long)
                    || toType == typeof(ulong)
                )
                {
                    return new CastValueExpression("int", ExtractOperandValueExpression());
                }
                if (toType == typeof(float) || toType == typeof(double))
                {
                    return new CastValueExpression("float", ExtractOperandValueExpression());
                }
                if (toType == typeof(decimal))
                {
                    return new CastValueExpression("decimal", ExtractOperandValueExpression());
                }
                if (toType == typeof(char) || toType == typeof(string))
                {
                    return new CastValueExpression("string", ExtractOperandValueExpression());
                }
                if (toType == typeof(TimeSpan) || toType == typeof(Duration))
                {
                    return new CastValueExpression("duration", ExtractOperandValueExpression());
                }
                if (toType == typeof(DateTime))
                {
                    return new CastValueExpression("number", ExtractOperandValueExpression());
                }
                if (toType == typeof(Guid))
                {
                    return new CastValueExpression("uuid", ExtractOperandValueExpression());
                }
                if (toType == typeof(Regex))
                {
                    return new CastValueExpression("regex", ExtractOperandValueExpression());
                }
                if (toType == typeof(RecordId) || toType.IsSubclassOf(typeof(RecordId)))
                {
                    return new CastValueExpression("record", ExtractOperandValueExpression());
                }
                if (toType == typeof(HashSet<>) || toType.IsSubclassOf(typeof(HashSet<>)))
                {
                    return new CastValueExpression("set", ExtractOperandValueExpression());
                }
                if (
                    toType == typeof(Array)
                    || toType == typeof(List<>)
                    || toType.IsSubclassOf(typeof(Array))
                    || toType.IsSubclassOf(typeof(List<>))
                )
                {
                    return new CastValueExpression("array", ExtractOperandValueExpression());
                }

                throw new NotSupportedException($"Cannot cast from {fromType} to {toType}.");
            case ExpressionType.TypeAs:
                // TODO : Cast expression?
                return null!;
            case ExpressionType.ArrayLength:
            {
                var valueExpression = ExtractOperandValueExpression();
                return new FunctionValueExpression("array::len", [valueExpression]);
            }
            case ExpressionType.Increment
            or ExpressionType.PreIncrementAssign
            or ExpressionType.PostIncrementAssign:
            {
                var valueExpression = ExtractOperandValueExpression();
                if (valueExpression is not IntegerValueExpression integerValueExpression)
                {
                    throw new NotSupportedException(
                        "Cannot increment on a non-integer value expression."
                    );
                }

                var incrementExpression = new BinaryValueExpression(
                    integerValueExpression,
                    new SimpleOperator(OperatorType.Add),
                    new Int32ValueExpression(1)
                );

                return node.NodeType == ExpressionType.PreIncrementAssign
                    ? incrementExpression.Invert()
                    : incrementExpression;
            }
            case ExpressionType.Decrement
            or ExpressionType.PreDecrementAssign
            or ExpressionType.PostDecrementAssign:
            {
                var valueExpression = ExtractOperandValueExpression();
                if (valueExpression is not IntegerValueExpression integerValueExpression)
                {
                    throw new NotSupportedException(
                        "Cannot decrement on a non-integer value expression."
                    );
                }

                var decrementExpression = new BinaryValueExpression(
                    integerValueExpression,
                    new SimpleOperator(OperatorType.Sub),
                    new Int32ValueExpression(1)
                );

                return node.NodeType == ExpressionType.PreDecrementAssign
                    ? decrementExpression.Invert()
                    : decrementExpression;
            }
            default:
            {
                var operatorType = node.NodeType switch
                {
                    ExpressionType.Not => OperatorType.Not,
                    ExpressionType.UnaryPlus => OperatorType.Add,
                    ExpressionType.Negate => OperatorType.Neg,
                    _ => throw new NotSupportedException(
                        $"The unary operator '{node.NodeType}' is not supported."
                    ),
                };

                var valueExpression = ExtractOperandValueExpression();
                return new UnaryValueExpression(new SimpleOperator(operatorType), valueExpression);
            }
        }
    }

    protected override Expression VisitBinary(BinaryExpression node)
    {
        // List of BinaryExpression: https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-linq-expressions-binaryexpression

        if (node.NodeType == ExpressionType.ArrayIndex)
        {
            var left = Visit(node.Left)?.ToIdiom();
            if (left is null)
            {
                throw new InvalidCastException(
                    $"Failed to convert '{node.Left.Type}' into an idiom expression."
                );
            }

            PartExpression? right = null;
            var innerRightExpresion = Visit(node.Right);
            if (
                innerRightExpresion is ConstantExpression constantExpression
                && constantExpression.Type == typeof(int)
            )
            {
                right = new IndexPartExpression((int)constantExpression.Value!);
            }

            var rightValueExpression = innerRightExpresion?.ToValue();
            if (rightValueExpression is null)
            {
                throw new InvalidCastException(
                    $"Failed to convert '{node.Right.Type}' into a value expression."
                );
            }
            right ??= new ValuePartExpression(rightValueExpression);

            // TODO : Array indexed access, giving "left[right]"
            return IdiomExpression.Chain(left, right);
        }

        var operatorType = node.NodeType switch
        {
            ExpressionType.Add => OperatorType.Add,
            ExpressionType.And => throw new NotSupportedException(
                "Bitwise operations are not supported."
            ),
            ExpressionType.AndAlso => OperatorType.And,
            ExpressionType.Coalesce => OperatorType.Nco,
            ExpressionType.Divide => OperatorType.Div,
            ExpressionType.Equal => OperatorType.Exact,
            ExpressionType.ExclusiveOr => throw new NotSupportedException(
                "Bitwise operations are not supported."
            ),
            ExpressionType.GreaterThan => OperatorType.MoreThan,
            ExpressionType.GreaterThanOrEqual => OperatorType.MoreThanOrEqual,
            ExpressionType.LeftShift => throw new NotSupportedException(
                "Bitwise operations are not supported."
            ),
            ExpressionType.LessThan => OperatorType.LessThan,
            ExpressionType.LessThanOrEqual => OperatorType.LessThanOrEqual,
            ExpressionType.Modulo => OperatorType.Rem,
            ExpressionType.Multiply => OperatorType.Mul,
            ExpressionType.NotEqual => OperatorType.NotEqual,
            ExpressionType.Or => throw new NotSupportedException(
                "Bitwise operations are not supported."
            ),
            ExpressionType.OrElse => OperatorType.Or,
            ExpressionType.Power => OperatorType.Pow,
            ExpressionType.RightShift => throw new NotSupportedException(
                "Bitwise operations are not supported."
            ),
            ExpressionType.Subtract => OperatorType.Sub,
            _ => throw new NotSupportedException(
                $"The binary operator '{node.NodeType}' is not supported."
            ),
        };

        {
            var leftExpression = Visit(node.Left)?.ToValue();
            if (leftExpression is not { } leftValueExpression)
            {
                throw new InvalidCastException(
                    "The binary left operand is not a value expression."
                );
            }

            var rightExpression = Visit(node.Right)?.ToValue();
            if (rightExpression is not { } rightValueExpression)
            {
                throw new InvalidCastException(
                    "The binary right operand is not a value expression."
                );
            }

            return new BinaryValueExpression(
                leftValueExpression,
                new SimpleOperator(operatorType),
                rightValueExpression
            );
        }
    }

    protected override Expression VisitConstant(ConstantExpression node)
    {
        if (node.Value is null)
        {
            return new NullValueExpression();
        }

        if (node.Type.IsEnum)
        {
            return new StringValueExpression(node.Value.ToString()!);
        }

        if (node.Value is RecordId recordId)
        {
            return AppendRecordIdParameter(recordId);
        }

        return node.Value switch
        {
            None => new NoneValueExpression(),
            bool boolValue => new BoolValueExpression(boolValue),
            sbyte sbyteValue => new FormattedIntegerValueExpression(
                sbyteValue.ToString(CultureInfo.InvariantCulture)
            ),
            byte byteValue => new FormattedIntegerValueExpression(
                byteValue.ToString(CultureInfo.InvariantCulture)
            ),
            short shortValue => new FormattedIntegerValueExpression(
                shortValue.ToString(CultureInfo.InvariantCulture)
            ),
            ushort ushortValue => new FormattedIntegerValueExpression(
                ushortValue.ToString(CultureInfo.InvariantCulture)
            ),
            int intValue => new Int32ValueExpression(intValue),
            uint uintValue => new FormattedIntegerValueExpression(
                uintValue.ToString(CultureInfo.InvariantCulture)
            ),
            long longValue => new Int64ValueExpression(longValue),
            ulong ulongValue => new FormattedIntegerValueExpression(
                ulongValue.ToString(CultureInfo.InvariantCulture)
            ),
            float floatValue => new SingleValueExpression(floatValue),
            double doubleValue => new DoubleValueExpression(doubleValue),
            decimal dValue => new DecimalValueExpression(dValue),
            char charValue => new CharValueExpression(charValue),
            string stringValue => new StringValueExpression(stringValue),
            TimeSpan timeSpanValue => new DurationValueExpression(timeSpanValue),
            Duration durationValue => new DurationValueExpression(durationValue),
            DateTime dateTimeValue => new DateTimeValueExpression(dateTimeValue),
            Guid guidValue => new GuidValueExpression(guidValue),
            Vector2 vector2Value => new ArrayValueExpression(
                [
                    new SingleValueExpression(vector2Value.X),
                    new SingleValueExpression(vector2Value.Y),
                ]
            ),
            Vector3 vector3Value => new ArrayValueExpression(
                [
                    new SingleValueExpression(vector3Value.X),
                    new SingleValueExpression(vector3Value.Y),
                    new SingleValueExpression(vector3Value.Z),
                ]
            ),
            Vector4 vector4Value => new ArrayValueExpression(
                [
                    new SingleValueExpression(vector4Value.X),
                    new SingleValueExpression(vector4Value.Y),
                    new SingleValueExpression(vector4Value.Z),
                    new SingleValueExpression(vector4Value.W),
                ]
            ),
            ISpatial spatialValue => new GeometryValueExpression(spatialValue),
            _ => node,
        };
    }

    private ParameterValueExpression AppendRecordIdParameter(RecordId recordId)
    {
        var parameterName = $"_rid{_recordIdParameters.Count}";
        _recordIdParameters.Add(parameterName, recordId);

        return new ParameterValueExpression(parameterName);
    }

    protected override Expression VisitMember(MemberExpression node)
    {
        if (node.Member.DeclaringType == typeof(DateTime))
        {
            return BindDateTimeMember(node);
        }
        if (node.Member.DeclaringType == typeof(string))
        {
            return BindStringMember(node);
        }
        if (node.Member.DeclaringType == typeof(TimeSpan))
        {
            return BindTimeSpanMember(node);
        }
        if (
            typeof(Enumerable).IsAssignableFrom(node.Member.DeclaringType)
            || typeof(IEnumerable<>).IsAssignableFrom(node.Member.DeclaringType)
        )
        {
            return BindEnumerableMember(node);
        }
        if (
            node.Member.DeclaringType == typeof(Vector2)
            || node.Member.DeclaringType == typeof(Vector3)
            || node.Member.DeclaringType == typeof(Vector4)
        )
        {
            return BindVectorMember(node);
        }

        var source = Visit(node.Expression);
        var fieldName = ReflectionExtensions.GetDatabaseFieldName(node.Member);

        if (source is ParameterExpression parameterExpression)
        {
            // TODO : Apply correlation based on mapped intermediate parameters
            int parameterLevel = _parametersNestedLevels.GetValueOrDefault(
                parameterExpression,
                _currentNestedSelectLevel
            );

            var parentsParts = Enumerable
                .Range(0, _currentNestedSelectLevel - parameterLevel)
                .Select<int, PartExpression>(index =>
                    index == 0
                        ? new StartPartExpression(new ParameterValueExpression("parent"))
                        : new FieldPartExpression("$parent")
                );

            return new IdiomExpression([.. parentsParts, new FieldPartExpression(fieldName)]);
        }

        if (source is ValueExpression valueExpression)
        {
            return new IdiomExpression(
                [new StartPartExpression(valueExpression), new FieldPartExpression(fieldName)]
            );
        }

        if (source is IdiomExpression idiomExpression)
        {
            return new IdiomExpression(
                [.. idiomExpression.Parts, new FieldPartExpression(fieldName)]
            );
        }

        return new IdiomExpression([new FieldPartExpression(fieldName)]);
    }

    private Expression BindDateTimeMember(MemberExpression node)
    {
        if (
            node.Member.Name.Equals(nameof(DateTime.Now), StringComparison.Ordinal)
            || node.Member.Name.Equals(nameof(DateTime.UtcNow), StringComparison.Ordinal)
        )
        {
            return new FunctionValueExpression("time::now", []);
        }
        if (node.Member.Name.Equals(nameof(DateTime.UnixEpoch), StringComparison.Ordinal))
        {
            return new FunctionValueExpression("time::from::unix", [new Int32ValueExpression(0)]);
        }
        if (node.Member.Name.Equals(nameof(DateTime.Today), StringComparison.Ordinal))
        {
            return new FunctionValueExpression(
                "time::floor",
                [
                    new FunctionValueExpression("time::now", []),
                    new DurationValueExpression(TimeSpan.FromDays(1)),
                ]
            );
        }
        // Non static
        if (node.Member.Name.Equals(nameof(DateTime.Year), StringComparison.Ordinal))
        {
            var innerExpression = Visit(node.Expression);
            if (innerExpression is DateTimeValueExpression dateTimeValueExpression)
            {
                return new Int32ValueExpression(dateTimeValueExpression.Value.Year);
            }

            return TryChainDateTimeMethod(innerExpression, "year");
        }
        if (node.Member.Name.Equals(nameof(DateTime.Month), StringComparison.Ordinal))
        {
            var innerExpression = Visit(node.Expression);
            if (innerExpression is DateTimeValueExpression dateTimeValueExpression)
            {
                return new Int32ValueExpression(dateTimeValueExpression.Value.Month);
            }

            return TryChainDateTimeMethod(innerExpression, "month");
        }
        if (node.Member.Name.Equals(nameof(DateTime.Day), StringComparison.Ordinal))
        {
            var innerExpression = Visit(node.Expression);
            if (innerExpression is DateTimeValueExpression dateTimeValueExpression)
            {
                return new Int32ValueExpression(dateTimeValueExpression.Value.Day);
            }

            return TryChainDateTimeMethod(innerExpression, "day");
        }
        if (node.Member.Name.Equals(nameof(DateTime.Hour), StringComparison.Ordinal))
        {
            var innerExpression = Visit(node.Expression);
            if (innerExpression is DateTimeValueExpression dateTimeValueExpression)
            {
                return new Int32ValueExpression(dateTimeValueExpression.Value.Hour);
            }

            return TryChainDateTimeMethod(innerExpression, "hour");
        }
        if (node.Member.Name.Equals(nameof(DateTime.Minute), StringComparison.Ordinal))
        {
            var innerExpression = Visit(node.Expression);
            if (innerExpression is DateTimeValueExpression dateTimeValueExpression)
            {
                return new Int32ValueExpression(dateTimeValueExpression.Value.Minute);
            }

            return TryChainDateTimeMethod(innerExpression, "minute");
        }
        if (node.Member.Name.Equals(nameof(DateTime.Second), StringComparison.Ordinal))
        {
            var innerExpression = Visit(node.Expression);
            if (innerExpression is DateTimeValueExpression dateTimeValueExpression)
            {
                return new Int32ValueExpression(dateTimeValueExpression.Value.Second);
            }

            return TryChainDateTimeMethod(innerExpression, "second");
        }
        if (node.Member.Name.Equals(nameof(DateTime.DayOfYear), StringComparison.Ordinal))
        {
            var innerExpression = Visit(node.Expression);
            if (innerExpression is DateTimeValueExpression dateTimeValueExpression)
            {
                return new Int32ValueExpression(dateTimeValueExpression.Value.DayOfYear);
            }

            return TryChainDateTimeMethod(innerExpression, "yday");
        }

        return base.VisitMember(node);
    }

    private static IdiomExpression TryChainDateTimeMethod(
        Expression? innerExpression,
        string functionName
    )
    {
        var innerValueExpression = innerExpression?.ToValue();
        if (innerValueExpression is null)
        {
            throw new InvalidCastException($"Cannot cast null to {nameof(DateTime)}");
        }
        return IdiomExpression.Chain(
            new StartPartExpression(innerValueExpression),
            new MethodPartExpression(functionName)
        );
    }

    private Expression BindStringMember(MemberExpression node)
    {
        if (node.Member.Name.Equals(nameof(string.Empty), StringComparison.Ordinal))
        {
            return new StringValueExpression(string.Empty);
        }
        if (node.Member.Name.Equals(nameof(string.Length), StringComparison.Ordinal))
        {
            var innerExpression = Visit(node.Expression);
            if (innerExpression is StringValueExpression stringValueExpression)
            {
                return new Int32ValueExpression(stringValueExpression.Value.Length);
            }

            var innerValueExpression = innerExpression?.ToValue();
            if (innerValueExpression is null)
            {
                throw new InvalidCastException("Cannot cast null to string");
            }
            return new FunctionValueExpression("string::len", [innerValueExpression]);
        }

        return base.VisitMember(node);
    }

    private Expression BindTimeSpanMember(MemberExpression node)
    {
        if (node.Member.Name.Equals(nameof(TimeSpan.Zero), StringComparison.Ordinal))
        {
            return new DurationValueExpression(Duration.Zero);
        }
        // Compile-time execution
        if (node.Member.Name.Equals(nameof(TimeSpan.Days), StringComparison.Ordinal))
        {
            var innerExpression = Visit(node.Expression);
            if (
                innerExpression is DurationValueExpression
                {
                    OriginalValue: not null
                } durationValueExpression
            )
            {
                return new Int32ValueExpression(durationValueExpression.OriginalValue.Value.Days);
            }
        }
        if (node.Member.Name.Equals(nameof(TimeSpan.Hours), StringComparison.Ordinal))
        {
            var innerExpression = Visit(node.Expression);
            if (
                innerExpression is DurationValueExpression
                {
                    OriginalValue: not null
                } durationValueExpression
            )
            {
                return new Int32ValueExpression(durationValueExpression.OriginalValue.Value.Hours);
            }
        }
        if (node.Member.Name.Equals(nameof(TimeSpan.Minutes), StringComparison.Ordinal))
        {
            var innerExpression = Visit(node.Expression);
            if (
                innerExpression is DurationValueExpression
                {
                    OriginalValue: not null
                } durationValueExpression
            )
            {
                return new Int32ValueExpression(
                    durationValueExpression.OriginalValue.Value.Minutes
                );
            }
        }
        if (node.Member.Name.Equals(nameof(TimeSpan.Seconds), StringComparison.Ordinal))
        {
            var innerExpression = Visit(node.Expression);
            if (
                innerExpression is DurationValueExpression
                {
                    OriginalValue: not null
                } durationValueExpression
            )
            {
                return new Int32ValueExpression(
                    durationValueExpression.OriginalValue.Value.Seconds
                );
            }
        }
        if (node.Member.Name.Equals(nameof(TimeSpan.Milliseconds), StringComparison.Ordinal))
        {
            var innerExpression = Visit(node.Expression);
            if (
                innerExpression is DurationValueExpression
                {
                    OriginalValue: not null
                } durationValueExpression
            )
            {
                return new Int32ValueExpression(
                    durationValueExpression.OriginalValue.Value.Milliseconds
                );
            }
        }
#if NET7_0_OR_GREATER
        if (node.Member.Name.Equals(nameof(TimeSpan.Microseconds), StringComparison.Ordinal))
        {
            var innerExpression = Visit(node.Expression);
            if (
                innerExpression is DurationValueExpression
                {
                    OriginalValue: not null
                } durationValueExpression
            )
            {
                return new Int32ValueExpression(
                    durationValueExpression.OriginalValue.Value.Microseconds
                );
            }
        }
        if (node.Member.Name.Equals(nameof(TimeSpan.Nanoseconds), StringComparison.Ordinal))
        {
            var innerExpression = Visit(node.Expression);
            if (
                innerExpression is DurationValueExpression
                {
                    OriginalValue: not null
                } durationValueExpression
            )
            {
                return new Int32ValueExpression(
                    durationValueExpression.OriginalValue.Value.Nanoseconds
                );
            }
        }
#endif
        if (node.Member.Name.Equals(nameof(TimeSpan.Ticks), StringComparison.Ordinal))
        {
            var innerExpression = Visit(node.Expression);
            if (
                innerExpression is DurationValueExpression
                {
                    OriginalValue: not null
                } durationValueExpression
            )
            {
                return new Int64ValueExpression(durationValueExpression.OriginalValue.Value.Ticks);
            }
        }
        // Non static
        if (node.Member.Name.Equals(nameof(TimeSpan.TotalDays), StringComparison.Ordinal))
        {
            var valueExpression = Visit(node.Expression)?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    "The caller of TimeSpan.TotalDays must be a value expression."
                );
            }
            return new FunctionValueExpression("duration::days", [valueExpression]);
        }
        if (node.Member.Name.Equals(nameof(TimeSpan.TotalHours), StringComparison.Ordinal))
        {
            var valueExpression = Visit(node.Expression)?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    "The caller of TimeSpan.TotalHours must be a value expression."
                );
            }
            return new FunctionValueExpression("duration::hours", [valueExpression]);
        }
        if (node.Member.Name.Equals(nameof(TimeSpan.TotalMinutes), StringComparison.Ordinal))
        {
            var valueExpression = Visit(node.Expression)?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    "The caller of TimeSpan.TotalMinutes must be a value expression."
                );
            }
            return new FunctionValueExpression("duration::mins", [valueExpression]);
        }
        if (node.Member.Name.Equals(nameof(TimeSpan.TotalSeconds), StringComparison.Ordinal))
        {
            var valueExpression = Visit(node.Expression)?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    "The caller of TimeSpan.TotalDays must be a value expression."
                );
            }
            return new FunctionValueExpression("duration::secs", [valueExpression]);
        }
        if (node.Member.Name.Equals(nameof(TimeSpan.TotalMilliseconds), StringComparison.Ordinal))
        {
            var valueExpression = Visit(node.Expression)?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    "The caller of TimeSpan.TotalMilliseconds must be a value expression."
                );
            }
            return new FunctionValueExpression("duration::millis", [valueExpression]);
        }
#if NET7_0_OR_GREATER
        if (node.Member.Name.Equals(nameof(TimeSpan.TotalMicroseconds), StringComparison.Ordinal))
        {
            var valueExpression = Visit(node.Expression)?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    "The caller of TimeSpan.TotalMicroseconds must be a value expression."
                );
            }
            return new FunctionValueExpression("duration::micros", [valueExpression]);
        }
        if (node.Member.Name.Equals(nameof(TimeSpan.TotalNanoseconds), StringComparison.Ordinal))
        {
            var valueExpression = Visit(node.Expression)?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    "The caller of TimeSpan.TotalNanoseconds must be a value expression."
                );
            }
            return new FunctionValueExpression("duration::nanos", [valueExpression]);
        }
#endif
        return base.VisitMember(node);
    }

    private Expression BindEnumerableMember(MemberExpression node)
    {
        if (node.Member.Name.Equals(nameof(IList.Count), StringComparison.Ordinal))
        {
            var valueExpression = Visit(node.Expression)?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    $"The caller of {node.Member.DeclaringType!.Name}.Count must be a value expression."
                );
            }
            return new FunctionValueExpression("array::len", [valueExpression]);
        }

        return base.VisitMember(node);
    }

    private Expression BindVectorMember(MemberExpression node)
    {
        string declaringTypeName = node.Member.DeclaringType!.Name;
        int vectorLength = declaringTypeName switch
        {
            nameof(Vector2) => 2,
            nameof(Vector3) => 3,
            nameof(Vector4) => 4,
            _ => throw new InvalidCastException("Cannot match this type of Vector"),
        };

        if (node.Member.Name.Equals(nameof(Vector2.Zero), StringComparison.Ordinal))
        {
            return new ArrayValueExpression(
                [.. Enumerable.Repeat(new SingleValueExpression(0), vectorLength)]
            );
        }
        if (node.Member.Name.Equals(nameof(Vector2.One), StringComparison.Ordinal))
        {
            return new ArrayValueExpression(
                [.. Enumerable.Repeat(new SingleValueExpression(1), vectorLength)]
            );
        }
        if (node.Member.Name.Equals(nameof(Vector2.UnitX), StringComparison.Ordinal))
        {
            return new ArrayValueExpression(
                [
                    new SingleValueExpression(1),
                    .. Enumerable.Repeat(new SingleValueExpression(0), vectorLength - 1),
                ]
            );
        }
        if (node.Member.Name.Equals(nameof(Vector2.UnitY), StringComparison.Ordinal))
        {
            return new ArrayValueExpression(
                [
                    new SingleValueExpression(0),
                    new SingleValueExpression(1),
                    .. Enumerable.Repeat(new SingleValueExpression(0), vectorLength - 2),
                ]
            );
        }
        if (node.Member.Name.Equals(nameof(Vector3.UnitZ), StringComparison.Ordinal))
        {
            return new ArrayValueExpression(
                [
                    new SingleValueExpression(0),
                    new SingleValueExpression(0),
                    new SingleValueExpression(1),
                    .. Enumerable.Repeat(new SingleValueExpression(0), vectorLength - 3),
                ]
            );
        }
        if (node.Member.Name.Equals(nameof(Vector4.UnitW), StringComparison.Ordinal))
        {
            return new ArrayValueExpression(
                [
                    new SingleValueExpression(0),
                    new SingleValueExpression(0),
                    new SingleValueExpression(0),
                    new SingleValueExpression(1),
                ]
            );
        }
        // Non static
        if (node.Member.Name.Equals(nameof(Vector2.X), StringComparison.Ordinal))
        {
            const int index = 0;
            return BindUnitVectorMember(node, index);
        }
        if (node.Member.Name.Equals(nameof(Vector2.Y), StringComparison.Ordinal))
        {
            const int index = 1;
            return BindUnitVectorMember(node, index);
        }
        if (node.Member.Name.Equals(nameof(Vector3.Z), StringComparison.Ordinal))
        {
            const int index = 2;
            return BindUnitVectorMember(node, index);
        }
        if (node.Member.Name.Equals(nameof(Vector4.W), StringComparison.Ordinal))
        {
            const int index = 3;
            return BindUnitVectorMember(node, index);
        }

        return base.VisitMember(node);
    }

    private Expression BindUnitVectorMember(MemberExpression node, int index)
    {
        var innerExpression = Visit(node.Expression);
        if (innerExpression is ArrayValueExpression arrayValueExpression)
        {
            return arrayValueExpression.Values[index];
        }

        var innerValueExpression = innerExpression?.ToValue();
        if (innerValueExpression is null)
        {
            throw new InvalidCastException(
                $"Cannot cast null to {node.Member.DeclaringType!.Name}"
            );
        }
        return IdiomExpression.Chain(
            new StartPartExpression(innerValueExpression),
            new IndexPartExpression(index)
        );
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        // TODO : Dictionary?
        if (node.Method.DeclaringType == typeof(string))
        {
            return BindStringMethodCall(node);
        }
        if (node.Method.DeclaringType == typeof(Math))
        {
            return BindMathMethodCall(node);
        }
        if (node.Method.DeclaringType == typeof(DateTime))
        {
            return BindDateTimeMethodCall(node);
        }
        if (node.Method.DeclaringType == typeof(TimeSpan))
        {
            return BindTimeSpanMethodCall(node);
        }
        if (
            typeof(Enumerable).IsAssignableFrom(node.Method.DeclaringType)
            || typeof(IEnumerable<>).IsAssignableFrom(node.Method.DeclaringType)
        )
        {
            return BindEnumerableMethodCall(node);
        }
        if (node.Method.DeclaringType == typeof(Array))
        {
            return BindArrayMethodCall(node);
        }
        if (node.Method.DeclaringType == typeof(Regex))
        {
            return BindRegexMethodCall(node);
        }
        if (
            node.Method.DeclaringType == typeof(Vector2)
            || node.Method.DeclaringType == typeof(Vector3)
            || node.Method.DeclaringType == typeof(Vector4)
        )
        {
            return BindVectorMethodCall(node);
        }

        return base.VisitMethodCall(node);
    }

    private Expression BindStringMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name.Equals(nameof(string.Concat), StringComparison.Ordinal))
        {
            ImmutableArray<ValueExpression> valueExpressions;

            if (node.Arguments.Count == 1)
            {
                // When 1st and only arg is IEnumerable<string>
                var innerValueExpression = Visit(node.Arguments[0])?.ToValue();
                if (innerValueExpression is ArrayValueExpression arrayValueExpression)
                {
                    valueExpressions = arrayValueExpression.Values;
                }
                else
                {
                    throw new InvalidCastException(
                        "The first argument of string.Concat cannot be cast to an array of value expressions."
                    );
                }
            }
            else
            {
                valueExpressions =
                [
                    .. node.Arguments.Select(
                        (arg, index) =>
                        {
                            var valueExpression = Visit(arg)?.ToValue();
                            if (valueExpression is null)
                            {
                                string argNumber = index switch
                                {
                                    0 => "first",
                                    1 => "second",
                                    2 => "third",
                                    _ => $"{(index + 1).ToString(CultureInfo.InvariantCulture)}nth",
                                };
                                throw new InvalidCastException(
                                    $"The {argNumber} argument of string.Concat must be a value expression."
                                );
                            }

                            return valueExpression;
                        }
                    ),
                ];
            }

            return new FunctionValueExpression("string::concat", valueExpressions);
        }
        if (
            node.Method.Name.Equals(nameof(string.Contains), StringComparison.Ordinal)
            && node.Arguments.Count == 1
        )
        {
            var valueExpression1 = Visit(node.Object)?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    "The caller of string.Contains must be a value expression."
                );
            }
            var valueExpression2 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    "The first argument of string.Contains must be a value expression."
                );
            }
            return new FunctionValueExpression(
                "string::contains",
                [valueExpression1, valueExpression2]
            );
        }
        if (
            node.Method.Name.Equals(nameof(string.EndsWith), StringComparison.Ordinal)
            && node.Arguments.Count == 1
        )
        {
            var valueExpression1 = Visit(node.Object)?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    "The caller of string.EndsWith must be a value expression."
                );
            }
            var valueExpression2 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    "The first argument of string.EndsWith must be a value expression."
                );
            }
            return new FunctionValueExpression(
                "string::ends_with",
                [valueExpression1, valueExpression2]
            );
        }
        if (node.Method.Name.Equals(nameof(string.IsNullOrEmpty), StringComparison.Ordinal))
        {
            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    "The first argument of string.IsNullOrEmpty must be a value expression."
                );
            }
            return UnaryValueExpression.Not(valueExpression);
        }
        if (node.Method.Name.Equals(nameof(string.IsNullOrWhiteSpace), StringComparison.Ordinal))
        {
            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    "The first argument of string.IsNullOrWhiteSpace must be a value expression."
                );
            }

            var hasWordsFunction = new FunctionValueExpression("string::words", [valueExpression]);
            return BinaryValueExpression.Or(
                UnaryValueExpression.Not(valueExpression),
                UnaryValueExpression.Not(hasWordsFunction)
            );
        }
        if (
            node.Method.Name.Equals(nameof(string.Join), StringComparison.Ordinal)
            && node.Arguments.Count == 2
        )
        {
            var valueExpression1 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    $"The first argument of {node.Method.DeclaringType!.Name}.Join must be a value expression."
                );
            }
            var valueExpression2 = Visit(node.Arguments[1])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    $"The second argument of {node.Method.DeclaringType!.Name}.Join must be a value expression."
                );
            }
            return new FunctionValueExpression("array::join", [valueExpression2, valueExpression1]);
        }
        if (
            node.Method.Name.Equals(nameof(string.Replace), StringComparison.Ordinal)
            && node.Arguments.Count == 2
        )
        {
            var valueExpression1 = Visit(node.Object)?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    "The caller of string.Replace must be a value expression."
                );
            }
            var valueExpression2 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    "The first argument of string.Replace must be a value expression."
                );
            }
            var valueExpression3 = Visit(node.Arguments[1])?.ToValue();
            if (valueExpression3 is null)
            {
                throw new InvalidCastException(
                    "The second argument of string.Replace must be a value expression."
                );
            }
            return new FunctionValueExpression(
                "string::replace",
                [valueExpression1, valueExpression2, valueExpression3]
            );
        }
        if (
            node.Method.Name.Equals(nameof(string.Split), StringComparison.Ordinal)
            && node.Arguments.Count == 2
        )
        {
            var valueExpression1 = Visit(node.Object)?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    "The caller of string.Split must be a value expression."
                );
            }
            var valueExpression2 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    "The first argument of string.Split must be a value expression."
                );
            }
            var options = node.Arguments[1].ExtractConstant<StringSplitOptions>();
            if (options != StringSplitOptions.None)
            {
                throw new InvalidCastException(
                    "The second argument of string.Split must be StringSplitOptions.None."
                );
            }
            return new FunctionValueExpression(
                "string::split",
                [valueExpression1, valueExpression2]
            );
        }
        if (
            node.Method.Name.Equals(nameof(string.StartsWith), StringComparison.Ordinal)
            && node.Arguments.Count == 1
        )
        {
            var valueExpression1 = Visit(node.Object)?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    "The caller of string.StartsWith must be a value expression."
                );
            }
            var valueExpression2 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    "The first argument of string.StartsWith must be a value expression."
                );
            }
            return new FunctionValueExpression(
                "string::starts_with",
                [valueExpression1, valueExpression2]
            );
        }
        if (node.Method.Name.Equals(nameof(string.Substring), StringComparison.Ordinal))
        {
            var valueExpressions = node
                .Arguments.Select(
                    (arg, index) =>
                    {
                        var valueExpression = Visit(arg)?.ToValue();
                        if (valueExpression is null)
                        {
                            string argNumber = index switch
                            {
                                0 => "first",
                                1 => "second",
                                2 => "third",
                                _ => $"{(index + 1).ToString(CultureInfo.InvariantCulture)}nth",
                            };
                            throw new InvalidCastException(
                                $"The {argNumber} argument of string.Substring must be a value expression."
                            );
                        }

                        return valueExpression;
                    }
                )
                .ToImmutableArray();
            return new FunctionValueExpression("string::slice", valueExpressions);
        }
        if (
            node.Method.Name.Equals(nameof(string.Trim), StringComparison.Ordinal)
            && node.Arguments.Count == 0
        )
        {
            var valueExpression = Visit(node.Object)?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    "The caller of string.Trim must be a value expression."
                );
            }
            return new FunctionValueExpression("string::trim", [valueExpression]);
        }
        if (
            node.Method.Name.Equals(nameof(string.ToLower), StringComparison.Ordinal)
            && node.Arguments.Count == 0
        )
        {
            var valueExpression = Visit(node.Object)?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    "The caller of string.ToLower must be a value expression."
                );
            }
            return new FunctionValueExpression("string::lowercase", [valueExpression]);
        }
        if (
            node.Method.Name.Equals(nameof(string.ToUpper), StringComparison.Ordinal)
            && node.Arguments.Count == 0
        )
        {
            var valueExpression = Visit(node.Object)?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    "The caller of string.ToUpper must be a value expression."
                );
            }
            return new FunctionValueExpression("string::uppercase", [valueExpression]);
        }

        return base.VisitMethodCall(node);
    }

    private Expression BindMathMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name.Equals(nameof(Math.Abs), StringComparison.Ordinal))
        {
            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    "The first argument of Math.Abs must be a value expression."
                );
            }
            return new FunctionValueExpression("math::abs", [valueExpression]);
        }
        if (node.Method.Name.Equals(nameof(Math.Acos), StringComparison.Ordinal))
        {
            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    "The first argument of Math.Acos must be a value expression."
                );
            }
            return new FunctionValueExpression("math::acos", [valueExpression]);
        }
        if (node.Method.Name.Equals(nameof(Math.Asin), StringComparison.Ordinal))
        {
            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    "The first argument of Math.Asin must be a value expression."
                );
            }
            return new FunctionValueExpression("math::asin", [valueExpression]);
        }
        if (node.Method.Name.Equals(nameof(Math.Atan), StringComparison.Ordinal))
        {
            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    "The first argument of Math.Atan must be a value expression."
                );
            }
            return new FunctionValueExpression("math::atan", [valueExpression]);
        }
        if (node.Method.Name.Equals(nameof(Math.Ceiling), StringComparison.Ordinal))
        {
            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    "The first argument of Math.Ceiling must be a value expression."
                );
            }
            return new FunctionValueExpression("math::ceil", [valueExpression]);
        }
        if (node.Method.Name.Equals(nameof(Math.Clamp), StringComparison.Ordinal))
        {
            var valueExpression1 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    "The first argument of Math.Clamp must be a value expression."
                );
            }
            var valueExpression2 = Visit(node.Arguments[1])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    "The second argument of Math.Clamp must be a value expression."
                );
            }
            var valueExpression3 = Visit(node.Arguments[2])?.ToValue();
            if (valueExpression3 is null)
            {
                throw new InvalidCastException(
                    "The third argument of Math.Clamp must be a value expression."
                );
            }
            return new FunctionValueExpression(
                "math::clamp",
                [valueExpression1, valueExpression2, valueExpression3]
            );
        }
        if (node.Method.Name.Equals(nameof(Math.Cos), StringComparison.Ordinal))
        {
            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    "The first argument of Math.Cos must be a value expression."
                );
            }
            return new FunctionValueExpression("math::cos", [valueExpression]);
        }
        if (node.Method.Name.Equals(nameof(Math.Floor), StringComparison.Ordinal))
        {
            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    "The first argument of Math.Floor must be a value expression."
                );
            }
            return new FunctionValueExpression("math::floor", [valueExpression]);
        }
        if (node.Method.Name.Equals(nameof(Math.Log), StringComparison.Ordinal))
        {
            var valueExpression1 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    "The first argument of Math.Log must be a value expression."
                );
            }

            if (node.Arguments.Count > 1)
            {
                var valueExpression2 = Visit(node.Arguments[1])?.ToValue();
                if (valueExpression2 is null)
                {
                    throw new InvalidCastException(
                        "The second argument of Math.Log must be a value expression."
                    );
                }
                return new FunctionValueExpression(
                    "math::log",
                    [valueExpression1, valueExpression2]
                );
            }
            return new FunctionValueExpression("math::log", [valueExpression1]);
        }
#if NET5_0_OR_GREATER
        if (node.Method.Name.Equals(nameof(Math.Log2), StringComparison.Ordinal))
        {
            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    "The first argument of Math.Log2 must be a value expression."
                );
            }
            return new FunctionValueExpression("math::log2", [valueExpression]);
        }
#endif
        if (node.Method.Name.Equals(nameof(Math.Log10), StringComparison.Ordinal))
        {
            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    "The first argument of Math.Log10 must be a value expression."
                );
            }
            return new FunctionValueExpression("math::log10", [valueExpression]);
        }
        if (node.Method.Name.Equals(nameof(Math.Max), StringComparison.Ordinal))
        {
            var valueExpression1 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    "The first argument of Math.Max must be a value expression."
                );
            }
            var valueExpression2 = Visit(node.Arguments[1])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    "The second argument of Math.Max must be a value expression."
                );
            }
            return new FunctionValueExpression("math::max", [valueExpression1, valueExpression2]);
        }
        if (node.Method.Name.Equals(nameof(Math.Min), StringComparison.Ordinal))
        {
            var valueExpression1 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    "The first argument of Math.Min must be a value expression."
                );
            }
            var valueExpression2 = Visit(node.Arguments[1])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    "The second argument of Math.Min must be a value expression."
                );
            }
            return new FunctionValueExpression("math::min", [valueExpression1, valueExpression2]);
        }
        if (node.Method.Name.Equals(nameof(Math.Pow), StringComparison.Ordinal))
        {
            var valueExpression1 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    "The first argument of Math.Pow must be a value expression."
                );
            }
            var valueExpression2 = Visit(node.Arguments[1])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    "The second argument of Math.Pow must be a value expression."
                );
            }
            return new FunctionValueExpression("math::pow", [valueExpression1, valueExpression2]);
        }
        if (
            node.Method.Name.Equals(nameof(Math.Round), StringComparison.Ordinal)
            && node.Arguments.Count == 1
        )
        {
            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    "The first argument of Math.Round must be a value expression."
                );
            }
            return new FunctionValueExpression("math::round", [valueExpression]);
        }
        if (node.Method.Name.Equals(nameof(Math.Sign), StringComparison.Ordinal))
        {
            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    "The first argument of Math.Sign must be a value expression."
                );
            }
            return new FunctionValueExpression("math::sign", [valueExpression]);
        }
        if (node.Method.Name.Equals(nameof(Math.Sin), StringComparison.Ordinal))
        {
            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    "The first argument of Math.Sin must be a value expression."
                );
            }
            return new FunctionValueExpression("math::sin", [valueExpression]);
        }
        if (node.Method.Name.Equals(nameof(Math.Sqrt), StringComparison.Ordinal))
        {
            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    "The first argument of Math.Sqrt must be a value expression."
                );
            }
            return new FunctionValueExpression("math::sqrt", [valueExpression]);
        }
        if (node.Method.Name.Equals(nameof(Math.Tan), StringComparison.Ordinal))
        {
            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    "The first argument of Math.Tan must be a value expression."
                );
            }
            return new FunctionValueExpression("math::tan", [valueExpression]);
        }

        return base.VisitMethodCall(node);
    }

    private Expression BindDateTimeMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name.Equals(nameof(DateTime.Add), StringComparison.Ordinal))
        {
            var valueExpression1 = Visit(node.Object)?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    "The first argument of DateTime.Add must be a value expression."
                );
            }
            var valueExpression2 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    "The second argument of DateTime.Add must be a value expression."
                );
            }
            return BinaryValueExpression.Add(valueExpression1, valueExpression2);
        }
        if (node.Method.Name.Equals(nameof(DateTime.Subtract), StringComparison.Ordinal))
        {
            var valueExpression1 = Visit(node.Object)?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    "The first argument of DateTime.Subtract must be a value expression."
                );
            }
            var valueExpression2 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    "The second argument of DateTime.Subtract must be a value expression."
                );
            }
            return BinaryValueExpression.Sub(valueExpression1, valueExpression2);
        }

        return base.VisitMethodCall(node);
    }

    private Expression BindTimeSpanMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name.Equals(nameof(TimeSpan.FromDays), StringComparison.Ordinal))
        {
            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            return valueExpression switch
            {
                null => throw new InvalidCastException(
                    "The first argument of TimeSpan.FromDays must be a value expression."
                ),
                Int32ValueExpression int32ValueExpression => new DurationValueExpression(
                    TimeSpan.FromDays(int32ValueExpression.Value)
                ),
                FloatValueExpression floatValueExpression => new DurationValueExpression(
                    TimeSpan.FromDays(floatValueExpression.ToInnerValue())
                ),
                _ => new FunctionValueExpression("duration::from::days", [valueExpression]),
            };
        }
        if (node.Method.Name.Equals(nameof(TimeSpan.FromHours), StringComparison.Ordinal))
        {
            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            return valueExpression switch
            {
                null => throw new InvalidCastException(
                    "The first argument of TimeSpan.FromHours must be a value expression."
                ),
                Int32ValueExpression int32ValueExpression => new DurationValueExpression(
                    TimeSpan.FromHours(int32ValueExpression.Value)
                ),
                FloatValueExpression floatValueExpression => new DurationValueExpression(
                    TimeSpan.FromHours(floatValueExpression.ToInnerValue())
                ),
                _ => new FunctionValueExpression("duration::from::hours", [valueExpression]),
            };
        }
        if (node.Method.Name.Equals(nameof(TimeSpan.FromMinutes), StringComparison.Ordinal))
        {
            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            return valueExpression switch
            {
                null => throw new InvalidCastException(
                    "The first argument of TimeSpan.FromMinutes must be a value expression."
                ),
                Int64ValueExpression int64ValueExpression => new DurationValueExpression(
                    TimeSpan.FromMinutes(int64ValueExpression.Value)
                ),
                FloatValueExpression floatValueExpression => new DurationValueExpression(
                    TimeSpan.FromMinutes(floatValueExpression.ToInnerValue())
                ),
                _ => new FunctionValueExpression("duration::from::mins", [valueExpression]),
            };
        }
        if (node.Method.Name.Equals(nameof(TimeSpan.FromSeconds), StringComparison.Ordinal))
        {
            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            return valueExpression switch
            {
                null => throw new InvalidCastException(
                    "The first argument of TimeSpan.FromSeconds must be a value expression."
                ),
                Int64ValueExpression int64ValueExpression => new DurationValueExpression(
                    TimeSpan.FromSeconds(int64ValueExpression.Value)
                ),
                FloatValueExpression floatValueExpression => new DurationValueExpression(
                    TimeSpan.FromSeconds(floatValueExpression.ToInnerValue())
                ),
                _ => new FunctionValueExpression("duration::from::secs", [valueExpression]),
            };
        }
        if (node.Method.Name.Equals(nameof(TimeSpan.FromMilliseconds), StringComparison.Ordinal))
        {
            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            return valueExpression switch
            {
                null => throw new InvalidCastException(
                    "The first argument of TimeSpan.FromMilliseconds must be a value expression."
                ),
                Int64ValueExpression int64ValueExpression => new DurationValueExpression(
                    TimeSpan.FromMilliseconds(int64ValueExpression.Value)
                ),
                FloatValueExpression floatValueExpression => new DurationValueExpression(
                    TimeSpan.FromMilliseconds(floatValueExpression.ToInnerValue())
                ),
                _ => new FunctionValueExpression("duration::from::millis", [valueExpression]),
            };
        }
#if NET7_0_OR_GREATER
        if (node.Method.Name.Equals(nameof(TimeSpan.FromMicroseconds), StringComparison.Ordinal))
        {
            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            return valueExpression switch
            {
                null => throw new InvalidCastException(
                    "The first argument of TimeSpan.FromMicroseconds must be a value expression."
                ),
                Int64ValueExpression int64ValueExpression => new DurationValueExpression(
                    TimeSpan.FromMicroseconds(int64ValueExpression.Value)
                ),
                FloatValueExpression floatValueExpression => new DurationValueExpression(
                    TimeSpan.FromMicroseconds(floatValueExpression.ToInnerValue())
                ),
                _ => new FunctionValueExpression("duration::from::micros", [valueExpression]),
            };
        }
#endif
        if (node.Method.Name.Equals(nameof(TimeSpan.FromTicks), StringComparison.Ordinal))
        {
            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    "The first argument of TimeSpan.FromTicks must be a value expression."
                );
            }
            var fromTicksToNanosExpression = new BinaryValueExpression(
                new FormattedIntegerValueExpression(
                    TimeConstants.NanosecondsPerTick.ToString(CultureInfo.InvariantCulture)
                ),
                new SimpleOperator(OperatorType.Mul),
                valueExpression
            );
            return new FunctionValueExpression(
                "duration::from::nanos",
                [fromTicksToNanosExpression]
            );
        }
        if (node.Method.Name.Equals(nameof(TimeSpan.Add), StringComparison.Ordinal))
        {
            var valueExpression1 = Visit(node.Object)?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    "The first argument of TimeSpan.Add must be a value expression."
                );
            }
            var valueExpression2 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    "The second argument of TimeSpan.Add must be a value expression."
                );
            }
            return BinaryValueExpression.Add(valueExpression1, valueExpression2);
        }
        if (node.Method.Name.Equals(nameof(TimeSpan.Subtract), StringComparison.Ordinal))
        {
            var valueExpression1 = Visit(node.Object)?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    "The first argument of TimeSpan.Subtract must be a value expression."
                );
            }
            var valueExpression2 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    "The second argument of TimeSpan.Subtract must be a value expression."
                );
            }
            return BinaryValueExpression.Sub(valueExpression1, valueExpression2);
        }

        return base.VisitMethodCall(node);
    }

    private Expression BindEnumerableMethodCall(MethodCallExpression node)
    {
        bool isFirstParamLambdaParameter =
            node.Arguments is [ParameterExpression parameter, ..]
            && _sourceExpressionParameters.ContainsKey(parameter);

        // TODO : OrderBy, Where, etc... -- Implement Closures
        if (
            node.Method.Name.Equals(nameof(Enumerable.Any), StringComparison.Ordinal)
            && node.Arguments.Count == 1
        )
        {
            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    $"The first argument of {node.Method.DeclaringType!.Name}.Any must be a value expression."
                );
            }
            // TODO : second arg?
            return UnaryValueExpression.Not(
                new FunctionValueExpression("array::is_empty", [valueExpression])
            );
        }
        if (node.Method.Name.Equals(nameof(Enumerable.Append), StringComparison.Ordinal))
        {
            var valueExpression1 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    $"The first argument of {node.Method.DeclaringType!.Name}.Append must be a value expression."
                );
            }
            var valueExpression2 = Visit(node.Arguments[1])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    $"The second argument of {node.Method.DeclaringType!.Name}.Append must be a value expression."
                );
            }
            return new FunctionValueExpression(
                "array::append",
                [valueExpression1, valueExpression2]
            );
        }
        if (node.Method.Name.Equals(nameof(Enumerable.Average), StringComparison.Ordinal))
        {
            if (node.Arguments.Count == 1)
            {
                if (isFirstParamLambdaParameter)
                {
                    return new FunctionValueExpression(
                        "math::mean",
                        [new ParameterValueExpression("this")]
                    );
                }

                var valueExpression1 = Visit(node.Arguments[0])?.ToValue();
                if (valueExpression1 is null)
                {
                    throw new InvalidCastException(
                        $"The first argument of {node.Method.DeclaringType!.Name}.Average must be a value expression."
                    );
                }
                return new FunctionValueExpression("math::mean", [valueExpression1]);
            }

            var valueExpression2 = Visit(node.Arguments[1])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    $"The second argument of {node.Method.DeclaringType!.Name}.Average must be a value expression."
                );
            }
            return new FunctionValueExpression("math::mean", [valueExpression2]);
        }
#if NET7_0_OR_GREATER
        if (node.Method.Name.Equals(nameof(Enumerable.Chunk), StringComparison.Ordinal))
        {
            var valueExpression1 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    $"The first argument of {node.Method.DeclaringType!.Name}.Append must be a value expression."
                );
            }
            var valueExpression2 = Visit(node.Arguments[1])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    $"The second argument of {node.Method.DeclaringType!.Name}.Append must be a value expression."
                );
            }
            return new FunctionValueExpression(
                "array::clump",
                [valueExpression1, valueExpression2]
            );
        }
#endif
        if (node.Method.Name.Equals(nameof(Enumerable.Concat), StringComparison.Ordinal))
        {
            var valueExpression1 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    $"The first argument of {node.Method.DeclaringType!.Name}.Concat must be a value expression."
                );
            }
            var valueExpression2 = Visit(node.Arguments[1])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    $"The second argument of {node.Method.DeclaringType!.Name}.Concat must be a value expression."
                );
            }
            return new FunctionValueExpression(
                "array::concat",
                [valueExpression1, valueExpression2]
            );
        }
        if (node.Method.Name.Equals(nameof(Enumerable.Contains), StringComparison.Ordinal))
        {
            var valueExpression1 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    $"The first argument of {node.Method.DeclaringType!.Name}.Contains must be a value expression."
                );
            }
            var valueExpression2 = Visit(node.Arguments[1])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    $"The second argument of {node.Method.DeclaringType!.Name}.Contains must be a value expression."
                );
            }
            return BinaryValueExpression.Contains(valueExpression1, valueExpression2);
        }
        if (
            node.Method.Name.Equals(nameof(Enumerable.Count), StringComparison.Ordinal)
            && node.Arguments.Count == 1
        )
        {
            if (isFirstParamLambdaParameter)
            {
                return new FunctionValueExpression("count", []);
            }

            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    $"The first argument of {node.Method.DeclaringType!.Name}.Count must be a value expression."
                );
            }
            // TODO : second arg?
            return new FunctionValueExpression("array::len", [valueExpression]);
        }
        if (
            node.Method.Name.Equals(nameof(Enumerable.Distinct), StringComparison.Ordinal)
            && node.Arguments.Count == 1
        )
        {
            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    $"The first argument of {node.Method.DeclaringType!.Name}.Distinct must be a value expression."
                );
            }
            return new FunctionValueExpression("array::distinct", [valueExpression]);
        }
        if (
            node.Method.Name.Equals(nameof(Enumerable.ElementAtOrDefault), StringComparison.Ordinal)
        )
        {
            var valueExpression1 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    $"The first argument of {node.Method.DeclaringType!.Name}.ElementAtOrDefault must be a value expression."
                );
            }
            var valueExpression2 = Visit(node.Arguments[1])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    $"The second argument of {node.Method.DeclaringType!.Name}.ElementAtOrDefault must be a value expression."
                );
            }
            return new FunctionValueExpression("array::at", [valueExpression1, valueExpression2]);
        }
        if (node.Method.Name.Equals(nameof(Enumerable.Empty), StringComparison.Ordinal))
        {
            return new ArrayValueExpression([]);
        }
        if (
            node.Method.Name.Equals(nameof(Enumerable.Except), StringComparison.Ordinal)
            && node.Arguments.Count == 2
        )
        {
            var valueExpression1 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    $"The first argument of {node.Method.DeclaringType!.Name}.Except must be a value expression."
                );
            }
            var valueExpression2 = Visit(node.Arguments[1])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    $"The second argument of {node.Method.DeclaringType!.Name}.Except must be a value expression."
                );
            }
            return new FunctionValueExpression(
                "array::difference",
                [valueExpression1, valueExpression2]
            );
        }
        if (
            node.Method.Name.Equals(nameof(Enumerable.FirstOrDefault), StringComparison.Ordinal)
            && node.Arguments.Count == 1
        )
        {
            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    $"The first argument of {node.Method.DeclaringType!.Name}.FirstOrDefault must be a value expression."
                );
            }
            // TODO : second arg?
            return new FunctionValueExpression("array::first", [valueExpression]);
        }
        if (
            node.Method.Name.Equals(nameof(Enumerable.Intersect), StringComparison.Ordinal)
            && node.Arguments.Count == 2
        )
        {
            var valueExpression1 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    $"The first argument of {node.Method.DeclaringType!.Name}.Intersect must be a value expression."
                );
            }
            var valueExpression2 = Visit(node.Arguments[1])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    $"The second argument of {node.Method.DeclaringType!.Name}.Intersect must be a value expression."
                );
            }
            return new FunctionValueExpression(
                "array::intersect",
                [valueExpression1, valueExpression2]
            );
        }
        if (
            node.Method.Name.Equals(nameof(Enumerable.LastOrDefault), StringComparison.Ordinal)
            && node.Arguments.Count == 1
        )
        {
            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    $"The first argument of {node.Method.DeclaringType!.Name}.LastOrDefault must be a value expression."
                );
            }
            // TODO : second arg?
            return new FunctionValueExpression("array::last", [valueExpression]);
        }
        if (node.Method.Name.Equals(nameof(Enumerable.Max), StringComparison.Ordinal))
        {
            if (node.Arguments.Count == 1)
            {
                if (isFirstParamLambdaParameter)
                {
                    return new FunctionValueExpression(
                        "array::max",
                        [new ParameterValueExpression("this")]
                    );
                }

                var valueExpression1 = Visit(node.Arguments[0])?.ToValue();
                if (valueExpression1 is null)
                {
                    throw new InvalidCastException(
                        $"The first argument of {node.Method.DeclaringType!.Name}.Max must be a value expression."
                    );
                }
                return new FunctionValueExpression("array::max", [valueExpression1]);
            }

            var valueExpression2 = Visit(node.Arguments[1])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    $"The second argument of {node.Method.DeclaringType!.Name}.Max must be a value expression."
                );
            }
            return new FunctionValueExpression("array::max", [valueExpression2]);
        }
        if (node.Method.Name.Equals(nameof(Enumerable.Min), StringComparison.Ordinal))
        {
            if (node.Arguments.Count == 1)
            {
                if (isFirstParamLambdaParameter)
                {
                    return new FunctionValueExpression(
                        "array::min",
                        [new ParameterValueExpression("this")]
                    );
                }

                var valueExpression1 = Visit(node.Arguments[0])?.ToValue();
                if (valueExpression1 is null)
                {
                    throw new InvalidCastException(
                        $"The first argument of {node.Method.DeclaringType!.Name}.Min must be a value expression."
                    );
                }
                return new FunctionValueExpression("array::min", [valueExpression1]);
            }

            var valueExpression2 = Visit(node.Arguments[1])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    $"The second argument of {node.Method.DeclaringType!.Name}.Min must be a value expression."
                );
            }
            return new FunctionValueExpression("array::min", [valueExpression2]);
        }
#if NET7_0_OR_GREATER
        if (
            node.Method.Name.Equals(nameof(Enumerable.Order), StringComparison.Ordinal)
            && node.Arguments.Count == 1
        )
        {
            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    $"The first argument of {node.Method.DeclaringType!.Name}.Order must be a value expression."
                );
            }
            return new FunctionValueExpression("array::sort::asc", [valueExpression]);
        }
        if (
            node.Method.Name.Equals(nameof(Enumerable.OrderDescending), StringComparison.Ordinal)
            && node.Arguments.Count == 1
        )
        {
            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    $"The first argument of {node.Method.DeclaringType!.Name}.OrderDescending must be a value expression."
                );
            }
            return new FunctionValueExpression("array::sort::desc", [valueExpression]);
        }
#endif
        if (node.Method.Name.Equals(nameof(Enumerable.Prepend), StringComparison.Ordinal))
        {
            var valueExpression1 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    $"The first argument of {node.Method.DeclaringType!.Name}.Prepend must be a value expression."
                );
            }
            var valueExpression2 = Visit(node.Arguments[1])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    $"The second argument of {node.Method.DeclaringType!.Name}.Prepend must be a value expression."
                );
            }
            return new FunctionValueExpression(
                "array::prepend",
                [valueExpression1, valueExpression2]
            );
        }
        if (node.Method.Name.Equals(nameof(Enumerable.Range), StringComparison.Ordinal))
        {
            var valueExpression1 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    $"The first argument of {node.Method.DeclaringType!.Name}.Range must be a value expression."
                );
            }
            var innerValueExpression2 = Visit(node.Arguments[1])?.ToValue();
            if (innerValueExpression2 is null)
            {
                throw new InvalidCastException(
                    $"The second argument of {node.Method.DeclaringType!.Name}.Range must be a value expression."
                );
            }
            var valueExpression2 = BinaryValueExpression.Add(
                valueExpression1,
                innerValueExpression2
            );
            return RangeValueExpression.Range(valueExpression1, valueExpression2);
        }
        if (node.Method.Name.Equals(nameof(Enumerable.Repeat), StringComparison.Ordinal))
        {
            var valueExpression1 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    $"The first argument of {node.Method.DeclaringType!.Name}.Repeat must be a value expression."
                );
            }
            var valueExpression2 = Visit(node.Arguments[1])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    $"The second argument of {node.Method.DeclaringType!.Name}.Repeat must be a value expression."
                );
            }
            return new FunctionValueExpression(
                "array::repeat",
                [valueExpression1, valueExpression2]
            );
        }
        if (node.Method.Name.Equals(nameof(Enumerable.Reverse), StringComparison.Ordinal))
        {
            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    $"The first argument of {node.Method.DeclaringType!.Name}.Reverse must be a value expression."
                );
            }
            return new FunctionValueExpression("array::reverse", [valueExpression]);
        }
        if (node.Method.Name.Equals(nameof(Enumerable.SelectMany), StringComparison.Ordinal))
        {
            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    $"The first argument of {node.Method.DeclaringType!.Name}.Reverse must be a value expression."
                );
            }
            return new FunctionValueExpression("array::reverse", [valueExpression]);
        }
        if (node.Method.Name.Equals(nameof(Enumerable.Skip), StringComparison.Ordinal))
        {
            var valueExpression1 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    $"The first argument of {node.Method.DeclaringType!.Name}.Skip must be a value expression."
                );
            }
            var valueExpression2 = Visit(node.Arguments[1])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    $"The second argument of {node.Method.DeclaringType!.Name}.Skip must be a value expression."
                );
            }
            return new FunctionValueExpression(
                "array::slice",
                [valueExpression1, valueExpression2]
            );
        }
        if (node.Method.Name.Equals(nameof(Enumerable.Sum), StringComparison.Ordinal))
        {
            if (node.Arguments.Count == 1)
            {
                return new FunctionValueExpression(
                    "math::sum",
                    [new ParameterValueExpression("this")]
                );
            }

            var valueExpression2 = Visit(node.Arguments[1])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    $"The second argument of {node.Method.DeclaringType!.Name}.Union must be a value expression."
                );
            }
            return new FunctionValueExpression("math::sum", [valueExpression2]);
        }
        if (node.Method.Name.Equals(nameof(Enumerable.Take), StringComparison.Ordinal))
        {
            var valueExpression1 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    $"The first argument of {node.Method.DeclaringType!.Name}.Take must be a value expression."
                );
            }
            var valueExpression2 = Visit(node.Arguments[1])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    $"The second argument of {node.Method.DeclaringType!.Name}.Take must be a value expression."
                );
            }
            return new FunctionValueExpression(
                "array::slice",
                [valueExpression1, new Int32ValueExpression(0), valueExpression2]
            );
        }
        if (
            node.Method.Name.Equals(nameof(Enumerable.ToHashSet), StringComparison.Ordinal)
            && node.Arguments.Count == 1
        )
        {
            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    $"The first argument of {node.Method.DeclaringType!.Name}.ToHashSet must be a value expression."
                );
            }
            return CastValueExpression.Set(valueExpression);
        }
        if (
            node.Method.Name.Equals(nameof(Enumerable.Union), StringComparison.Ordinal)
            && node.Arguments.Count == 2
        )
        {
            var valueExpression1 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    $"The first argument of {node.Method.DeclaringType!.Name}.Union must be a value expression."
                );
            }
            var valueExpression2 = Visit(node.Arguments[1])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    $"The second argument of {node.Method.DeclaringType!.Name}.Union must be a value expression."
                );
            }
            return new FunctionValueExpression(
                "array::union",
                [valueExpression1, valueExpression2]
            );
        }

        return base.VisitMethodCall(node);
    }

    private Expression BindArrayMethodCall(MethodCallExpression node)
    {
        if (
            node.Method.Name.Equals(nameof(Array.IndexOf), StringComparison.Ordinal)
            && node.Arguments.Count == 2
        )
        {
            var valueExpression1 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    $"The first argument of {node.Method.DeclaringType!.Name}.IndexOf must be a value expression."
                );
            }
            var valueExpression2 = Visit(node.Arguments[1])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    $"The second argument of {node.Method.DeclaringType!.Name}.IndexOf must be a value expression."
                );
            }
            return new FunctionValueExpression(
                "array::find_index",
                [valueExpression1, valueExpression2]
            );
        }

        return base.VisitMethodCall(node);
    }

    private Expression BindRegexMethodCall(MethodCallExpression node)
    {
        if (node.Method.Name.Equals(nameof(Regex.Replace), StringComparison.Ordinal))
        {
            if (node.Object is null)
            {
                // Static method
                if (node.Arguments.Count == 3 && node.Arguments.All(a => a.Type == typeof(string)))
                {
                    var valueExpression1 = Visit(node.Arguments[0])?.ToValue();
                    if (valueExpression1 is null)
                    {
                        throw new InvalidCastException(
                            "The first argument of Regex.Replace must be a value expression."
                        );
                    }
                    var valueExpression2 = Visit(node.Arguments[1])?.ToValue()?.ToRegex();
                    if (valueExpression2 is null)
                    {
                        throw new InvalidCastException(
                            "The second argument of Regex.Replace must be a regex expression."
                        );
                    }
                    var valueExpression3 = Visit(node.Arguments[2])?.ToValue();
                    if (valueExpression3 is null)
                    {
                        throw new InvalidCastException(
                            "The third argument of Regex.Replace must be a value expression."
                        );
                    }

                    return new FunctionValueExpression(
                        "string::replace",
                        [valueExpression1, valueExpression2, valueExpression3]
                    );
                }
            }
            else
            {
                // Non static method
                if (node.Arguments.Count == 2 && node.Arguments.All(a => a.Type == typeof(string)))
                {
                    var valueExpression1 = Visit(node.Arguments[0])?.ToValue();
                    if (valueExpression1 is null)
                    {
                        throw new InvalidCastException(
                            "The first argument of Regex.Replace must be a value expression."
                        );
                    }
                    var valueExpression2 = Visit(node.Object)?.ToValue()?.ToRegex();
                    if (valueExpression2 is null)
                    {
                        throw new InvalidCastException(
                            "The caller of Regex.Replace must be a regex expression."
                        );
                    }
                    var valueExpression3 = Visit(node.Arguments[1])?.ToValue();
                    if (valueExpression3 is null)
                    {
                        throw new InvalidCastException(
                            "The second argument of Regex.Replace must be a value expression."
                        );
                    }

                    return new FunctionValueExpression(
                        "string::replace",
                        [valueExpression1, valueExpression2, valueExpression3]
                    );
                }
            }
        }

        return base.VisitMethodCall(node);
    }

    private Expression BindVectorMethodCall(MethodCallExpression node)
    {
        string declaringTypeName = node.Method.DeclaringType!.Name;

        if (node.Method.Name.Equals(nameof(Vector2.Add), StringComparison.Ordinal))
        {
            var valueExpression1 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    $"The first argument of {declaringTypeName}.Add must be a value expression."
                );
            }
            var valueExpression2 = Visit(node.Arguments[1])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    $"The second argument of {declaringTypeName}.Add must be a value expression."
                );
            }
            return new FunctionValueExpression("vector::add", [valueExpression1, valueExpression2]);
        }
        if (node.Method.Name.Equals(nameof(Vector3.Cross), StringComparison.Ordinal))
        {
            var valueExpression1 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    $"The first argument of {declaringTypeName}.Cross must be a value expression."
                );
            }
            var valueExpression2 = Visit(node.Arguments[1])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    $"The second argument of {declaringTypeName}.Cross must be a value expression."
                );
            }
            return new FunctionValueExpression(
                "vector::cross",
                [valueExpression1, valueExpression2]
            );
        }
        if (node.Method.Name.Equals(nameof(Vector3.Distance), StringComparison.Ordinal))
        {
            var valueExpression1 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    $"The first argument of {declaringTypeName}.Distance must be a value expression."
                );
            }
            var valueExpression2 = Visit(node.Arguments[1])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    $"The second argument of {declaringTypeName}.Distance must be a value expression."
                );
            }
            return new FunctionValueExpression(
                "vector::distance::euclidean",
                [valueExpression1, valueExpression2]
            );
        }
        if (node.Method.Name.Equals(nameof(Vector3.Divide), StringComparison.Ordinal))
        {
            var valueExpression1 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    $"The first argument of {declaringTypeName}.Divide must be a value expression."
                );
            }
            var valueExpression2 = Visit(node.Arguments[1])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    $"The second argument of {declaringTypeName}.Divide must be a value expression."
                );
            }
            return new FunctionValueExpression(
                "vector::divide",
                [valueExpression1, valueExpression2]
            );
        }
        if (node.Method.Name.Equals(nameof(Vector2.Dot), StringComparison.Ordinal))
        {
            var valueExpression1 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    $"The first argument of {declaringTypeName}.Dot must be a value expression."
                );
            }
            var valueExpression2 = Visit(node.Arguments[1])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    $"The second argument of {declaringTypeName}.Dot must be a value expression."
                );
            }
            return new FunctionValueExpression("vector::dot", [valueExpression1, valueExpression2]);
        }
        if (node.Method.Name.Equals(nameof(Vector2.Multiply), StringComparison.Ordinal))
        {
            var valueExpression1 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    $"The first argument of {declaringTypeName}.Multiply must be a value expression."
                );
            }
            var valueExpression2 = Visit(node.Arguments[1])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    $"The second argument of {declaringTypeName}.Multiply must be a value expression."
                );
            }

            string functionName =
                valueExpression1 is FloatValueExpression || valueExpression2 is FloatValueExpression
                    ? "vector::scale"
                    : "vector::multiply";
            bool isFirstArgFloat = valueExpression1 is FloatValueExpression;
            ImmutableArray<ValueExpression> functionArgs = isFirstArgFloat
                ? [valueExpression2, valueExpression1]
                : [valueExpression1, valueExpression2];
            return new FunctionValueExpression(functionName, functionArgs);
        }
        if (node.Method.Name.Equals(nameof(Vector2.Normalize), StringComparison.Ordinal))
        {
            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression is null)
            {
                throw new InvalidCastException(
                    $"The first argument of {declaringTypeName}.Normalize must be a value expression."
                );
            }
            return new FunctionValueExpression("vector::normalize", [valueExpression]);
        }
        if (node.Method.Name.Equals(nameof(Vector2.Subtract), StringComparison.Ordinal))
        {
            var valueExpression1 = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression1 is null)
            {
                throw new InvalidCastException(
                    $"The first argument of {declaringTypeName}.Subtract must be a value expression."
                );
            }
            var valueExpression2 = Visit(node.Arguments[1])?.ToValue();
            if (valueExpression2 is null)
            {
                throw new InvalidCastException(
                    $"The second argument of {declaringTypeName}.Subtract must be a value expression."
                );
            }
            return new FunctionValueExpression(
                "vector::substract",
                [valueExpression1, valueExpression2]
            );
        }

        return base.VisitMethodCall(node);
    }

    protected override Expression VisitNew(NewExpression node)
    {
        if (node.Type.IsAnonymous() && node.Members is not null)
        {
            var fields = node
                .Members.Select(
                    (m, index) =>
                    {
                        var argExpression = node.Arguments[index];
                        var valueExpression = Visit(argExpression)?.ToValue();
                        if (valueExpression is null)
                        {
                            throw new NotSupportedException(
                                $"Cannot convert {argExpression.Type.Name} to {nameof(ValueExpression)}."
                            );
                        }

                        return (FieldName: m.Name, Expression: valueExpression);
                    }
                )
                .ToImmutableDictionary(x => x.FieldName, x => x.Expression);

            return new ObjectValueExpression(fields);
        }

        if (node.Type == typeof(Regex) && node.Arguments.Count == 1)
        {
            var valueExpression = Visit(node.Arguments[0])?.ToValue();
            if (valueExpression is not StringValueExpression stringValueExpression)
            {
                throw new InvalidCastException(
                    $"The first argument of {nameof(Regex)} ctor must be a string expression."
                );
            }
#pragma warning disable MA0009
            return new RegexValueExpression(new Regex(stringValueExpression.Value));
#pragma warning restore MA0009
        }

        if (
            node.Type == typeof(Vector2)
            || node.Type == typeof(Vector3)
            || node.Type == typeof(Vector4)
        )
        {
            int vectorLength =
                node.Type == typeof(Vector2) ? 2
                : node.Type == typeof(Vector3) ? 3
                : node.Type == typeof(Vector4) ? 4
                : throw new InvalidCastException("Cannot convert vector length.");

            var valueExpressions = node
                .Arguments.SelectMany(
                    (arg, index) =>
                    {
                        var valueExpression = Visit(arg)?.ToValue();
                        if (valueExpression is null)
                        {
                            string argNumber = index switch
                            {
                                0 => "first",
                                1 => "second",
                                2 => "third",
                                _ => $"{(index + 1).ToString(CultureInfo.InvariantCulture)}nth",
                            };
                            throw new InvalidCastException(
                                $"The {argNumber} argument of {node.Type.Name} ctor must be a value expression."
                            );
                        }

                        return valueExpression is ArrayValueExpression arrayValueExpression
                            ? arrayValueExpression.Values
                            : [valueExpression];
                    }
                )
                .ToImmutableArray();
            valueExpressions =
                valueExpressions.Length == 1
                    ? [.. Enumerable.Repeat(valueExpressions[0], vectorLength)]
                    : valueExpressions;

            return new ArrayValueExpression(valueExpressions);
        }

        return base.VisitNew(node);
    }

    protected override Expression VisitMemberInit(MemberInitExpression node)
    {
        var fields = node
            .Bindings.Select(
                (binding) =>
                {
                    if (binding is MemberAssignment memberAssignment)
                    {
                        var valueExpression = Visit(memberAssignment.Expression)?.ToValue();
                        if (valueExpression is null)
                        {
                            throw new NotSupportedException(
                                $"Cannot convert {memberAssignment.Member.DeclaringType} to {nameof(ValueExpression)}."
                            );
                        }

                        return (FieldName: binding.Member.Name, Expression: valueExpression);
                    }

                    throw new NotSupportedException(
                        $"'{binding.BindingType}' is not currently supported."
                    );
                }
            )
            .ToImmutableDictionary(x => x.FieldName, x => x.Expression);

        return new ObjectValueExpression(fields);
    }

    protected override Expression VisitNewArray(NewArrayExpression node)
    {
        return new ArrayValueExpression(
            [
                .. node.Expressions.Select(e =>
                {
                    var valueExpression = Visit(e)?.ToValue();
                    if (valueExpression is null)
                    {
                        throw new NotSupportedException(
                            $"Cannot convert {e.Type.Name} to {nameof(ValueExpression)}."
                        );
                    }

                    return valueExpression;
                }),
            ]
        );
    }
}
