using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using SurrealDb.AgentMemory.Api;
using SurrealDb.AgentMemory.Model;

namespace SurrealDb.AgentMemory;

/// <summary>
/// Raised when the server ends a streaming chat with an <c>error</c> frame after the
/// response headers were already sent.
/// </summary>
public sealed class ChatStreamException : Exception
{
    public ChatStreamException(string message)
        : base(message) { }
}

/// <summary>
/// Server-sent-event streaming for the Agent Memory chat endpoint.
/// </summary>
public static class ChatStreamingExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = AgentMemoryJson.DefaultOptions;

    /// <summary>
    /// Streams a chat reply as an <see cref="IAsyncEnumerable{T}"/> of <see cref="ChatChunk"/>
    /// frames. Mirrors the non-streaming <c>ChatAsync</c> operation, but returns tokens as they
    /// are produced; the terminal frame carries the full reply, memory updates and citations.
    /// </summary>
    /// <param name="api">The Agent Memory client.</param>
    /// <param name="contextId">Agent Memory context id.</param>
    /// <param name="request">The chat request; <c>Stream</c> is forced to <see langword="true"/>.</param>
    /// <param name="cancellationToken">Cancels the stream.</param>
    public static async IAsyncEnumerable<ChatChunk> StreamChatAsync(
        this DefaultApi api,
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
            JsonSerializer.Serialize(request, JsonOptions),
            Encoding.UTF8,
            "application/json"
        );

        var bearer = await api
            .BearerTokenProvider.GetAsync(cancellation: cancellationToken)
            .ConfigureAwait(false);
        bearer.UseInHeader(httpRequest, "Authorization");

        var response = await api
            .HttpClient.SendAsync(
                httpRequest,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken
            )
            .ConfigureAwait(false);
        using (response)
        {
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
            using var reader = new StreamReader(stream, Encoding.UTF8, false, 1024);
            // StreamReader.ReadLineAsync does not accept a token; wire cancellation to the stream.
            var registration = cancellationToken.Register(() =>
            {
                try
                {
                    stream.Dispose();
                }
                catch
                {
                    // Disposal races with the read loop during cancellation; the loop
                    // surfaces the cancellation itself.
                }
            });

            try
            {
                string? eventName = null;
                var dataLines = new List<string>();

                while (true)
                {
                    string? line;
                    try
                    {
                        line = await reader.ReadLineAsync().ConfigureAwait(false);
                    }
                    catch (Exception ex) when (cancellationToken.IsCancellationRequested)
                    {
                        throw new OperationCanceledException(
                            "The chat stream was cancelled.",
                            ex,
                            cancellationToken
                        );
                    }

                    cancellationToken.ThrowIfCancellationRequested();

                    if (line is null)
                    {
                        yield break; // EOF: drop any incomplete trailing frame
                    }

                    if (line.Length == 0)
                    {
                        // Blank line: dispatch the accumulated frame, then reset.
                        if (dataLines.Count > 0)
                        {
                            var chunk = BuildFrame(dataLines, eventName, out var terminal);
                            if (chunk is not null)
                            {
                                yield return chunk;
                            }
                            if (terminal)
                            {
                                yield break;
                            }

                            eventName = null;
                            dataLines.Clear();
                        }
                        continue;
                    }

                    if (line[0] == ':')
                    {
                        continue; // comment / keep-alive
                    }

                    if (line.StartsWith("event:", StringComparison.Ordinal))
                    {
                        eventName = line["event:".Length..].Trim();
                        continue;
                    }

                    if (line.StartsWith("data:", StringComparison.Ordinal))
                    {
                        dataLines.Add(line["data:".Length..].TrimStart());
                    }
                }
            }
            finally
            {
                registration.Dispose();
            }
        }
    }

    private static ChatChunk? BuildFrame(
        List<string> dataLines,
        string? eventName,
        out bool terminal
    )
    {
        terminal = false;
        var data = string.Join("\n", dataLines);

        if (data == "[DONE]")
        {
            terminal = true;
            return new ChatChunk { Event = eventName, Done = true };
        }

        JsonElement payload;
        var document = default(JsonDocument);
        try
        {
            document = JsonDocument.Parse(data);
            payload = document.RootElement.Clone();
        }
        catch (JsonException)
        {
            // Not JSON: treat the whole payload as a delta, mirroring the JS client.
            return new ChatChunk { Event = eventName, Delta = data };
        }
        finally
        {
            document?.Dispose();
        }

        if (payload.ValueKind == JsonValueKind.Object && eventName == "error")
        {
            throw new ChatStreamException(
                TryString(payload, "error", out var frameError) && frameError is not null
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

        var done =
            eventName == "done"
            || (
                payload.ValueKind == JsonValueKind.Object
                && payload.TryGetProperty("done", out var doneElement)
                && doneElement.ValueKind == JsonValueKind.True
            );

        if (done)
        {
            terminal = true;
        }

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
            return JsonSerializer.Deserialize<T>(element.GetRawText(), JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool TryString(
        JsonElement element,
        string name,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? value
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
