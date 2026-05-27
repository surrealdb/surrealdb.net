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

    public static bool ShouldUseTestContainers()
    {
        var ci = Environment.GetEnvironmentVariable("CI");
        var disableTestContainers = Environment.GetEnvironmentVariable("DISABLE_TESTCONTAINERS");

        bool isCi =
            string.Equals(ci, "true", StringComparison.OrdinalIgnoreCase)
            || (ci is not null && ci != "false");
        bool isDisabled =
            disableTestContainers == "1"
            || string.Equals(disableTestContainers, "true", StringComparison.OrdinalIgnoreCase);

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
