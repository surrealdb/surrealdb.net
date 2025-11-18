using System.Collections.Immutable;
using System.Linq.Expressions;
using System.Text;
using SurrealDb.Net.Internals.Queryable.Expressions.Surreal;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Queryable.Visitors;

internal sealed class QueryGeneratorExpressionVisitor : ExpressionVisitor
{
    private StringBuilder _surqlQueryBuilder = null!;

    public string Translate(SurrealExpression expression)
    {
        _surqlQueryBuilder = new StringBuilder();

        Visit(expression);

        return _surqlQueryBuilder.ToString();
    }

    public override Expression? Visit(Expression? node)
    {
        if (node is null)
        {
            return null;
        }

        if (node is SurrealExpression surrealExpression)
        {
            return surrealExpression switch
            {
                SelectStatementExpression selectStatementExpression => VisitSelect(
                    selectStatementExpression
                ),
                FieldsExpression fieldsExpression => VisitFields(fieldsExpression),
                WhatExpression whatExpression => VisitWhat(whatExpression),
                IndexingExpression indexingExpression => VisitIndexing(indexingExpression),
                ConditionsExpression whereExpression => VisitWhere(whereExpression),
                SplitsExpression splitsExpression => VisitSplits(splitsExpression),
                GroupingExpression groupsExpression => VisitGroups(groupsExpression),
                OrderingExpression orderingExpression => VisitOrdering(orderingExpression),
                LimitExpression limitExpression => VisitLimit(limitExpression),
                StartExpression startExpression => VisitStart(startExpression),
                FetchsExpression fetchsExpression => VisitFetchs(fetchsExpression),
                VersionExpression versionExpression => VisitVersion(versionExpression),
                ExplainExpression explainExpression => VisitExplain(explainExpression),
                IdiomExpression idiomExpression => VisitIdiom(idiomExpression),
                PartExpression partExpression => VisitPart(partExpression),
                ValueExpression valueExpression => VisitValue(valueExpression),
                _ => base.Visit(node),
            };
        }

        return base.Visit(node);
    }

    private SelectStatementExpression VisitSelect(
        SelectStatementExpression selectStatementExpression
    )
    {
        _surqlQueryBuilder.Append("SELECT ");
        Visit(selectStatementExpression.Fields);
        _surqlQueryBuilder.Append(" FROM ");
        if (selectStatementExpression.Only)
        {
            _surqlQueryBuilder.Append("ONLY ");
        }
        Visit(selectStatementExpression.What);
        Visit(selectStatementExpression.With);
        Visit(selectStatementExpression.Cond);
        Visit(selectStatementExpression.Splits);
        Visit(selectStatementExpression.Group);
        Visit(selectStatementExpression.Order);
        Visit(selectStatementExpression.Limit);
        Visit(selectStatementExpression.Start);
        Visit(selectStatementExpression.Fetchs);
        Visit(selectStatementExpression.Version);
        if (selectStatementExpression.Timeout.HasValue)
        {
            _surqlQueryBuilder.Append(" TIMEOUT ");
            _surqlQueryBuilder.Append(selectStatementExpression.Timeout.Value.ToString());
        }
        if (selectStatementExpression.Parallel)
        {
            _surqlQueryBuilder.Append(" PARALLEL");
        }
        if (selectStatementExpression.Tempfiles)
        {
            _surqlQueryBuilder.Append(" TEMPFILES");
        }
        Visit(selectStatementExpression.Explain);

        return selectStatementExpression;
    }

    private FieldsExpression VisitFields(FieldsExpression fieldsExpression)
    {
        if (fieldsExpression.IsSingleValue)
        {
            var singleFieldExpression = (SingleFieldExpression)fieldsExpression.Fields[0];

            _surqlQueryBuilder.Append("VALUE ");
            Visit(singleFieldExpression.Expression);

            return fieldsExpression;
        }

        for (int index = 0; index < fieldsExpression.Fields.Length; index++)
        {
            if (index > 0)
            {
                _surqlQueryBuilder.Append(", ");
            }

            var fieldExpression = fieldsExpression.Fields[index];
            switch (fieldExpression)
            {
                case AllFieldExpression:
                    _surqlQueryBuilder.Append('*');
                    break;
                case SingleFieldExpression singleFieldExpression:
                {
                    Visit(singleFieldExpression.Expression);
                    if (singleFieldExpression.Alias is not null)
                    {
                        _surqlQueryBuilder.Append(" AS ");
                        Visit(singleFieldExpression.Alias);
                    }

                    break;
                }
            }
        }

        return fieldsExpression;
    }

