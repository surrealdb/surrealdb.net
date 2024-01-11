using System.Text;

namespace SurrealDb.Net.Internals.Queryable.Expressions.Surreal;

// 💡 Here is a list of non-supported operators:
// * "?:" - conditional ternary operator (https://docs.surrealdb.com/docs/surrealql/operators#tco)
// * "=" - simple equality operator without type matching (https://docs.surrealdb.com/docs/surrealql/operators#equal)
// * "?=" - value existence in array (https://docs.surrealdb.com/docs/surrealql/operators#anyequal)
// * "*=" - all values are the same in array (https://docs.surrealdb.com/docs/surrealql/operators#allequal)
// * "~" - fuzzy matching (https://docs.surrealdb.com/docs/surrealql/operators#match)
// * "!~" - inequality fuzzy matching (https://docs.surrealdb.com/docs/surrealql/operators#notmatch)
// * "?~" - value existence in array using fuzzy matching (https://docs.surrealdb.com/docs/surrealql/operators#notmatch)
// * "*~" - all values are the same in array using fuzzy matching (https://docs.surrealdb.com/docs/surrealql/operators#allmatch)

internal interface IOperator : IPrintableExpression
{
    OperatorType Type { get; }
}

internal readonly struct SimpleOperator : IOperator
{
    public OperatorType Type { get; }

    public SimpleOperator(OperatorType type)
    {
        if (type is OperatorType.Matches or OperatorType.Knn or OperatorType.Ann)
        {
            throw new ArgumentOutOfRangeException(
                nameof(type),
                type,
                "Simple operator type could not be one of 'MATCHES', 'KNN' or 'ANN'."
            );
        }

        Type = type;
    }

    public void AppendTo(StringBuilder stringBuilder)
    {
        string str = Type switch
        {
            OperatorType.Neg => "-",
            OperatorType.Not => "!",
            OperatorType.Or => "||",
            OperatorType.And => "&&",
            OperatorType.Tco => "?:",
            OperatorType.Nco => "??",
            OperatorType.Add => "+",
            OperatorType.Sub => "-",
            OperatorType.Mul => "*",
            OperatorType.Div => "/",
            OperatorType.Rem => "%",
            OperatorType.Pow => "**",
            OperatorType.Inc => "+=",
            OperatorType.Dec => "-=",
            OperatorType.Ext => "+?=",
            OperatorType.Equal => "=",
            OperatorType.Exact => "==",
            OperatorType.NotEqual => "!=",
            OperatorType.AllEqual => "*=",
            OperatorType.AnyEqual => "?=",
            OperatorType.Like => "~",
            OperatorType.NotLike => "!~",
            OperatorType.AllLike => "*~",
            OperatorType.AnyLike => "?~",
            OperatorType.LessThan => "<",
            OperatorType.LessThanOrEqual => "<=",
            OperatorType.MoreThan => ">",
            OperatorType.MoreThanOrEqual => ">=",
            OperatorType.Contain => "CONTAINS",
            OperatorType.NotContain => "CONTAINSNOT",
            OperatorType.ContainAll => "CONTAINSALL",
            OperatorType.ContainAny => "CONTAINSANY",
            OperatorType.ContainNone => "CONTAINSNONE",
            OperatorType.Inside => "INSIDE",
            OperatorType.NotInside => "NOTINSIDE",
            OperatorType.AllInside => "ALLINSIDE",
            OperatorType.AnyInside => "ANYINSIDE",
            OperatorType.NoneInside => "NONEINSIDE",
            OperatorType.Outside => "OUTSIDE",
            OperatorType.Intersects => "INTERSECTS",
            _ => throw new ArgumentOutOfRangeException(nameof(Type)),
        };

        stringBuilder.Append(str);
    }
}

internal readonly struct MatchesOperator : IOperator
{
    public OperatorType Type => OperatorType.Matches;
    public int? Reference { get; }

    public MatchesOperator(int? reference)
    {
        Reference = reference;
    }

    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append('@');
        if (Reference.HasValue)
        {
            stringBuilder.Append(Reference.Value);
        }
        stringBuilder.Append('@');
    }
}

internal readonly struct KnnOperator : IOperator
{
    public OperatorType Type => OperatorType.Knn;
    public int K { get; }
    public KnnDistance? Distance { get; }
    public int? MinkowskiOrder { get; }

    public KnnOperator(int k, KnnDistance? distance, int? minkowskiOrder = null)
    {
        K = k;
        Distance = distance;
        MinkowskiOrder = minkowskiOrder;
    }

    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append("<|");
        stringBuilder.Append(K);
        if (Distance.HasValue)
        {
            stringBuilder.Append(',');
            stringBuilder.Append(DistanceToString(Distance, MinkowskiOrder));
        }
        stringBuilder.Append("|>");
    }

    private static string DistanceToString(KnnDistance? distance, int? minkowskiOrder)
    {
        return distance switch
        {
            KnnDistance.Chebyshev => "CHEBYSHEV",
            KnnDistance.Cosine => "COSINE",
            KnnDistance.Euclidean => "EUCLIDEAN",
            KnnDistance.Hamming => "HAMMING",
            KnnDistance.Jaccard => "JACCARD",
            KnnDistance.Manhattan => "MANHATTAN",
            KnnDistance.Minkowski => $"MINKOWSKI {minkowskiOrder}",
            KnnDistance.Pearson => "PEARSON",
            _ => throw new ArgumentOutOfRangeException(nameof(distance)),
        };
    }
}

internal readonly struct AnnOperator : IOperator
{
    public OperatorType Type => OperatorType.Ann;
    public int K { get; }
    public int Ef { get; }

    public AnnOperator(int k, int ef)
    {
        K = k;
        Ef = ef;
    }

    public void AppendTo(StringBuilder stringBuilder)
    {
        stringBuilder.Append("<|");
        stringBuilder.Append(K);
        stringBuilder.Append(',');
        stringBuilder.Append(Ef);
        stringBuilder.Append("|>");
    }
}

internal enum OperatorType
{
    Neg = 1,
    Not,
    Or,
    And,
    Tco,
    Nco,
    Add,
    Sub,
    Mul,
    Div,
    Pow,
    Inc,
    Dec,
    Ext,
    Equal,
    Exact,
    NotEqual,
    AllEqual,
    AnyEqual,
    Like,
    NotLike,
    AllLike,
    AnyLike,
    Matches, // Complex
    LessThan,
    LessThanOrEqual,
    MoreThan,
    MoreThanOrEqual,
    Contain,
    NotContain,
    ContainAll,
    ContainAny,
    ContainNone,
    Inside,
    NotInside,
    AllInside,
    AnyInside,
    NoneInside,
    Outside,
    Intersects,
    Knn, // Complex
    Ann, // Complex
    Rem,
}
