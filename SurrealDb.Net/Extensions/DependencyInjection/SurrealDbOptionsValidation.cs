using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SurrealDb.Net.Internals.Auth;
using SurrealDb.Net.Internals.Constants;

namespace SurrealDb.Net.Extensions.DependencyInjection;

internal sealed class SurrealDbOptionsValidation : IValidateOptions<SurrealDbOptions>
{
    private readonly ILogger<SurrealDbOptionsValidation> _logger;

    public SurrealDbOptionsValidation(ILogger<SurrealDbOptionsValidation> logger)
    {
        _logger = logger;
    }

    public ValidateOptionsResult Validate(string? name, SurrealDbOptions options)
    {
        _logger.LogInformation("Validating options for: '{}'", nameof(SurrealDbOptions));

        if (string.IsNullOrWhiteSpace(options.Endpoint))
        {
            return ValidateOptionsResult.Fail("Endpoint is required.");
        }

        if (!IsValidEndpoint(options.Endpoint))
        {
            return ValidateOptionsResult.Fail("Endpoint should be a valid URL.");
        }

        return ValidateOptionsResult.Success;
    }

    internal static bool IsValidEndpoint(string endpoint)
    {
        return IsValidServerEndpoint(endpoint) || IsValidClientEndpoint(endpoint);
    }

    internal static bool IsValidServerEndpoint(string endpoint)
    {
        string[] validServerEndpoints =
        [
            EndpointConstants.Server.HTTP,
            EndpointConstants.Server.HTTPS,
            EndpointConstants.Server.WS,
            EndpointConstants.Server.WSS,
        ];

        return validServerEndpoints.Any(vse =>
            endpoint.StartsWith(vse, StringComparison.OrdinalIgnoreCase)
        );
    }

    internal static bool IsValidClientEndpoint(string endpoint)
    {
        string[] validClientEndpoints =
        [
            EndpointConstants.Client.MEMORY,
            EndpointConstants.Client.ROCKSDB,
            EndpointConstants.Client.SURREALKV,
        ];

        return validClientEndpoints.Any(vce =>
            endpoint.StartsWith(vce, StringComparison.OrdinalIgnoreCase)
        );
    }

    internal static bool IsValidAuthLevel(string authMode)
    {
        return Enum.TryParse<SystemAuthLevel>(authMode, out _);
    }
}
