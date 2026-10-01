using SurrealDb.AgentMemory.Client;

namespace SurrealDb.AgentMemory;

/// <summary>
/// Bridges nullable reference values to the generated <see cref="Option{TType}"/> query-parameter wrapper:
/// a <see langword="null"/> cursor must map to an <i>unset</i> option (parameter omitted),
/// which the implicit <c>string? → Option&lt;string&gt;</c> conversion would not guarantee.
/// </summary>
public static class OptionExtensions
{
    /// <summary>
    /// Converts a nullable value into an <see cref="Option{TType}"/> that is unset when the
    /// value is <see langword="null"/>, e.g. <c>cursor: cursor.ToOption()</c>.
    /// </summary>
    public static Option<T> ToOption<T>(this T? value)
        where T : class => value is null ? default : new Option<T>(value);
}
