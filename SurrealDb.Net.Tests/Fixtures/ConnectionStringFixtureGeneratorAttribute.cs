namespace SurrealDb.Net.Tests.Fixtures;

public class ConnectionStringFixtureGeneratorAttribute : DataSourceGeneratorAttribute<string>
{
    private static readonly EmbeddedConnectionStringFixtureGeneratorAttribute _embeddedConnectionStringFixtureGeneratorAttribute =
        new();
    private static readonly RemoteConnectionStringFixtureGeneratorAttribute _remoteConnectionStringFixtureGeneratorAttribute =
        new();

    public override IEnumerable<Func<string>> GenerateDataSources(
        DataGeneratorMetadata dataGeneratorMetadata
    )
    {
        foreach (
            var cs in _embeddedConnectionStringFixtureGeneratorAttribute.GenerateDataSources(
                dataGeneratorMetadata
            )
        )
        {
            yield return cs;
        }
        foreach (
            var cs in _remoteConnectionStringFixtureGeneratorAttribute.GenerateDataSources(
                dataGeneratorMetadata
            )
        )
        {
            yield return cs;
        }
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
#if EMBEDDED_MODE
        yield return () => "Endpoint=mem://";
        yield return () => "Endpoint=rocksdb://";
        yield return () => "Endpoint=surrealkv://";
#else
        return [];
#endif
    }
}
