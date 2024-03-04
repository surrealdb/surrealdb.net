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

    [Benchmark]
    public Dictionary<DurationUnit, int> Superpower()
    {
        return SuperpowerDurationParser.Parser
            .Parse(Param)
            .ToDictionary(kv => kv.unit, kv => kv.value);
    }

    [Benchmark]
    public Dictionary<DurationUnit, int> Pidgin()
    {
        return PidginDurationParser.Parser
            .ParseOrThrow(Param)
            .ToDictionary(kv => kv.unit, kv => kv.value);
    }
}
