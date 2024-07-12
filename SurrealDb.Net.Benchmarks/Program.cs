using System.Text.Json.Serialization;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Environments;
using BenchmarkDotNet.Exporters.Json;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using SurrealDb.Net.Benchmarks.Constants;
using SurrealDb.Net.Benchmarks.Helpers;

var config = DefaultConfig
    .Instance.AddJob(Job.Default.WithRuntime(CoreRuntime.Core80))
    .AddJob(
        Job.Default.WithRuntime(NativeAotRuntime.Net80)
            .WithEnvironmentVariable(EnvVariablesConstants.NativeAotRuntime, "true")
    )
    .AddDiagnoser(MemoryDiagnoser.Default)
    .AddExporter(JsonExporter.Full)
    .HideColumns(Column.EnvironmentVariables);

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);

BenchmarkHelper.CombineBenchmarkResults();

[JsonSerializable(typeof(IEnumerable<Post>))]
[JsonSerializable(typeof(Address))]
[JsonSerializable(typeof(Customer))]
[JsonSerializable(typeof(Product))]
[JsonSerializable(typeof(IEnumerable<ProductAlsoPurchased>))]
internal partial class AppJsonSerializerContext : JsonSerializerContext;
