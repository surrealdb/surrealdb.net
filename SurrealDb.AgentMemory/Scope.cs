namespace SurrealDb.AgentMemory;

/// <summary>
/// A scope selector, accepted by write and session calls (<c>Scopes</c>) and by read lenses (<c>Lens</c>).
/// </summary>
/// <remarks>
/// <para>
/// The wire format is a <c>ScopeSets</c>: a DNF (disjunctive-normal-form) selector shaped as <c>string[][]</c>.
/// The outer array is an OR of clauses; each inner array is an AND of hierarchical <c>key/value</c> paths,
/// so <c>[["team/a"], ["team/b", "clearance/secret"]]</c> means <c>team/a OR (team/b AND clearance/secret)</c>.
/// </para>
/// <para>
/// For convenience a bare string converts to a single-path clause,
/// and a flat string array converts to an OR of single-path clauses:
/// <c>"team/eng"</c> is <c>[["team/eng"]]</c>, and <c>["a", "b"]</c> is <c>[["a"], ["b"]]</c> (a OR b).
/// Nest the arrays to express an AND: <c>new[] { new[] { "a", "b" } }</c> is <c>a AND b</c>.
/// </para>
/// </remarks>
public sealed class Scope
{
    private readonly string[][] _clauses;

    /// <summary>
    /// Creates a scope from DNF clauses: an OR of clauses, each an AND of paths.
    /// </summary>
    /// <param name="clauses">The clauses.</param>
    public Scope(IEnumerable<IEnumerable<string>> clauses)
    {
        ArgumentNullException.ThrowIfNull(clauses);
        _clauses = clauses.Select(clause => (clause ?? []).ToArray()).ToArray();
    }

    /// <summary>
    /// The clauses as given, before normalization.
    /// </summary>
    public IReadOnlyList<IReadOnlyList<string>> Clauses => _clauses;

    /// <summary>
    /// A single clause with one path.
    /// </summary>
    /// <param name="path">A <c>key/value</c> path, optionally ending in <c>/*</c> to select a subtree.</param>
    public static Scope Path(string path)
    {
        return new Scope(
            [
                [path],
            ]
        );
    }

    /// <summary>
    /// An OR of single-path clauses: matches any of <paramref name="paths"/>.
    /// </summary>
    /// <param name="paths">The paths.</param>
    public static Scope AnyOf(params string[] paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        return new Scope(paths.Select(path => new[] { path }));
    }

    /// <summary>
    /// A single AND clause: matches only where all of <paramref name="paths"/> apply.
    /// </summary>
    /// <param name="paths">The paths.</param>
    public static Scope AllOf(params string[] paths)
    {
        ArgumentNullException.ThrowIfNull(paths);
        return new Scope([paths]);
    }

    /// <summary>
    /// Converts a bare path to a single-path clause.
    /// </summary>
    /// <param name="path">The path.</param>
    public static implicit operator Scope?(string? path)
    {
        return path is null ? null : Path(path);
    }

    /// <summary>
    /// Converts a flat array to an OR of single-path clauses.
    /// </summary>
    /// <param name="paths">The paths.</param>
    public static implicit operator Scope?(string[]? paths)
    {
        return paths is null ? null : AnyOf(paths);
    }

    /// <summary>
    /// Converts nested arrays to DNF clauses as given.
    /// </summary>
    /// <param name="clauses">The clauses.</param>
    public static implicit operator Scope?(string[][]? clauses)
    {
        return clauses is null ? null : new Scope(clauses);
    }

    /// <summary>
    /// Normalizes a scope to the wire <c>ScopeSets</c>.
    /// </summary>
    /// <remarks>
    /// Within each clause, empty paths are dropped and duplicates are removed, keeping the first occurrence.
    /// A clause that ends up empty is dropped, because the server rejects empty clauses.
    /// </remarks>
    /// <param name="scope">The scope.</param>
    /// <returns>
    /// The normalized selector, or <see langword="null"/> when no clause remains,
    /// so the field can be omitted and the key's default region applies.
    /// </returns>
    public static IReadOnlyList<IReadOnlyList<string>>? Normalize(Scope? scope)
    {
        if (scope is null)
        {
            return null;
        }

        var result = new List<IReadOnlyList<string>>();
        foreach (var clause in scope._clauses)
        {
            var paths = new List<string>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var path in clause)
            {
                if (!string.IsNullOrEmpty(path) && seen.Add(path))
                {
                    paths.Add(path);
                }
            }

            if (paths.Count > 0)
            {
                result.Add(paths);
            }
        }

        return result.Count > 0 ? result : null;
    }

    /// <inheritdoc cref="Normalize(Scope?)"/>
    public IReadOnlyList<IReadOnlyList<string>>? Normalize()
    {
        return Normalize(this);
    }
}