    private WhatExpression VisitWhat(WhatExpression whatExpression)
    {
        VisitCommaSeparatedValues(whatExpression.Values);
        return whatExpression;
    }

    private IndexingExpression VisitIndexing(IndexingExpression indexingExpression)
    {
        _surqlQueryBuilder.Append(" WITH ");

        switch (indexingExpression)
        {
            case NoIndexExpression:
                _surqlQueryBuilder.Append("NOINDEX");
                break;
            case WithIndexesExpression withIndexesExpression:
            {
                _surqlQueryBuilder.Append("INDEX ");
                for (int index = 0; index < withIndexesExpression.Names.Length; index++)
                {
                    if (index > 0)
                    {
                        _surqlQueryBuilder.Append(", ");
                    }

                    var indexName = withIndexesExpression.Names[index];
                    _surqlQueryBuilder.Append(indexName);
                }

                break;
            }
        }

        return indexingExpression;
    }

    private ConditionsExpression VisitWhere(ConditionsExpression conditionsExpression)
    {
        _surqlQueryBuilder.Append(" WHERE ");
        Visit(conditionsExpression.Value);

        return conditionsExpression;
    }

    private SplitsExpression VisitSplits(SplitsExpression splitsExpression)
    {
        _surqlQueryBuilder.Append(" SPLIT ");
        for (int index = 0; index < splitsExpression.Fields.Length; index++)
        {
            if (index > 0)
            {
                _surqlQueryBuilder.Append(", ");
            }

            var field = splitsExpression.Fields[index];
            Visit(field);
        }

        return splitsExpression;
    }

    private GroupingExpression VisitGroups(GroupingExpression groupingExpression)
    {
        if (groupingExpression.Fields.IsEmpty)
        {
            _surqlQueryBuilder.Append(" GROUP ALL");
            return groupingExpression;
        }

        _surqlQueryBuilder.Append(" GROUP BY ");
        for (int index = 0; index < groupingExpression.Fields.Length; index++)
        {
            if (index > 0)
            {
                _surqlQueryBuilder.Append(", ");
            }

            var field = groupingExpression.Fields[index];
            Visit(field);
        }

        return groupingExpression;
    }

    private OrderingExpression VisitOrdering(OrderingExpression orderingExpression)
    {
        _surqlQueryBuilder.Append(" ORDER BY ");

        if (orderingExpression is RandomOrderingExpression)
        {
            _surqlQueryBuilder.Append(" RAND()");
        }

        if (orderingExpression is ListOrderingExpression listOrderingExpression)
        {
            for (int index = 0; index < listOrderingExpression.Orders.Length; index++)
            {
                if (index > 0)
                {
                    _surqlQueryBuilder.Append(", ");
                }

                var order = listOrderingExpression.Orders[index];
                Visit(order.Value);
                _surqlQueryBuilder.Append(order.GetSuffix());
            }
        }

        return orderingExpression;
    }

    private LimitExpression VisitLimit(LimitExpression limitExpression)
    {
        _surqlQueryBuilder.Append(" LIMIT ");
        Visit(limitExpression.Value);

        return limitExpression;
    }

    private StartExpression VisitStart(StartExpression startExpression)
    {
        _surqlQueryBuilder.Append(" START ");
        Visit(startExpression.Value);

        return startExpression;
    }

    private FetchsExpression VisitFetchs(FetchsExpression fetchsExpression)
    {
        _surqlQueryBuilder.Append(" FETCH ");
        VisitCommaSeparatedValues(fetchsExpression.Fields);

        return fetchsExpression;
    }

    private VersionExpression VisitVersion(VersionExpression versionExpression)
    {
        _surqlQueryBuilder.Append(" VERSION ");
        Visit(versionExpression.Value);

        return versionExpression;
    }

    private ExplainExpression VisitExplain(ExplainExpression explainExpression)
    {
        _surqlQueryBuilder.Append(" EXPLAIN");
        if (explainExpression.Full)
        {
            _surqlQueryBuilder.Append(" FULL");
        }

        return explainExpression;
    }

