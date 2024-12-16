using SurrealDb.Net.Internals.Extensions;

namespace SurrealDb.Net.Models;

/// <summary>
/// This represents the <c>future</c> type that we retrieve in certain responses from SurrealDB.
/// It is used to represent a computational function.
/// </summary>
public readonly struct Future
{
    private readonly string _inner;

    internal string Inner => _inner;

    public Future()
    {
        throw new NotImplementedException();
    }

    public Future(string inner)
    {
        _inner = inner;
    }

    public override string ToString()
    {
        const string prefix = "<future> { ";
        const string suffix = " }";

        return string.Create(
            prefix.Length + _inner.Length + suffix.Length,
            this,
            (buffer, self) =>
            {
                buffer.Write(prefix);
                buffer.Write(self._inner);
                buffer.Write(suffix);
            }
        );
    }
}
