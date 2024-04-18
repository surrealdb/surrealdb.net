using System.Text;
using BenchmarkDotNet.Attributes;

namespace SurrealDb.Net.LocalBenchmarks;

/// <summary>
/// A simple benchmark to compare performance of various <c>RandomId</c> method implementations.
/// This method is used to generate a unique identifier to identify a unique websocket request/response pair.
///
/// Benchmark inspired by the work of Denis Voituron: https://dvoituron.com/2022/04/07/generate-small-unique-identifier/
/// </summary>
public class RandomIdBenchmark
{
    [Params(1, 20)]
    public int Iterations;

    private string[] _results = new string[20];

    [Benchmark(Baseline = true)]
    public string[] FromRandomInt()
    {
        for (int i = 0; i < Iterations; i++)
        {
            _results[i] = Random.Shared.Next().ToString("x");
        }
        return _results;
    }

    [Benchmark]
    public string[] FromRandomLong()
    {
        for (int i = 0; i < Iterations; i++)
        {
            _results[i] = Random.Shared.NextInt64().ToString("x");
        }
        return _results;
    }

    [Benchmark]
    public string[] FromRandomBytes()
    {
        for (int i = 0; i < Iterations; i++)
        {
            var bytes = new byte[8];
            Random.Shared.NextBytes(bytes);
            _results[i] = Encoding.UTF8.GetString(bytes);
        }
        return _results;
    }

    [Benchmark]
    public string[] FromGuid()
    {
        for (int i = 0; i < Iterations; i++)
        {
            _results[i] = Guid.NewGuid().ToString("N");
        }
        return _results;
    }

    [Benchmark]
    public string[] FromGuidShorted()
    {
        for (int i = 0; i < Iterations; i++)
        {
            _results[i] = Guid.NewGuid().ToString("N")[..8];
        }
        return _results;
    }

    [Benchmark]
    public string[] FromGuidBase64()
    {
        for (int i = 0; i < Iterations; i++)
        {
            _results[i] = Convert
                .ToBase64String(Guid.NewGuid().ToByteArray())
                .Replace("/", "_")
                .Replace("+", "-")[..8];
        }
        return _results;
    }

    [Benchmark]
    public string[] FromUlid()
    {
        for (int i = 0; i < Iterations; i++)
        {
            _results[i] = Ulid.NewUlid().ToString();
        }
        return _results;
    }

    [Benchmark]
    public string[] FromUlidShorted()
    {
        for (int i = 0; i < Iterations; i++)
        {
            _results[i] = Ulid.NewUlid().ToString()[..8];
        }
        return _results;
    }
}
