namespace SurrealDb.Net.Internals.Queryable.Expressions.Surreal;

internal enum KnnDistance
{
    Chebyshev = 1,
    Cosine,
    Euclidean,
    Hamming,
    Jaccard,
    Manhattan,
    Minkowski,
    Pearson,
}
