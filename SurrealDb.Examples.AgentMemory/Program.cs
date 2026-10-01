using Microsoft.Extensions.DependencyInjection;
using SurrealDb.AgentMemory.Api;
using SurrealDb.AgentMemory.Client;
using SurrealDb.AgentMemory.Model;

var endpoint = Setting("SURREAL_AGENT_ENDPOINT"); //"https://...surreal.cloud"
var contextId = Setting("SURREAL_AGENT_CONTEXT_ID");
var apiKey = Setting("SURREAL_AGENT_API_KEY");

if (
    !Uri.TryCreate(endpoint, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https")
)
{
    Console.Error.WriteLine("SURREAL_AGENT_ENDPOINT muss eine absolute http(s)-URL sein.");
    return 1;
}

var services = new ServiceCollection();
services.AddAgentMemory(options =>
{
    options.Endpoint = endpoint;
    options.ApiKey = apiKey;
    options.RequestTimeout = TimeSpan.FromMinutes(2);
});
using var provider = services.BuildServiceProvider();
var api = provider.GetRequiredService<AgentMemoryApi>();

try
{
    var identity = await api.WhoamiAsync(contextId);
    if (!identity.IsSuccessStatusCode || identity.Ok() is not { } me)
    {
        PrintFailure(identity);
        return 1;
    }

    Console.WriteLine(
        $"Connected to {uri.Host}, Context '{contextId}', Principal '{me.DisplayName}'."
    );
    Console.WriteLine(
        "Commands: /remember <Fact>, /recall <Question>, /chat <Message>, /session, /help, /quit"
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
                    var request = new FactsRequest(text: argument);
                    if (sessionId is not null)
                    {
                        request.SessionId = sessionId;
                    }
                    var response = await api.CreateFactAsync(contextId, request);
                    if (!response.IsSuccessStatusCode || response.Ok() is not { } result)
                    {
                        PrintFailure(response);
                        break;
                    }
                    sessionId = result.SessionId;
                    Console.WriteLine($"Stored (Mode: {result.Mode}, Session: {sessionId}).");
                    break;
                }
                case "/recall" when argument.Length > 0:
                {
                    var response = await api.QueryMemoryAsync(
                        contextId,
                        new QueryMemoryRequestJson(argument, k: 5)
                    );
                    if (!response.IsSuccessStatusCode || response.Ok() is not { } result)
                    {
                        PrintFailure(response);
                        break;
                    }
                    Console.WriteLine($"{result.Hits.Count} Hits in {result.QueryMs} ms:");
                    foreach (var hit in result.Hits)
                        Console.WriteLine($"  {hit.Score:0.00} [{hit.Source}] {hit.Text}");
                    break;
                }
                case "/chat" when argument.Length > 0:
                {
                    var request = new ChatRequestJson(argument);
                    if (sessionId is not null)
                    {
                        request.SessionId = sessionId;
                    }
                    var response = await api.ChatAsync(contextId, request);
                    if (!response.IsSuccessStatusCode || response.Ok() is not { } result)
                    {
                        PrintFailure(response);
                        break;
                    }
                    sessionId = result.SessionId;
                    Console.WriteLine(result.Reply);
                    Console.WriteLine($"(Session: {sessionId}, Sources: {result.Citations.Count})");
                    break;
                }
                case "/session":
                    Console.WriteLine(
                        sessionId is null ? "No current session." : $"Current session: {sessionId}"
                    );
                    break;
                case "/help":
                    Console.WriteLine(
                        "/remember <Fact> stores a fact; /recall <Question> shows hits; /chat <Message> continues the session; /session shows its ID; /quit exits."
                    );
                    break;
                default:
                    Console.WriteLine("Unknown or incomplete command. /help shows the commands.");
                    break;
            }
        }
        catch (Exception ex)
            when (ex is HttpRequestException or TaskCanceledException or ApiException)
        {
            Console.Error.WriteLine($"Request failed: {ex.Message}");
        }
    }
}
catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or ApiException)
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

static void PrintFailure(IApiResponse response) =>
    Console.Error.WriteLine(
        $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {response.RawContent}"
    );
