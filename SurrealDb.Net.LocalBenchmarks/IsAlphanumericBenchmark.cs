using BenchmarkDotNet.Attributes;

namespace SurrealDb.Net.LocalBenchmarks;

/// <summary>
/// A simple benchmark to compare performance of various <c>IsAlphanumeric</c> method implementations.
/// </summary>
public class IsAlphanumericBenchmark
{
    [Params("alphabetic", "123456789", "with white spaces", "", "      ")]
    public string? Param;

    [Benchmark]
    public bool Standard()
    {
        string str = Param!;

        foreach (char c in str)
        {
            if (!char.IsLetterOrDigit(c))
            {
                return false;
            }
        }

        return true;
    }

    [Benchmark]
    public bool Linq()
    {
        string str = Param!;
        return str.All(char.IsLetterOrDigit);
    }
}
