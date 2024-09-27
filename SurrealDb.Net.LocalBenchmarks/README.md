# SurrealDb.Net.LocalBenchmarks

A set of benchmarks to compare different implementations of methods to find the best implementation in terms of CPU and memory usage. And also ensuring regression testing in performance through .NET versions.

Important rules regarding local benchmarks implementation:

* Tested code must be part of a hot path (e.g. part of a single rpc method like `Create`)
* Benchmark params must be as close to reality as possible, also providing the wider ranger of examples possible
* `Baseline` should display the adopted implementation