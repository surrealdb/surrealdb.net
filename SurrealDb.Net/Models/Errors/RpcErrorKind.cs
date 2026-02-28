namespace SurrealDb.Net.Models.Errors;

public enum RpcErrorKind
{
    Validation = 1,
    NotFound = 2,
    NotAllowed = 3,
    Configuration = 4,
    Internal = 5,
    Connection = 6,
    Query = 7,
    Thrown = 8,
    Serialization = 9,
    AlreadyExists = 10,
}
