using Dahomey.Cbor.Serialization;
using Dahomey.Cbor.Serialization.Converters;

namespace SurrealDb.Net.Internals.Cbor.Converters;

internal static class RpcParametersWriter
{
    /// <summary>
    /// Writes the parameters of an RPC request as a CBOR array.
    /// Each parameter is written with the converter of its runtime type.
    /// </summary>
    public static void Write(
        ref CborWriter writer,
        object?[] parameters,
        ICborConverter<object?> parameterConverter
    )
    {
        writer.WriteBeginArray(parameters.Length);

        foreach (var parameter in parameters)
        {
            parameterConverter.Write(ref writer, parameter);
        }
    }
}
