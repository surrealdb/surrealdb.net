using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace SurrealDb.Net.Tests.Fixtures;

public sealed class SurrealDbContainer : IAsyncDisposable
{
    private const string Image = "surrealdb/surrealdb";
    private const string Tag = "latest";
    private const int Port = 8000;
    private const string Username = "root";
    private const string Password = "root";

    private IContainer? _container;

    private static bool IsEnvironmentVariableEnabled(string environmentVariableName)
    {
        var value = Environment.GetEnvironmentVariable(environmentVariableName);

        return string.Equals(value, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value, "1", StringComparison.Ordinal);
    }

    public static bool ShouldUseTestContainers()
    {
        bool isCi = IsEnvironmentVariableEnabled("CI");
        bool isDisabled = IsEnvironmentVariableEnabled("DISABLE_TESTCONTAINERS");

        return !isCi && !isDisabled;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        if (!ShouldUseTestContainers())
        {
            return;
        }

        _container = new ContainerBuilder($"{Image}:{Tag}")
            .WithPortBinding(Port, Port)
            .WithCommand("start", "--allow-all", "--user", Username, "--pass", Password, "memory")
            .WithWaitStrategy(
                Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(r => r.ForPort(Port).ForPath("/health"))
            )
            .Build();

        await _container.StartAsync(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
        }
    }
}
