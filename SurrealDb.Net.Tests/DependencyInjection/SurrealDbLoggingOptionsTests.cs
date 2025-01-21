using SurrealDb.Net.Extensions.DependencyInjection;

namespace SurrealDb.Net.Tests.DependencyInjection;

public class SurrealDbLoggingOptionsTests
{
    [Test]
    public void SensitiveDataLoggingShouldBeDisabledByDefault()
    {
        new SurrealDbLoggingOptions().SensitiveDataLoggingEnabled.Should().BeFalse();
    }
}
