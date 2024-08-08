using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net.Exceptions;

/// <summary>
/// Generated exception when the response from the SurrealDb query is an unexpected error.
/// </summary>
public class SurrealDbErrorResultException : Exception
{
    public SurrealDbErrorResultException()
        : base(GetErrorMessage()) { }

    public SurrealDbErrorResultException(ISurrealDbErrorResult errorResult)
        : base(GetErrorMessage(errorResult)) { }

    private static string GetErrorMessage(ISurrealDbErrorResult? errorResult = null)
    {
        switch (errorResult)
        {
            case SurrealDbErrorResult defaultErrorResult:
                return defaultErrorResult.Details;
            case SurrealDbProtocolErrorResult protocolErrorResult:
                return $"{protocolErrorResult.Details}\n{protocolErrorResult.Information}";
            default:
                return "The response from the SurrealDb query was an unexpected error.";
        }
    }
}
