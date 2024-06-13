using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Http;

internal class RelateOneHttpRequest<T>
{
    public Thing In { get; set; } = null!;
    public Thing Out { get; set; } = null!;
    public T? Content { get; set; }
}
