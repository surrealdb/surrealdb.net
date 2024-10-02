using Microsoft.Extensions.Logging;
using SurrealDb.Net.Internals.Logging;

namespace SurrealDb.Net.Extensions.DependencyInjection;

public interface ISurrealDbLoggerFactory
{
    ILogger? Connection { get; }
    ILogger? Method { get; }
    ILogger? Query { get; }
}

internal sealed class SurrealDbLoggerFactory : ISurrealDbLoggerFactory
{
    public ILogger? Connection { get; }
    public ILogger? Method { get; }
    public ILogger? Query { get; }

    public SurrealDbLoggerFactory(ILoggerFactory loggerFactory)
    {
        Connection = loggerFactory.CreateLogger(DbLoggerCategory.Connection.Name);
        Method = loggerFactory.CreateLogger(DbLoggerCategory.Method.Name);
        Query = loggerFactory.CreateLogger(DbLoggerCategory.Query.Name);
    }
}
