namespace SurrealDb.Instrumentation.Internals;

internal static class SemanticConventions
{
    public const string AttributeRpcSystem = "rpc.system";
    public const string AttributeRpcService = "rpc.service";
    public const string AttributeRpcMethod = "rpc.method";
    public const string AttributeRpcGrpcStatusCode = "rpc.grpc.status_code";

    public const string AttributeExceptionEventName = "exception";
    public const string AttributeExceptionType = "exception.type";
    public const string AttributeExceptionMessage = "exception.message";
    public const string AttributeExceptionStacktrace = "exception.stacktrace";
    public const string AttributeErrorType = "error.type";

    // v1.23.0
    // https://github.com/open-telemetry/semantic-conventions/blob/v1.23.0/docs/http/http-metrics.md#http-server
    public const string AttributeClientAddress = "client.address";
    public const string AttributeClientPort = "client.port";
    public const string AttributeNetworkProtocolVersion = "network.protocol.version"; // replaces: "http.flavor" (AttributeHttpFlavor)
    public const string AttributeNetworkProtocolName = "network.protocol.name";
    public const string AttributeServerAddress = "server.address"; // replaces: "net.host.name" (AttributeNetHostName)
    public const string AttributeServerPort = "server.port"; // replaces: "net.host.port" (AttributeNetHostPort)
    public const string AttributeUserAgentOriginal = "user_agent.original"; // replaces: http.user_agent (AttributeHttpUserAgent)

    // v1.36.0 database conventions:
    // https://github.com/open-telemetry/semantic-conventions/tree/v1.36.0/docs/database
    public const string AttributeDbCollectionName = "db.collection.name";
    public const string AttributeDbOperationName = "db.operation.name";
    public const string AttributeDbSystemName = "db.system.name";
    public const string AttributeDbNamespace = "db.namespace";
    public const string AttributeDbResponseStatusCode = "db.response.status_code";
    public const string AttributeDbOperationBatchSize = "db.operation.batch.size";
    public const string AttributeDbQuerySummary = "db.query.summary";
    public const string AttributeDbQueryText = "db.query.text";
    public const string AttributeDbStoredProcedureName = "db.stored_procedure.name";
}
