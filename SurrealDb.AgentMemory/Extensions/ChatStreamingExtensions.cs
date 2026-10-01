using System.Diagnostics.CodeAnalysis;
using System.Net.ServerSentEvents;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using SurrealDb.AgentMemory.Api;
using SurrealDb.AgentMemory.Model;

namespace SurrealDb.AgentMemory;

/// <summary>
/// Server-sent-event streaming for the Agent Memory chat endpoint.
/// </summary>
public static class ChatStreamingExtensions
{
    private static readonly JsonSerializerOptions JsonOptions =
        AgentMemoryJsonExtensions.DefaultOptions;

    /// <summary>
    /// Streams a chat reply as an <see cref="IAsyncEnumerable{T}"/> of <see cref="ChatChunk"/> frames.
    /// Mirrors the non-streaming <c>ChatAsync</c> operation, but returns tokens as they are produced;
    /// the terminal frame carries the full reply, memory updates and citations.
    /// </summary>
    /// <param name="api">The Agent Memory client.</param>
    /// <param name="contextId">Agent Memory context id.</param>
    /// <param name="request">The chat request; <c>Stream</c> is forced to <see langword="true"/>.</param>
    /// <param name="cancellationToken">Cancels the stream.</param>
    public static async IAsyncEnumerable<ChatChunk> StreamChatAsync(
        this AgentMemoryApi api,
        string contextId,
        ChatRequestJson request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        if (api is null)
        {
            throw new ArgumentNullException(nameof(api));
        }

        request.Stream = true;

        using var httpRequest = new HttpRequestMessage(
            HttpMethod.Post,
            $"api/v1/{Uri.EscapeDataString(contextId)}/chat"
        );
        httpRequest.Headers.Accept.ParseAdd("text/event-stream");
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(request, JsonOptions.GetTypeInfo(typeof(ChatRequestJson))),
            Encoding.UTF8,
            "application/json"
        );

        var bearer = await api
            .BearerTokenProvider.GetAsync(cancellation: cancellationToken)
            .ConfigureAwait(false);
        bearer.UseInHeader(httpRequest, "Authorization");

        using var response = await api
            .HttpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken
            )
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        await using var stream = await response
            .Content.ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);

        await foreach (
            var item in SseParser
                .Create(stream)
                .EnumerateAsync(cancellationToken)
                .ConfigureAwait(false)
        )
        {
            var eventName = item.EventType == SseParser.EventTypeDefault ? null : item.EventType;
            var chunk = BuildFrame(item.Data, eventName);
            yield return chunk;
            if (chunk.Done)
            {
                yield break;
            }
        }
    }

    private static ChatChunk BuildFrame(string data, string? eventName)
    {
        if (data == "[DONE]")
        {
            return new ChatChunk { Event = eventName, Done = true };
        }

        JsonElement payload;
        try
        {
            using var document = JsonDocument.Parse(data);
            payload = document.RootElement.Clone();
        }
        catch (JsonException)
        {
            // Not JSON: treat the whole payload as a delta, mirroring the JS client.
            return new ChatChunk { Event = eventName, Delta = data };
        }

        if (payload.ValueKind == JsonValueKind.Object && eventName == "error")
        {
            throw new ChatStreamException(
                TryString(payload, "error", out var frameError)
                    ? frameError
                    : "The server ended the chat stream with an error."
            );
        }

        if (
            payload.ValueKind == JsonValueKind.Object
            && payload.TryGetProperty("error", out var errorElement)
            && errorElement.ValueKind == JsonValueKind.String
        )
        {
            throw new ChatStreamException(
                errorElement.GetString() ?? "The server ended the chat stream with an error."
            );
        }

        bool done =
            eventName == "done"
            || (
                payload.ValueKind == JsonValueKind.Object
                && payload.TryGetProperty("done", out var doneElement)
                && doneElement.ValueKind == JsonValueKind.True
            );

        return new ChatChunk
        {
            Event = eventName,
            Delta = TryString(payload, "delta", out var delta) ? delta : string.Empty,
            TraceId = TryString(payload, "traceId", out var traceId) ? traceId : null,
            SessionId = TryString(payload, "sessionId", out var sessionId) ? sessionId : null,
            Reply = TryString(payload, "reply", out var reply) ? reply : null,
            MemoryUpdates = TryElement(payload, "memoryUpdates", out var memoryUpdates)
                ? Deserialize<ExtractionResultJson>(memoryUpdates)
                : null,
            Citations = TryElement(payload, "citations", out var citations)
                ? Deserialize<List<CitationJson>>(citations)
                : null,
            Done = done,
            Raw = payload,
        };
    }

    private static T? Deserialize<T>(JsonElement element)
        where T : class
    {
        try
        {
            return (T?)
                JsonSerializer.Deserialize(
                    element.GetRawText(),
                    JsonOptions.GetTypeInfo(typeof(T))
                );
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool TryString(
        JsonElement element,
        string name,
        [NotNullWhen(true)] out string? value
    )
    {
        value = null;

        return element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(name, out var property)
            && property.ValueKind == JsonValueKind.String
            && (value = property.GetString()) is not null;
    }

    private static bool TryElement(JsonElement element, string name, out JsonElement value)
    {
        value = default;
        return element.ValueKind == JsonValueKind.Object && element.TryGetProperty(name, out value);
    }
}
