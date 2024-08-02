using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using SurrealDb.Net.Handlers;
using SurrealDb.Net.Internals.Extensions;

namespace SurrealDb.Net.LocalBenchmarks;

public class QueryFormatterBenchmark
{
    private const string TABLE = "test";

    [Params(1, 100)]
    public int Count;

    [Params("Interpolation", "NoInterpolation")]
    public string Mode = string.Empty;

    [Benchmark]
    public List<(string, IReadOnlyDictionary<string, object?>)> FormattableString()
    {
        var list = new List<(string, IReadOnlyDictionary<string, object?>)>(Count);

        for (int index = 0; index < Count; index++)
        {
            if (Mode == "NoInterpolation")
            {
                list.Add(
                    ExtractRawQueryParams(
                        $"""
                        DEFINE TABLE test;

                        CREATE test SET value = 5;
                        UPDATE test SET value = 10;
                        DELETE test;

                        SELECT * FROM test;
                        """
                    )
                );
            }
            else
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
        }

        return list;
    }

    [Benchmark]
    public List<(string, IReadOnlyDictionary<string, object?>)> QueryInterpolatedStringHandler()
    {
        var list = new List<(string, IReadOnlyDictionary<string, object?>)>(Count);

        for (int index = 0; index < Count; index++)
        {
            if (Mode == "NoInterpolation")
            {
                list.Add(
                    HandleQuery(
                        $"""
                        DEFINE TABLE test;

                        CREATE test SET value = 5;
                        UPDATE test SET value = 10;
                        DELETE test;

                        SELECT * FROM test;
                        """
                    )
                );
            }
            else
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
        return (handler.FormattedText, handler.Parameters);
    }
}
