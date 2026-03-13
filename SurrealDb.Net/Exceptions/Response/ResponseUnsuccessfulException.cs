using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net.Exceptions.Response;

public sealed class ResponseUnsuccessfulException : SurrealDbException
{
    internal ResponseUnsuccessfulException()
        : base($"The {nameof(SurrealDbResponse)} is unsuccessful.") { }
}
