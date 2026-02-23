namespace SurrealDb.Net.Tests.Fixtures;

public class ConnectionStringFixtureGeneratorAttribute : DataSourceGeneratorAttribute<string>
{
    private static readonly EmbeddedConnectionStringFixtureGeneratorAttribute _embeddedConnectionStringFixtureGeneratorAttribute =
        new();
    private static readonly RemoteConnectionStringFixtureGeneratorAttribute _remoteConnectionStringFixtureGeneratorAttribute =
        new();

    protected override IEnumerable<Func<string>> GenerateDataSources(
        DataGeneratorMetadata dataGeneratorMetadata
    )
    {
        foreach (
            var cs in _embeddedConnectionStringFixtureGeneratorAttribute.GenerateDataSourcesPublicly()
        )
        {
            yield return cs;
        }
        foreach (
            var cs in _remoteConnectionStringFixtureGeneratorAttribute.GenerateDataSourcesPublicly(
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
    private static readonly WebsocketConnectionStringFixtureGeneratorAttribute _websocketConnectionStringFixtureGeneratorAttribute =
        new();

    protected override IEnumerable<Func<string>> GenerateDataSources(
        DataGeneratorMetadata dataGeneratorMetadata
    )
    {
        return GenerateDataSourcesPublicly(dataGeneratorMetadata);
    }

    public IEnumerable<Func<string>> GenerateDataSourcesPublicly(
        DataGeneratorMetadata dataGeneratorMetadata
    )
    {
        yield return () => "Endpoint=http://127.0.0.1:8000;User=root;Pass=root";
        foreach (
            var cs in _websocketConnectionStringFixtureGeneratorAttribute.GenerateDataSourcesPublicly()
        )
        {
            yield return cs;
        }
    }
}

public class WebsocketConnectionStringFixtureGeneratorAttribute
    : DataSourceGeneratorAttribute<string>
{
    protected override IEnumerable<Func<string>> GenerateDataSources(
        DataGeneratorMetadata dataGeneratorMetadata
    )
    {
        return GenerateDataSourcesPublicly();
    }

    public IEnumerable<Func<string>> GenerateDataSourcesPublicly()
    {
        yield return () => "Endpoint=ws://127.0.0.1:8000/rpc;User=root;Pass=root";
    }
}

public class EmbeddedConnectionStringFixtureGeneratorAttribute
    : DataSourceGeneratorAttribute<string>
{
    protected override IEnumerable<Func<string>> GenerateDataSources(
        DataGeneratorMetadata dataGeneratorMetadata
    )
    {
        return GenerateDataSourcesPublicly();
    }

    public IEnumerable<Func<string>> GenerateDataSourcesPublicly()
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
