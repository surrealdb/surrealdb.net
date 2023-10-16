using Semver;

namespace SurrealDb.Net.Extensions;

public static class SemverExtensions
{
	/// <summary>
	/// Converts a SurrealDB version string to a Semver version.
	/// </summary>
	/// <param name="version">The SurrealDB version string extracted from <see cref="ISurrealDbClient.Version(CancellationToken)"/>.</param>
	/// <returns>The full Semver version.</returns>
	/// <exception cref="ArgumentException"></exception>
	/// <exception cref="ArgumentNullException"></exception>
	/// <exception cref="FormatException"></exception>
	/// <exception cref="OverflowException"></exception>
	public static SemVersion ToSemver(this string version)
	{
		return SemVersion.Parse(version.Replace("surrealdb-", ""), SemVersionStyles.Strict);
	}
}
