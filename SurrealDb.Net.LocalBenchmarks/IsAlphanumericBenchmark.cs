﻿using BenchmarkDotNet.Attributes;

namespace SurrealDb.Net.LocalBenchmarks;

/// <summary>
/// A simple benchmark to compare performance of various `IsAlphanumeric` method implementations.
/// </summary>
public class IsAlphanumericBenchmark
{
    [Params("alphabetic", "123456789", "with white spaces", "", "      ")]
    public string? Param;

    [Benchmark]
    public bool Standard()
    {
        string str = Param!;

        for (int i = 0; i < str.Length; i++)
        {
            char c = str[i];
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
