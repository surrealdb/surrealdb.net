namespace SurrealDb.Net.Internals.Cbor;

internal static class CborTagConstants
{
    public const ulong TAG_RECORDID = 8;
    public const ulong TAG_STRING_DECIMAL = 10;
    public const ulong TAG_CUSTOM_DATETIME = 12;
    public const ulong TAG_CUSTOM_DURATION = 14;
    public const ulong TAG_UUID = 37;
    public const ulong TAG_GEOMETRY_POINT = 88;
    public const ulong TAG_GEOMETRY_LINE = 89;
    public const ulong TAG_GEOMETRY_POLYGON = 90;
    public const ulong TAG_GEOMETRY_MULTIPOINT = 91;
    public const ulong TAG_GEOMETRY_MULTILINE = 92;
    public const ulong TAG_GEOMETRY_MULTIPOLYGON = 93;
    public const ulong TAG_GEOMETRY_COLLECTION = 94;
}
