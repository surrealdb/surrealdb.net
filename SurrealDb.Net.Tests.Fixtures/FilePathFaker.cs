using Bogus;

namespace SurrealDb.Net.Tests.Fixtures;

internal sealed class FilePathFaker : Faker<FilePathInfo>
{
    public FilePathFaker()
    {
        RuleFor(o => o.Path, f => $"temp/{f.Random.AlphaNumeric(40)}");
    }
}
