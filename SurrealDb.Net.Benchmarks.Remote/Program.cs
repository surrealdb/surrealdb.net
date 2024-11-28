using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using SurrealDb.Net.Benchmarks.Constants;
using SurrealDb.Net.Benchmarks.Helpers;

const bool enableNativeAotBenchmarks = false;

var config = DefaultConfig.Instance.AddJob(Job.Default.WithRuntime(CoreRuntime.Core90));

if (enableNativeAotBenchmarks)
{
#pragma warning disable CS0162 // Unreachable code detected
    config = config.AddJob(
        Job.Default.WithRuntime(NativeAotRuntime.Net80)
            .WithEnvironmentVariable(EnvVariablesConstants.NativeAotRuntime, "true")
    );
#pragma warning restore CS0162 // Unreachable code detected
}

config = config
    .AddDiagnoser(MemoryDiagnoser.Default)
    .AddExporter(JsonExporter.Full)
    .HideColumns(Column.EnvironmentVariables);

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);

BenchmarkHelper.CombineBenchmarkResults();