    private IdiomExpression VisitIdiom(IdiomExpression idiomExpression)
    {
        if (idiomExpression.IsSingleFieldPart)
        {
            var fieldPartExpression = (FieldPartExpression)idiomExpression.Parts[0];
            _surqlQueryBuilder.Append(fieldPartExpression.FieldName);

            return idiomExpression;
        }

        foreach (var partExpression in idiomExpression.Parts)
        {
            Visit(partExpression);
        }

        return idiomExpression;
    }

    private PartExpression VisitPart(PartExpression partExpression)
    {
        if (partExpression is IPrintableExpression printableExpression)
        {
            printableExpression.AppendTo(_surqlQueryBuilder);
        }

        if (partExpression is StartPartExpression startExpression)
        {
            Visit(startExpression.Value);
        }

        if (partExpression is ValuePartExpression valueExpression)
        {
            _surqlQueryBuilder.Append('[');
            Visit(valueExpression.Value);
            _surqlQueryBuilder.Append(']');
        }

        if (partExpression is MethodPartExpression methodExpression)
        {
            _surqlQueryBuilder.Append('.');
            _surqlQueryBuilder.Append(methodExpression.Name);
            _surqlQueryBuilder.Append('(');
            VisitCommaSeparatedValues(methodExpression.Args);
            _surqlQueryBuilder.Append(')');
        }

        if (partExpression is WherePartExpression whereExpression)
        {
            _surqlQueryBuilder.Append('[');
            _surqlQueryBuilder.Append("WHERE ");
            Visit(whereExpression.Value);
            _surqlQueryBuilder.Append(']');
        }

        return partExpression;
    }

