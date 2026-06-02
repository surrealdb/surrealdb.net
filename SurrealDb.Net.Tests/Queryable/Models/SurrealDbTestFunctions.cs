using SurrealDb.Net.Attributes;

namespace SurrealDb.Net.Tests.Queryable.Models;

/// <summary>
/// A helper class with static methods decorated with <see cref="SurrealDbFunctionAttribute"/> for use in Queryable expression tests.
/// <see cref="NotSupportedException"/> is thrown for simplicity.
/// </summary>
public static class SurrealDbTestFunctions
{
    [SurrealDbFunction("greet")]
    public static string Greet() => throw new NotSupportedException();

    [SurrealDbFunction("process")]
    public static string Process(string value) => throw new NotSupportedException();

    [SurrealDbFunction("combine")]
    public static string Combine(string first, string second, string third) =>
        throw new NotSupportedException();

    [SurrealDbFunction("followers")]
    public static List<User> Followers(this User user) => throw new NotSupportedException();

    [SurrealDbFunction("string::uppercase", IsBuiltIn = true)]
    public static string StringUppercase(string value) => throw new NotSupportedException();

    [SurrealDbFunction("math::abs", IsBuiltIn = true)]
    public static double MathAbs(double value) => throw new NotSupportedException();
}
