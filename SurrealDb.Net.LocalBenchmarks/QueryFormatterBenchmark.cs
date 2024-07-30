using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using SurrealDb.Net.Handlers;
using SurrealDb.Net.Internals.Extensions;

namespace SurrealDb.Net.LocalBenchmarks;

public class QueryFormatterBenchmark
{
    private const string TABLE = "test";

    [Params(1, 100)]
    public int Param;

    [Benchmark]
    public List<(string, IReadOnlyDictionary<string, object?>)> FormattableString()
    {
        var list = new List<(string, IReadOnlyDictionary<string, object?>)>(Param);

        for (int index = 0; index < Param; index++)
        {
            list.Add(
                ExtractRawQueryParams(
                    $"""
                    DEFINE TABLE {TABLE};

                    CREATE {TABLE} SET value = {5};
                    UPDATE {TABLE} SET value = {10};
                    DELETE {TABLE};

                    SELECT * FROM {TABLE};
                    """
                )
            );
        }

        return list;
    }

    [Benchmark]
    public List<(string, IReadOnlyDictionary<string, object?>)> QueryInterpolatedStringHandler()
    {
        var list = new List<(string, IReadOnlyDictionary<string, object?>)>(Param);

        for (int index = 0; index < Param; index++)
        {
            list.Add(
                HandleQuery(
                    $"""
                    DEFINE TABLE {TABLE};

                    CREATE {TABLE} SET value = {5};
                    UPDATE {TABLE} SET value = {10};
                    DELETE {TABLE};

                    SELECT * FROM {TABLE};
                    """
                )
            );
        }

        return list;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private (string, IReadOnlyDictionary<string, object?>) ExtractRawQueryParams(
        FormattableString formattableString
    )
    {
        return formattableString.ExtractRawQueryParams();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private (string, IReadOnlyDictionary<string, object?>) HandleQuery(
        QueryInterpolatedStringHandler handler
    )
    {
        return (handler.GetFormattedText(), handler.GetParameters());
    }
}
