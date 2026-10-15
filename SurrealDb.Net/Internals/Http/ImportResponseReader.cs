using System.Text;
using System.Text.Json;
using Dahomey.Cbor;
using SurrealDb.Net.Exceptions.Methods;
using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net.Internals.Http;

internal static class ImportResponseReader
{
    private const int MAX_ERRORS_IN_MESSAGE = 3;

    /// <summary>
    /// Throws a <see cref="SurrealDbImportException"/> when the import failed,
    /// with the statements that failed when the server reports them.
    /// </summary>
    public static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CborOptions cborOptions,
        CancellationToken cancellationToken
    )
    {
#if NET5_0_OR_GREATER
        byte[] body = await response
            .Content.ReadAsByteArrayAsync(cancellationToken)
            .ConfigureAwait(false);
#else
        byte[] body = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
#endif

        // 💡 Media types are case-insensitive
        bool isCbor = string.Equals(
            response.Content.Headers.ContentType?.MediaType,
            "application/cbor",
            StringComparison.OrdinalIgnoreCase
        );

        // 💡 Failed statements are listed as query results, other failures as an error message
        var errors = isCbor
            ? ReadErrorResults(body, cborOptions)
            : Array.Empty<ISurrealDbErrorResult>();

        if (response.IsSuccessStatusCode && errors.Count == 0)
        {
            return;
        }

        string message =
            errors.Count > 0
                ? FormatErrors(errors)
                : $"The import failed: {ReadErrorMessage(body, isCbor) ?? response.ReasonPhrase}";

        throw new SurrealDbImportException(response.StatusCode, errors, message);
    }

    private static IReadOnlyList<ISurrealDbErrorResult> ReadErrorResults(
        byte[] body,
        CborOptions cborOptions
    )
    {
        if (body.Length == 0)
        {
            return Array.Empty<ISurrealDbErrorResult>();
        }

        try
        {
            return Dahomey
                .Cbor.Cbor.Deserialize<List<ISurrealDbResult>>(body, cborOptions)
                .OfType<ISurrealDbErrorResult>()
                .ToList();
        }
        catch (Exception)
        {
            return Array.Empty<ISurrealDbErrorResult>();
        }
    }

    private static string? ReadErrorMessage(byte[] body, bool isCbor)
    {
        if (body.Length == 0)
        {
            return null;
        }

        if (!isCbor)
        {
            try
            {
                using var document = JsonDocument.Parse(body);
                var root = document.RootElement;

                if (root.ValueKind == JsonValueKind.Object)
                {
                    foreach (string property in new[] { "information", "description", "details" })
                    {
                        if (
                            root.TryGetProperty(property, out var value)
                            && value.ValueKind == JsonValueKind.String
                        )
                        {
                            return value.GetString();
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // 💡 Not JSON, the body is the message
            }
        }

        return Encoding.UTF8.GetString(body);
    }

    private static string FormatErrors(IReadOnlyList<ISurrealDbErrorResult> errors)
    {
        var builder = new StringBuilder();
        builder.Append(
            errors.Count == 1
                ? "1 statement failed during the import:"
                : $"{errors.Count} statements failed during the import:"
        );

        foreach (var error in errors.Take(MAX_ERRORS_IN_MESSAGE))
        {
            builder.Append("\n- ").Append(GetDetails(error));
        }

        if (errors.Count > MAX_ERRORS_IN_MESSAGE)
        {
            builder.Append($"\n- and {errors.Count - MAX_ERRORS_IN_MESSAGE} more");
        }

        return builder.ToString();
    }

    private static string GetDetails(ISurrealDbErrorResult error)
    {
        return error switch
        {
            SurrealDbErrorResult errorResult => errorResult.Details,
            SurrealDbProtocolErrorResult protocolErrorResult => protocolErrorResult.Details,
            _ => error.ToString() ?? string.Empty,
        };
    }
}
