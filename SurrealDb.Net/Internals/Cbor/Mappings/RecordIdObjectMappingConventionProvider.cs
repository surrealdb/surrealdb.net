using System;
using Dahomey.Cbor.Serialization.Converters.Mappings;
using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Cbor.Mappings;

/// <summary>
/// Supplies a <see cref="RecordIdObjectMappingConvention"/> for every type implementing
/// <see cref="IRecord"/>, so the CBOR representation of its <c>Id</c> member is always correct
/// regardless of whether the concrete type redeclares the attributes carried by
/// <see cref="IRecord.Id"/>.
/// </summary>
internal sealed class RecordIdObjectMappingConventionProvider : IObjectMappingConventionProvider
{
    private readonly DefaultObjectMappingConvention _defaultObjectMappingConvention = new();

    public IObjectMappingConvention? GetConvention(Type type)
    {
        if (!typeof(IRecord).IsAssignableFrom(type))
        {
            return null;
        }

        return new RecordIdObjectMappingConvention(_defaultObjectMappingConvention);
    }
}
