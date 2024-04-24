using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using SurrealDb.Net.Benchmarks.Helpers;

var config = DefaultConfig
    .Instance.AddJob(Job.Default.WithRuntime(CoreRuntime.Core80))
    .AddDiagnoser(MemoryDiagnoser.Default)
    .AddExporter(JsonExporter.Full)
    .HideColumns(Column.EnvironmentVariables);

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);

BenchmarkHelper.CombineBenchmarkResults();
