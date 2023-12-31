using SurrealDb.Net.Models;

namespace SurrealDb.Net.Internals.Http;

internal class RelateHttpRequest<T>
{
    public IEnumerable<Thing> Ins { get; set; } = null!;
    public IEnumerable<Thing> Outs { get; set; } = null!;
    public T? Content { get; set; }
}
