using SurrealDb.Net.Models.Errors;

namespace SurrealDb.Net.Internals.Extensions;

internal static class RpcErrorKindExtensions
{
    private static readonly IReadOnlyDictionary<long, RpcErrorKind> _codeToKind = new Dictionary<
        long,
        RpcErrorKind
    >
    {
        { -32700, RpcErrorKind.Validation },
        { -32600, RpcErrorKind.Validation },
        { -32601, RpcErrorKind.NotFound },
        { -32602, RpcErrorKind.NotAllowed },
        { -32603, RpcErrorKind.Validation },
        { -32604, RpcErrorKind.Configuration },
        { -32605, RpcErrorKind.Configuration },
        { -32606, RpcErrorKind.Configuration },
        { -32000, RpcErrorKind.Internal },
        { -32001, RpcErrorKind.Connection },
        { -32002, RpcErrorKind.NotAllowed },
        { -32003, RpcErrorKind.Query },
        { -32004, RpcErrorKind.Query },
        { -32005, RpcErrorKind.Query },
        { -32006, RpcErrorKind.Thrown },
        { -32007, RpcErrorKind.Serialization },
        { -32008, RpcErrorKind.Serialization },
    };

    public static RpcErrorKind From(string? kind, long? code)
    {
        if (!string.IsNullOrWhiteSpace(kind) && Enum.TryParse<RpcErrorKind>(kind, out var result))
        {
            return result;
        }

        if (code.HasValue)
        {
            return _codeToKind.GetValueOrDefault(code.Value, RpcErrorKind.Internal);
        }

        return RpcErrorKind.Internal;
    }
}
