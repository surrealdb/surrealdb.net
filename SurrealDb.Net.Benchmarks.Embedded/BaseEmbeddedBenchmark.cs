using SurrealDb.Net.Tests.Fixtures;

namespace SurrealDb.Net.Benchmarks.Embedded;

public class BaseEmbeddedBenchmark : BaseBenchmark
{
    protected DatabaseInfo DefaultDatabaseInfo { get; } =
        new() { Namespace = "test", Database = "test" };
}