    private ValueExpression VisitValue(ValueExpression valueExpression)
    {
        if (valueExpression is IPrintableExpression printableExpression)
        {
            printableExpression.AppendTo(_surqlQueryBuilder);
        }

        if (valueExpression is IdiomValueExpression idiomExpression)
        {
            Visit(idiomExpression.Idiom);
        }

        if (valueExpression is UnaryValueExpression unaryExpression)
        {
            unaryExpression.Operator.AppendTo(_surqlQueryBuilder);
            bool shouldAppendParentheses = unaryExpression.Value is BinaryValueExpression;

            if (shouldAppendParentheses)
            {
                _surqlQueryBuilder.Append('(');
            }
            Visit(unaryExpression.Value);
            if (shouldAppendParentheses)
            {
                _surqlQueryBuilder.Append(')');
            }
        }

        if (valueExpression is BinaryValueExpression binaryExpression)
        {
            bool isLeftBindingPowerLower = IsBindingPowerLowerThan(
                binaryExpression.Left,
                binaryExpression.Operator
            );
            bool isRightBindingPowerLower = IsBindingPowerLowerThan(
                binaryExpression.Right,
                binaryExpression.Operator
            );

            if (isLeftBindingPowerLower)
            {
                _surqlQueryBuilder.Append('(');
            }
            Visit(binaryExpression.Left);
            if (isLeftBindingPowerLower)
            {
                _surqlQueryBuilder.Append(')');
            }
            _surqlQueryBuilder.Append(' ');
            binaryExpression.Operator.AppendTo(_surqlQueryBuilder);
            _surqlQueryBuilder.Append(' ');
            if (isRightBindingPowerLower)
            {
                _surqlQueryBuilder.Append('(');
            }
            Visit(binaryExpression.Right);
            if (isRightBindingPowerLower)
            {
                _surqlQueryBuilder.Append(')');
            }
        }

        if (valueExpression is SubqueryValueExpression subqueryExpression)
        {
            _surqlQueryBuilder.Append('(');
            Visit(subqueryExpression.Expression);
            _surqlQueryBuilder.Append(')');
        }

        if (valueExpression is FunctionValueExpression functionExpression)
        {
            _surqlQueryBuilder.Append(functionExpression.Fullname);
            _surqlQueryBuilder.Append('(');
            VisitCommaSeparatedValues(functionExpression.Parameters);
            _surqlQueryBuilder.Append(')');
        }

        if (valueExpression is ArrayValueExpression arrayExpression)
        {
            _surqlQueryBuilder.Append('[');
            VisitCommaSeparatedValues(arrayExpression.Values);
            _surqlQueryBuilder.Append(']');
        }

        if (valueExpression is ObjectValueExpression objectExpression)
        {
            _surqlQueryBuilder.Append('{');
            _surqlQueryBuilder.Append(' ');
            int index = 0;
            foreach (var (key, value) in objectExpression.Fields)
            {
                if (index > 0)
                {
                    _surqlQueryBuilder.Append(", ");
                }

                bool shouldEscapeObjectKey = ShouldEscapeObjectKey(key);
                if (shouldEscapeObjectKey)
                {
                    _surqlQueryBuilder.Append('"');
                }
                _surqlQueryBuilder.Append(key);
                if (shouldEscapeObjectKey)
                {
                    _surqlQueryBuilder.Append('"');
                }
                _surqlQueryBuilder.Append(':');
                _surqlQueryBuilder.Append(' ');
                Visit(value);

                index++;
            }
            _surqlQueryBuilder.Append(' ');
            _surqlQueryBuilder.Append('}');
        }

        if (valueExpression is RangeValueExpression rangeExpression)
        {
            if (rangeExpression.BeginBoundType != RangeBoundType.Unbounded)
            {
                bool appendParentheses = rangeExpression.BeginValue is not IConstantValueExpression;
                if (appendParentheses)
                {
                    _surqlQueryBuilder.Append('(');
                }
                Visit(rangeExpression.BeginValue);
                if (appendParentheses)
                {
                    _surqlQueryBuilder.Append(')');
                }
            }
            if (rangeExpression.BeginBoundType == RangeBoundType.Exclusive)
            {
                _surqlQueryBuilder.Append('>');
            }
            _surqlQueryBuilder.Append("..");
            if (rangeExpression.EndBoundType == RangeBoundType.Inclusive)
            {
                _surqlQueryBuilder.Append('=');
            }
            if (rangeExpression.EndBoundType != RangeBoundType.Unbounded)
            {
                bool appendParentheses = rangeExpression.EndValue is not IConstantValueExpression;
                if (appendParentheses)
                {
                    _surqlQueryBuilder.Append('(');
                }
                Visit(rangeExpression.EndValue);
                if (appendParentheses)
                {
                    _surqlQueryBuilder.Append(')');
                }
            }
        }

        if (valueExpression is CastValueExpression castExpression)
        {
            _surqlQueryBuilder.Append('<');
            _surqlQueryBuilder.Append(castExpression.Kind);
            _surqlQueryBuilder.Append('>');
            _surqlQueryBuilder.Append(' ');
            Visit(castExpression.Value);
        }

        if (valueExpression is IfElseStatementValueExpression ifElseExpression)
        {
            bool isFirstCondition = true;
            foreach (var (cond, then) in ifElseExpression.Expressions)
            {
                if (!isFirstCondition)
                {
                    _surqlQueryBuilder.Append("ELSE");
                    _surqlQueryBuilder.Append(' ');
                }

                _surqlQueryBuilder.Append("IF");
                _surqlQueryBuilder.Append(' ');
                Visit(cond);
                _surqlQueryBuilder.Append(' ');
                _surqlQueryBuilder.Append("THEN");
                _surqlQueryBuilder.Append(' ');
                Visit(then);

                isFirstCondition = false;

                _surqlQueryBuilder.Append(' ');
            }
            if (ifElseExpression.Else is not null)
            {
                _surqlQueryBuilder.Append("ELSE");
                _surqlQueryBuilder.Append(' ');
                Visit(ifElseExpression.Else);
                _surqlQueryBuilder.Append(' ');
            }
            _surqlQueryBuilder.Append("END");
        }

        if (valueExpression is BlockValueExpression blockExpression)
        {
            _surqlQueryBuilder.Append('{');
            _surqlQueryBuilder.Append(' ');
            foreach (var expression in blockExpression.Expressions)
            {
                Visit(expression);
                _surqlQueryBuilder.Append(';');
            }
            _surqlQueryBuilder.Append(' ');
            _surqlQueryBuilder.Append('}');
        }

        return valueExpression;
    }

