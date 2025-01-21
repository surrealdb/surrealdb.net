namespace SurrealDb.Net.Tests.Fixtures;

public class ConnectionStringFixtureGeneratorAttribute : DataSourceGeneratorAttribute<string>
{
    public override IEnumerable<Func<string>> GenerateDataSources(
        DataGeneratorMetadata dataGeneratorMetadata
    )
    {
        yield return () => "Endpoint=mem://";
        yield return () => "Endpoint=rocksdb://";
        yield return () => "Endpoint=surrealkv://";
        yield return () => "Endpoint=http://127.0.0.1:8000;User=root;Pass=root";
        yield return () => "Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root";
    }
}

public class RemoteConnectionStringFixtureGeneratorAttribute : DataSourceGeneratorAttribute<string>
{
    public override IEnumerable<Func<string>> GenerateDataSources(
        DataGeneratorMetadata dataGeneratorMetadata
    )
    {
        yield return () => "Endpoint=http://127.0.0.1:8000;User=root;Pass=root";
        yield return () => "Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root";
    }
}

public class EmbeddedConnectionStringFixtureGeneratorAttribute
    : DataSourceGeneratorAttribute<string>
{
    public override IEnumerable<Func<string>> GenerateDataSources(
        DataGeneratorMetadata dataGeneratorMetadata
    )
    {
        yield return () => "Endpoint=mem://";
        yield return () => "Endpoint=rocksdb://";
        yield return () => "Endpoint=surrealkv://";
    }
}
