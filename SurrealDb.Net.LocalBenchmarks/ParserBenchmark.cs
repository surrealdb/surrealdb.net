using BenchmarkDotNet.Attributes;
using Pidgin;
using Superpower;
using SurrealDb.Net.LocalBenchmarks.Models;
using SurrealDb.Net.LocalBenchmarks.Parsers;

namespace SurrealDb.Net.LocalBenchmarks;

public class ParserBenchmark
{
    [Params("0ns", "5y4w3h115ms4ns")]
    public string Param = string.Empty;

    private (long? Seconds, int? Nanos) _paramAsArray = (null, null);

    [GlobalSetup]
    public void GlobalSetup()
    {
        _paramAsArray = Param switch
        {
            "0ns" => (null, null),
            "5y4w3h115ms4ns" => (160_110_000, 115_000_004),
            _ => throw new ArgumentOutOfRangeException(nameof(Param)),
        };
    }

    [Benchmark(Baseline = true)]
    public Dictionary<DurationUnit, int> Superpower()
    {
        return SuperpowerDurationParser
            .Parser.Parse(Param)
            .ToDictionary(kv => kv.unit, kv => kv.value);
    }

    [Benchmark]
    public Dictionary<DurationUnit, int> Pidgin()
    {
        return PidginDurationParser
            .Parser.ParseOrThrow(Param)
            .ToDictionary(kv => kv.unit, kv => kv.value);
    }

    [Benchmark]
    public Dictionary<DurationUnit, int> FromSpan()
    {
        return FromSpanDurationParser.Parse(Param);
    }

    [Benchmark]
    public Dictionary<DurationUnit, int> FromArray()
    {
        return FromArrayDurationParser.Parse(_paramAsArray.Seconds, _paramAsArray.Nanos);
    }
}