    private static bool IsBindingPowerLowerThan(
        ValueExpression valueExpression,
        IOperator @operator
    )
    {
        PowerBindingCategory? nestedBindingCategory = valueExpression switch
        {
            BinaryValueExpression binaryExpression => GetBinaryPowerBindingCategory(
                binaryExpression.Operator
            ),
            UnaryValueExpression => PowerBindingCategory.Unary,
            CastValueExpression => PowerBindingCategory.Cast,
            RangeValueExpression => PowerBindingCategory.Range,
            _ => null,
        };

        if (nestedBindingCategory is null)
        {
            return false;
        }

        var currentBindingCategory = GetBinaryPowerBindingCategory(@operator);
        if (currentBindingCategory is null)
        {
            return false;
        }

        return nestedBindingCategory < currentBindingCategory;
    }

    private static PowerBindingCategory? GetBinaryPowerBindingCategory(IOperator @operator)
    {
        return @operator switch
        {
            SimpleOperator simpleOperator => simpleOperator.Type switch
            {
                OperatorType.Or => PowerBindingCategory.Or,
                OperatorType.And => PowerBindingCategory.And,
                OperatorType.Tco => PowerBindingCategory.Nullish,
                OperatorType.Nco => PowerBindingCategory.Nullish,
                OperatorType.Add => PowerBindingCategory.AddSub,
                OperatorType.Sub => PowerBindingCategory.AddSub,
                OperatorType.Mul => PowerBindingCategory.MulDiv,
                OperatorType.Div => PowerBindingCategory.MulDiv,
                OperatorType.Pow => PowerBindingCategory.Power,
                OperatorType.Inc => PowerBindingCategory.Unary,
                OperatorType.Dec => PowerBindingCategory.Unary,
                OperatorType.Ext => PowerBindingCategory.Unary,
                OperatorType.Equal => PowerBindingCategory.Equality,
                OperatorType.Exact => PowerBindingCategory.Equality,
                OperatorType.NotEqual => PowerBindingCategory.Equality,
                OperatorType.AllEqual => PowerBindingCategory.Equality,
                OperatorType.AnyEqual => PowerBindingCategory.Equality,
                OperatorType.Like => PowerBindingCategory.Equality,
                OperatorType.NotLike => PowerBindingCategory.Equality,
                OperatorType.AllLike => PowerBindingCategory.Equality,
                OperatorType.AnyLike => PowerBindingCategory.Equality,
                OperatorType.LessThan => PowerBindingCategory.Equality,
                OperatorType.LessThanOrEqual => PowerBindingCategory.Equality,
                OperatorType.MoreThan => PowerBindingCategory.Equality,
                OperatorType.MoreThanOrEqual => PowerBindingCategory.Equality,
                OperatorType.Contain => PowerBindingCategory.Relation,
                OperatorType.NotContain => PowerBindingCategory.Relation,
                OperatorType.ContainAll => PowerBindingCategory.Relation,
                OperatorType.ContainAny => PowerBindingCategory.Relation,
                OperatorType.ContainNone => PowerBindingCategory.Relation,
                OperatorType.Inside => PowerBindingCategory.Relation,
                OperatorType.NotInside => PowerBindingCategory.Relation,
                OperatorType.AllInside => PowerBindingCategory.Relation,
                OperatorType.AnyInside => PowerBindingCategory.Relation,
                OperatorType.NoneInside => PowerBindingCategory.Relation,
                OperatorType.Outside => PowerBindingCategory.Relation,
                OperatorType.Intersects => PowerBindingCategory.Relation,
                OperatorType.Rem => PowerBindingCategory.MulDiv,
                _ => null,
            },
            _ => null,
        };
    }

    private void VisitCommaSeparatedValues(ImmutableArray<ValueExpression> valueExpressions)
    {
        for (int index = 0; index < valueExpressions.Length; index++)
        {
            if (index > 0)
            {
                _surqlQueryBuilder.Append(", ");
            }

            var value = valueExpressions[index];
            Visit(value);
        }
    }

    private static bool ShouldEscapeObjectKey(string key)
    {
#if NET7_0_OR_GREATER
        return char.IsAsciiDigit(key[0])
            || key.Any(c => !char.IsAsciiLetter(c) && c != '_' && !char.IsAsciiDigit(c));
#else
        return true;
#endif
    }
}
