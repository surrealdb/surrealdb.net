using Microsoft.Extensions.DependencyInjection;
using SurrealDb.AgentMemory;

var endpoint = Setting("SURREAL_AGENT_ENDPOINT"); // for example "https://….surreal.cloud"
var contextId = Setting("SURREAL_AGENT_CONTEXT_ID");
var apiKey = Setting("SURREAL_AGENT_API_KEY");

if (
    !Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")
)
{
    Console.Error.WriteLine("SURREAL_AGENT_ENDPOINT must be an absolute http(s) URL.");
    return 1;
}

var services = new ServiceCollection();
services.AddAgentMemory(options =>
{
    options.Endpoint = endpoint;
    options.Context = contextId;
    options.ApiKey = apiKey;
    options.Timeout = TimeSpan.FromMinutes(2);
});
await using var provider = services.BuildServiceProvider();
var memory = provider.GetRequiredService<AgentMemoryClient>();

try
{
    var me = await memory.WhoamiAsync();
    Console.WriteLine(
        $"Connected to {uri.Host}, context '{memory.ContextId}', principal '{me.DisplayName}'."
    );
    Console.WriteLine(
        "Commands: /remember <fact>, /recall <question>, /chat <message>, /session, /help, /quit"
    );

    string? sessionId = null;
    while (true)
    {
        Console.Write("\nagent-memory> ");
        var line = Console.ReadLine()?.Trim();
        if (line is null || line.Equals("/quit", StringComparison.OrdinalIgnoreCase))
        {
            break;
        }

        if (line.Length == 0)
        {
            continue;
        }

        var separator = line.IndexOf(' ');
        var command = separator < 0 ? line : line[..separator];
        var argument = separator < 0 ? "" : line[(separator + 1)..].Trim();

        try
        {
            switch (command.ToLowerInvariant())
            {
                case "/remember" when argument.Length > 0:
                {
                    var result = await memory.RememberAsync(
                        argument,
                        new RememberOptions { SessionId = sessionId }
                    );
                    sessionId = result.SessionId;
                    Console.WriteLine($"Stored (mode: {result.Mode}, session: {sessionId}).");
                    break;
                }
                case "/recall" when argument.Length > 0:
                {
                    var result = await memory.RecallAsync(argument, new RecallOptions { K = 5 });
                    Console.WriteLine($"{result.Hits.Count} hits in {result.QueryMs} ms:");
                    foreach (var hit in result.Hits)
                    {
                        Console.WriteLine($"  {hit.Score:0.00} [{hit.Source}] {hit.Text}");
                    }
                    break;
                }
                case "/chat" when argument.Length > 0:
                {
                    await foreach (
                        var chunk in memory.StreamChatAsync(
                            argument,
                            new ChatOptions { SessionId = sessionId }
                        )
                    )
                    {
                        Console.Write(chunk.Delta);
                        sessionId = chunk.SessionId ?? sessionId;
                        if (chunk.Done)
                        {
                            Console.WriteLine();
                            Console.WriteLine(
                                $"(session: {sessionId}, sources: {chunk.Citations?.Count ?? 0})"
                            );
                        }
                    }
                    break;
                }
                case "/session":
                    Console.WriteLine(
                        sessionId is null ? "No current session." : $"Current session: {sessionId}"
                    );
                    break;
                case "/help":
                    Console.WriteLine(
                        "/remember <fact> stores a fact; /recall <question> shows hits; /chat <message> continues the session; /session shows its id; /quit exits."
                    );
                    break;
                default:
                    Console.WriteLine("Unknown or incomplete command. /help shows the commands.");
                    break;
            }
        }
        catch (AgentMemoryException ex)
        {
            Console.Error.WriteLine($"Request failed: {ex.Message}");
        }
    }
}
catch (AgentMemoryException ex)
{
    Console.Error.WriteLine($"Request failed: {ex.Message}");
    return 1;
}

return 0;

static string Setting(string name) =>
    Environment.GetEnvironmentVariable(name)
    ?? (
        OperatingSystem.IsWindows()
            ? Environment.GetEnvironmentVariable(name, EnvironmentVariableTarget.User)
            : null
    )
    ?? throw new InvalidOperationException($"Environment variable not found: {name}");
