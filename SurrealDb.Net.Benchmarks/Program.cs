using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Running;
using SurrealDb.Net.Benchmarks.Helpers;

var config = DefaultConfig.Instance
	.AddDiagnoser(MemoryDiagnoser.Default)
	.AddExporter(JsonExporter.Full);

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);

BenchmarkHelper.CombineBenchmarkResults();
