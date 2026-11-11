namespace SurrealDb.Net.Internals.Queryable;

internal struct CachedQueryKey
{
    public string MemberName { get; set; }
    public string SourceFilePath { get; set; }
    public int SourceLineNumber { get; set; }
}

internal sealed class CachedQueryKeyComparer : IEqualityComparer<CachedQueryKey>
{
    public bool Equals(CachedQueryKey x, CachedQueryKey y)
    {
        return x.MemberName == y.MemberName
            && x.SourceFilePath == y.SourceFilePath
            && x.SourceLineNumber == y.SourceLineNumber;
    }

    public int GetHashCode(CachedQueryKey obj)
    {
        return HashCode.Combine(obj.MemberName, obj.SourceFilePath, obj.SourceLineNumber);
    }
}
