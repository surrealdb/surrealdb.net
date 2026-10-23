using SurrealDb.Embedded.InMemory;

namespace SurrealDb.Net.Tests;

public class EmbeddedCancellationTests
{
    [Test]
    public async Task ShouldUnregisterCallerCancellationAfterCompletedQuery()
    {
        await using var client = new SurrealDbMemoryClient();
        await client.Use("cancellation", "query");
        using var caller = new CancellationTokenSource();

        var result = await client.RawQuery("RETURN 1;", cancellationToken: caller.Token);
        result.EnsureAllOks();

        Action cancel = caller.Cancel;
        cancel.Should().NotThrow();
    }

    [Test]
    public async Task ShouldUnregisterCallerCancellationAfterCompletedExport()
    {
        await using var client = new SurrealDbMemoryClient();
        await client.Use("cancellation", "export");
        using var caller = new CancellationTokenSource();

        var result = await client.Export(cancellationToken: caller.Token);
        result.Should().NotBeNullOrWhiteSpace();

        Action cancel = caller.Cancel;
        cancel.Should().NotThrow();
    }

    [Test]
    public async Task ShouldUnregisterCallerCancellationAfterCompletedImport()
    {
        await using var client = new SurrealDbMemoryClient();
        await client.Use("cancellation", "import");
        await client.RawQuery("CREATE marker:1 SET value = 'retained';");
        var export = await client.Export();
        await client.Delete("marker");
        using var caller = new CancellationTokenSource();

        await client.Import(export, cancellationToken: caller.Token);
        var restored = await client.Select<object>("marker");
        restored.Should().HaveCount(1);

        Action cancel = caller.Cancel;
        cancel.Should().NotThrow();
    }

    [Test]
    [Arguments("query")]
    [Arguments("export")]
    public async Task ShouldStillObserveAlreadyCancelledCaller(string operation)
    {
        await using var client = new SurrealDbMemoryClient();
        await client.Use("cancellation", "already_cancelled");
        using var caller = new CancellationTokenSource();
        caller.Cancel();

        Func<Task> execute = async () =>
        {
            switch (operation)
            {
                case "query":
                    await client.RawQuery("RETURN 1;", cancellationToken: caller.Token);
                    break;
                case "export":
                    await client.Export(cancellationToken: caller.Token);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(operation));
            }
        };

        await execute.Should().ThrowAsync<OperationCanceledException>();
    }
}
