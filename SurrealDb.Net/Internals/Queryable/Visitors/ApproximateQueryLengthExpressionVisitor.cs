using System.Linq.Expressions;
using SurrealDb.Net.Internals.Queryable.Expressions.Surreal;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Queryable.Visitors;

/// <summary>
/// Traverses a <see cref="SurrealExpression"/> tree and produces a pessimistic approximation of
/// the number of characters that <see cref="QueryGeneratorExpressionVisitor"/> will write.
/// The result is intended to pre-size a <see cref="System.Text.StringBuilder"/> so that query
/// generation avoids internal buffer re-allocations.
/// </summary>
internal sealed class ApproximateQueryLengthExpressionVisitor : ExpressionVisitor
{
    private int _length;

    /// <summary>Returns the approximate character length of the translated query.</summary>
    public int Approximate(SurrealExpression expression)
    {
        _length = 0;
        Visit(expression);
        return _length;
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
                ConditionsExpression conditionsExpression => VisitWhere(conditionsExpression),
                SplitsExpression splitsExpression => VisitSplits(splitsExpression),
                GroupingExpression groupingExpression => VisitGroups(groupingExpression),
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

    // "SELECT " (7) + fields + " FROM " (6) + optional "ONLY " (5) + what
    // optional clauses accounted for in their respective Visit* methods
    private SelectStatementExpression VisitSelect(
        SelectStatementExpression selectStatementExpression
    )
    {
        _length += 7; // "SELECT "
        Visit(selectStatementExpression.Fields);
        _length += 6; // " FROM "
        if (selectStatementExpression.Only)
        {
            _length += 5; // "ONLY "
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
            _length += 9; // " TIMEOUT "
            _length += 16; // pessimistic duration string length
        }
        if (selectStatementExpression.Parallel)
        {
            _length += 9; // " PARALLEL"
        }
        if (selectStatementExpression.Tempfiles)
        {
            _length += 10; // " TEMPFILES"
        }
        Visit(selectStatementExpression.Explain);

        return selectStatementExpression;
    }

    private FieldsExpression VisitFields(FieldsExpression fieldsExpression)
    {
        if (fieldsExpression.IsSingleValue)
        {
            _length += 6; // "VALUE "
            var singleField = (SingleFieldExpression)fieldsExpression.Fields[0];
            Visit(singleField.Expression);
            return fieldsExpression;
        }

        for (int i = 0; i < fieldsExpression.Fields.Length; i++)
        {
            if (i > 0)
            {
                _length += 2; // ", "
            }

            var field = fieldsExpression.Fields[i];
            switch (field)
            {
                case AllFieldExpression:
                    _length += 1; // "*"
                    break;
                case SingleFieldExpression singleField:
                    Visit(singleField.Expression);
                    if (singleField.Alias is not null)
                    {
                        _length += 4; // " AS "
                        Visit(singleField.Alias);
                    }
                    break;
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
        _length += 6; // " WITH "
        switch (indexingExpression)
        {
            case NoIndexExpression:
                _length += 7; // "NOINDEX"
                break;
            case WithIndexesExpression withIndexesExpression:
                _length += 6; // "INDEX "
                for (int i = 0; i < withIndexesExpression.Names.Length; i++)
                {
                    if (i > 0)
                    {
                        _length += 2; // ", "
                    }
                    _length += withIndexesExpression.Names[i].Length;
                }
                break;
        }

        return indexingExpression;
    }

    private ConditionsExpression VisitWhere(ConditionsExpression conditionsExpression)
    {
        _length += 7; // " WHERE "
        Visit(conditionsExpression.Value);
        return conditionsExpression;
    }

    private SplitsExpression VisitSplits(SplitsExpression splitsExpression)
    {
        _length += 7; // " SPLIT "
        for (int i = 0; i < splitsExpression.Fields.Length; i++)
        {
            if (i > 0)
            {
                _length += 2; // ", "
            }
            Visit(splitsExpression.Fields[i]);
        }
        return splitsExpression;
    }

    private GroupingExpression VisitGroups(GroupingExpression groupingExpression)
    {
        if (groupingExpression.Fields.IsEmpty)
        {
            _length += 10; // " GROUP ALL"
            return groupingExpression;
        }

        _length += 10; // " GROUP BY "
        for (int i = 0; i < groupingExpression.Fields.Length; i++)
        {
            if (i > 0)
            {
                _length += 2; // ", "
            }
            Visit(groupingExpression.Fields[i]);
        }
        return groupingExpression;
    }

    private OrderingExpression VisitOrdering(OrderingExpression orderingExpression)
    {
        _length += 10; // " ORDER BY "
        if (orderingExpression is RandomOrderingExpression)
        {
            _length += 7; // " RAND()"
        }
        else if (orderingExpression is ListOrderingExpression listOrdering)
        {
            for (int i = 0; i < listOrdering.Orders.Length; i++)
            {
                if (i > 0)
                {
                    _length += 2; // ", "
                }
                var order = listOrdering.Orders[i];
                Visit(order.Value);
                _length += order.GetSuffix().Length;
            }
        }
        return orderingExpression;
    }

    private LimitExpression VisitLimit(LimitExpression limitExpression)
    {
        _length += 7; // " LIMIT "
        Visit(limitExpression.Value);
        return limitExpression;
    }

    private StartExpression VisitStart(StartExpression startExpression)
    {
        _length += 7; // " START "
        Visit(startExpression.Value);
        return startExpression;
    }

    private FetchsExpression VisitFetchs(FetchsExpression fetchsExpression)
    {
        _length += 7; // " FETCH "
        VisitCommaSeparatedValues(fetchsExpression.Fields);
        return fetchsExpression;
    }

    private VersionExpression VisitVersion(VersionExpression versionExpression)
    {
        _length += 9; // " VERSION "
        Visit(versionExpression.Value);
        return versionExpression;
    }

    private ExplainExpression VisitExplain(ExplainExpression explainExpression)
    {
        _length += 8; // " EXPLAIN"
        if (explainExpression.Full)
        {
            _length += 5; // " FULL"
        }
        return explainExpression;
    }

    private IdiomExpression VisitIdiom(IdiomExpression idiomExpression)
    {
        if (idiomExpression.IsSingleFieldPart)
        {
            var fieldPart = (FieldPartExpression)idiomExpression.Parts[0];
            _length += fieldPart.FieldName.Length;
            return idiomExpression;
        }

        foreach (var part in idiomExpression.Parts)
        {
            Visit(part);
        }
        return idiomExpression;
    }

    private PartExpression VisitPart(PartExpression partExpression)
    {
        switch (partExpression)
        {
            case AllPartExpression:
                _length += 3; // "[*]"
                break;
            case FlattenPartExpression:
                _length += 3; // "..."
                break;
            case LastPartExpression:
                _length += 3; // "[$]"
                break;
            case FirstPartExpression:
                _length += 3; // "[0]"
                break;
            case FieldPartExpression fieldPart:
                _length += 1 + fieldPart.FieldName.Length; // "." + name
                break;
            case IndexPartExpression:
                _length += 5; // "[N]" pessimistic (up to 5 chars: "[" + up to 3-digit index + "]")
                break;
            case OptionalPartExpression:
                _length += 1; // "?"
                break;
            case DocPartExpression:
                _length += 1; // "@"
                break;
            case RepeatRecursePartExpression:
                _length += 2; // ".@"
                break;
            case WherePartExpression wherePart:
                _length += 8; // "[WHERE " + "]"
                Visit(wherePart.Value);
                break;
            case ValuePartExpression valuePart:
                _length += 2; // "[" + "]"
                Visit(valuePart.Value);
                break;
            case StartPartExpression startPart:
                Visit(startPart.Value);
                break;
            case MethodPartExpression methodPart:
                _length += 2 + methodPart.Name.Length + 2; // "." + name + "()"
                VisitCommaSeparatedValues(methodPart.Args);
                break;
        }

        return partExpression;
    }

    private ValueExpression VisitValue(ValueExpression valueExpression)
    {
        switch (valueExpression)
        {
            // IPrintableExpression types with known/computable lengths
            case NoneValueExpression:
                _length += 4; // "NONE"
                break;
            case NullValueExpression:
                _length += 4; // "null"
                break;
            case BoolValueExpression:
                _length += 5; // "False" (pessimistic over "True")
                break;
            case Int32ValueExpression int32:
                _length += CountDigits(int32.Value);
                break;
            case Int64ValueExpression int64:
                _length += CountDigits(int64.Value);
                break;
            case FormattedIntegerValueExpression:
                _length += 20; // pessimistic: max int64 string length
                break;
            case SingleValueExpression singleVal:
                // InvariantCulture float string; pessimistic upper bound
                _length += CountFloatLength(singleVal.Value);
                break;
            case DoubleValueExpression doubleVal:
                _length += CountFloatLength(doubleVal.Value);
                break;
            case DecimalValueExpression:
                _length += 32; // pessimistic: large decimal + "dec"
                break;
            case CharValueExpression:
                _length += 3; // '"c"' or "'c'"
                break;
            case StringValueExpression str:
                _length += str.Value.Length + 2; // '"' + value + '"'
                break;
            case DurationValueExpression:
                _length += 16; // pessimistic duration (e.g. "1000000000000ms")
                break;
            case DateTimeValueExpression:
                _length += 29; // 'd"' + ISO 8601 "O" format (27 chars) + '"'
                break;
            case GuidValueExpression:
                _length += 38; // 'u"' + 36-char guid + '"'
                break;
            case ParameterValueExpression:
                // "$" + param name; parameter names are like "p0", "p1", etc.
                _length += 4; // "$pNN"
                break;
            case TableValueExpression:
                // Table name may have backtick escaping (`name`).
                // The actual name is not publicly accessible; use a pessimistic constant.
                _length += 64;
                break;
            case ConstantValueExpression:
                _length += 20; // longest builtin constant e.g. "math::FRAC_2_SQRT_PI"
                break;
            case RegexValueExpression:
                _length += 16; // pessimistic
                break;
            case EmptyBlockValueExpression:
                _length += 2; // "{}"
                break;

            // Composite value expressions
            case IdiomValueExpression idiomValue:
                Visit(idiomValue.Idiom);
                break;
            case UnaryValueExpression unary:
                _length += GetOperatorLength(unary.Operator);
                bool isUnaryWrapped = unary.Value is BinaryValueExpression;
                if (isUnaryWrapped)
                {
                    _length += 2; // "()"
                }
                Visit(unary.Value);
                break;
            case BinaryValueExpression binary:
                bool isLeftWrapped = IsBindingPowerLowerThan(binary.Left, binary.Operator);
                bool isRightWrapped = IsBindingPowerLowerThan(binary.Right, binary.Operator);
                if (isLeftWrapped)
                {
                    _length += 2; // "()"
                }
                Visit(binary.Left);
                _length += 2; // " " + " " around operator
                _length += GetOperatorLength(binary.Operator);
                if (isRightWrapped)
                {
                    _length += 2; // "()"
                }
                Visit(binary.Right);
                break;
            case SubqueryValueExpression subquery:
                _length += 2; // "()"
                Visit(subquery.Expression);
                break;
            case FunctionValueExpression func:
                _length += func.Fullname.Length + 2; // name + "()"
                VisitCommaSeparatedValues(func.Parameters);
                break;
            case ArrayValueExpression array:
                _length += 2; // "[]"
                VisitCommaSeparatedValues(array.Values);
                break;
            case SetValueExpression set:
                _length += 2; // "{}"
                VisitCommaSeparatedValues(set.Values);
                break;
            case ObjectValueExpression obj:
                _length += 4; // "{ " + " }"
                int objIndex = 0;
                foreach (var (key, value) in obj.Fields)
                {
                    if (objIndex > 0)
                    {
                        _length += 2; // ", "
                    }
                    // key may be escaped with '"'
                    _length += key.Length + 4; // pessimistic: '"' + key + '"' + ": "
                    Visit(value);
                    objIndex++;
                }
                break;
            case RangeValueExpression range:
                if (range.BeginBoundType != RangeBoundType.Unbounded)
                {
                    bool appendBeginParens = range.BeginValue is not IConstantValueExpression;
                    if (appendBeginParens)
                    {
                        _length += 2; // "()"
                    }
                    Visit(range.BeginValue);
                }
                if (range.BeginBoundType == RangeBoundType.Exclusive)
                {
                    _length += 1; // ">"
                }
                _length += 2; // ".."
                if (range.EndBoundType == RangeBoundType.Inclusive)
                {
                    _length += 1; // "="
                }
                if (range.EndBoundType != RangeBoundType.Unbounded)
                {
                    bool appendEndParens = range.EndValue is not IConstantValueExpression;
                    if (appendEndParens)
                    {
                        _length += 2; // "()"
                    }
                    Visit(range.EndValue);
                }
                break;
            case CastValueExpression cast:
                _length += 3 + cast.Kind.Length; // "<" + kind + "> "
                Visit(cast.Value);
                break;
            case IfElseStatementValueExpression ifElse:
                bool isFirst = true;
                foreach (var (cond, then) in ifElse.Expressions)
                {
                    if (!isFirst)
                    {
                        _length += 5; // "ELSE "
                    }
                    _length += 8; // "IF " + " THEN "
                    Visit(cond);
                    Visit(then);
                    _length += 1; // trailing " "
                    isFirst = false;
                }
                if (ifElse.Else is not null)
                {
                    _length += 5; // "ELSE "
                    Visit(ifElse.Else);
                    _length += 1; // " "
                }
                _length += 3; // "END"
                break;
            case BlockValueExpression block:
                _length += 4; // "{ " + " }"
                foreach (var expr in block.Expressions)
                {
                    Visit(expr);
                    _length += 1; // ";"
                }
                break;
            default:
                // Unknown — use a pessimistic constant
                _length += 32;
                break;
        }

        return valueExpression;
    }

    private void VisitCommaSeparatedValues(
        System.Collections.Immutable.ImmutableArray<ValueExpression> values
    )
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (i > 0)
            {
                _length += 2; // ", "
            }
            Visit(values[i]);
        }
    }

    // ─── Helpers ────────────────────────────────────────────────────────────────

    private static int CountDigits(int value)
    {
        if (value == 0)
            return 1;
        int digits = value < 0 ? 1 : 0; // "-" sign
        long abs = Math.Abs((long)value);
        while (abs > 0)
        {
            digits++;
            abs /= 10;
        }
        return digits;
    }

    private static int CountDigits(long value)
    {
        if (value == 0)
            return 1;
        int digits = value < 0 ? 1 : 0; // "-" sign
        ulong abs = value == long.MinValue ? (ulong)long.MaxValue + 1 : (ulong)Math.Abs(value);
        while (abs > 0)
        {
            digits++;
            abs /= 10;
        }
        return digits;
    }

    private static int CountFloatLength(double value)
    {
        // InvariantCulture "R" or "G" format; pessimistic upper bound
        if (double.IsNaN(value) || double.IsInfinity(value))
            return 9; // "Infinity" / "-Infinity" / "NaN"
        return 20; // pessimistic: sign + digits + "." + digits + "E+NNN"
    }

    private static int GetOperatorLength(IOperator @operator)
    {
        return @operator switch
        {
            SimpleOperator simple => simple.Type switch
            {
                OperatorType.Neg => 1, // "-"
                OperatorType.Not => 1, // "!"
                OperatorType.Or => 2, // "||"
                OperatorType.And => 2, // "&&"
                OperatorType.Tco => 2, // "?:"
                OperatorType.Nco => 2, // "??"
                OperatorType.Add => 1, // "+"
                OperatorType.Sub => 1, // "-"
                OperatorType.Mul => 1, // "*"
                OperatorType.Div => 1, // "/"
                OperatorType.Rem => 1, // "%"
                OperatorType.Pow => 2, // "**"
                OperatorType.Inc => 2, // "+="
                OperatorType.Dec => 2, // "-="
                OperatorType.Ext => 3, // "+?="
                OperatorType.Equal => 1, // "="
                OperatorType.Exact => 2, // "=="
                OperatorType.NotEqual => 2, // "!="
                OperatorType.AllEqual => 2, // "*="
                OperatorType.AnyEqual => 2, // "?="
                OperatorType.Like => 1, // "~"
                OperatorType.NotLike => 2, // "!~"
                OperatorType.AllLike => 2, // "*~"
                OperatorType.AnyLike => 2, // "?~"
                OperatorType.LessThan => 1, // "<"
                OperatorType.LessThanOrEqual => 2, // "<="
                OperatorType.MoreThan => 1, // ">"
                OperatorType.MoreThanOrEqual => 2, // ">="
                OperatorType.Contain => 8, // "CONTAINS"
                OperatorType.NotContain => 11, // "CONTAINSNOT"
                OperatorType.ContainAll => 11, // "CONTAINSALL"
                OperatorType.ContainAny => 11, // "CONTAINSANY"
                OperatorType.ContainNone => 12, // "CONTAINSNONE"
                OperatorType.Inside => 6, // "INSIDE"
                OperatorType.NotInside => 9, // "NOTINSIDE"
                OperatorType.AllInside => 9, // "ALLINSIDE"
                OperatorType.AnyInside => 9, // "ANYINSIDE"
                OperatorType.NoneInside => 10, // "NONEINSIDE"
                OperatorType.Outside => 7, // "OUTSIDE"
                OperatorType.Intersects => 10, // "INTERSECTS"
                _ => 4, // fallback
            },
            MatchesOperator matches => matches.Reference.HasValue ? 3 : 2, // "@N@" or "@@"
            KnnOperator knn => knn.Distance.HasValue ? 12 : 6, // pessimistic "<|K,DISTANCE|>"
            AnnOperator => 10, // pessimistic "<|K,EF|>"
            _ => 4, // fallback
        };
    }

    /// <summary>Mirrors <c>QueryGeneratorExpressionVisitor.IsBindingPowerLowerThan</c>.</summary>
    private static bool IsBindingPowerLowerThan(
        ValueExpression valueExpression,
        IOperator @operator
    )
    {
        PowerBindingCategory? nestedCategory = valueExpression switch
        {
            BinaryValueExpression binary => GetBinaryPowerBindingCategory(binary.Operator),
            UnaryValueExpression => PowerBindingCategory.Unary,
            CastValueExpression => PowerBindingCategory.Cast,
            RangeValueExpression => PowerBindingCategory.Range,
            _ => null,
        };

        if (nestedCategory is null)
            return false;

        var currentCategory = GetBinaryPowerBindingCategory(@operator);
        if (currentCategory is null)
            return false;

        return nestedCategory < currentCategory;
    }

    private static PowerBindingCategory? GetBinaryPowerBindingCategory(IOperator @operator)
    {
        return @operator switch
        {
            SimpleOperator simple => simple.Type switch
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
}
