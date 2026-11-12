using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using SurrealDb.AgentMemory.Client;
using SurrealDb.AgentMemory.Model;

namespace SurrealDb.AgentMemory;

/// <summary>
/// Server-sent-event parsing for the streaming chat endpoint.
/// </summary>
public static class ChatStreamParser
{
    private static readonly JsonElement EmptyObject = JsonDocument.Parse("{}").RootElement.Clone();

    /// <summary>
    /// Parses an SSE body into <see cref="ChatChunk"/> frames.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Handles multi-line <c>data:</c> payloads, comment lines, event labels, and the terminal
    /// <c>[DONE]</c> sentinel. Frames are separated by a blank line; an incomplete frame at the end of the
    /// stream is not dispatched.
    /// </para>
    /// <para>
    /// A failure part-way through a stream surfaces where a request failure would, never as a stream that
    /// simply ended: a frame the server labels <c>error</c>, or one that carries a string <c>error</c>, and a
    /// stream that closes before its terminal frame, raise <see cref="AgentMemoryStreamException"/>; a
    /// connection that drops raises <see cref="AgentMemoryConnectionException"/>.
    /// </para>
    /// </remarks>
    /// <param name="stream">The response body.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    public static async IAsyncEnumerable<ChatChunk> ParseAsync(
        Stream stream,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(stream);

        using var reader = new StreamReader(stream, Encoding.UTF8);
        var dataLines = new List<string>();
        string? eventName = null;

        while (true)
        {
            string? line;
            try
            {
                line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is IOException or HttpRequestException)
            {
                cancellationToken.ThrowIfCancellationRequested();
                throw new AgentMemoryConnectionException(
                    "Chat stream interrupted",
                    exception.Message,
                    exception
                );
            }

            if (line is null)
            {
                // The stream closed before its terminal frame, so the reply is incomplete.
                // A frame without its closing blank line is not dispatched.
                throw new AgentMemoryStreamException(
                    "Chat stream ended early",
                    "The stream closed before its terminal frame."
                );
            }

            if (line.Length > 0)
            {
                if (line.StartsWith(':'))
                {
                    // Comment or keep-alive.
                    continue;
                }

                if (line.StartsWith("event:", StringComparison.Ordinal))
                {
                    eventName = line[6..].Trim();
                    continue;
                }

                if (line.StartsWith("data:", StringComparison.Ordinal))
                {
                    dataLines.Add(line[5..].TrimStart());
                }
                continue;
            }

            // A blank line ends the frame.
            if (dataLines.Count == 0)
            {
                eventName = null;
                continue;
            }

            var chunk = ParseFrame(eventName, string.Join('\n', dataLines));
            dataLines.Clear();
            eventName = null;

            yield return chunk;
            if (chunk.Done)
            {
                yield break;
            }
        }
    }

    /// <summary>
    /// Turns one frame into a <see cref="ChatChunk"/>.
    /// </summary>
    /// <param name="eventName">The frame's <c>event:</c> label, if any.</param>
    /// <param name="data">The frame's joined <c>data:</c> lines.</param>
    /// <exception cref="AgentMemoryStreamException">The frame reports a failure.</exception>
    public static ChatChunk ParseFrame(string? eventName, string data)
    {
        ArgumentNullException.ThrowIfNull(data);

        if (data == "[DONE]")
        {
            return new ChatChunk
            {
                Event = eventName,
                Delta = string.Empty,
                Done = true,
                Raw = EmptyObject,
            };
        }

        JsonElement payload;
        try
        {
            using var document = JsonDocument.Parse(data);
            payload = document.RootElement.Clone();
        }
        catch (JsonException)
        {
            // Not JSON: the whole payload is a token delta.
            payload = DeltaObject(data);
        }

        var error = ReadString(payload, "error");
        if (eventName == "error" || error is not null)
        {
            throw new AgentMemoryStreamException(
                "Chat stream failed",
                error ?? "The server ended the stream with an error."
            );
        }

        var done =
            eventName == "done"
            || (
                payload.ValueKind == JsonValueKind.Object
                && payload.TryGetProperty("done", out var doneElement)
                && doneElement.ValueKind == JsonValueKind.True
            );

        // `text` is what the chat stream labels its token frames with; `delta` and `token` are the
        // shapes other streaming endpoints use. All three are read so one parser serves every stream.
        var delta =
            ReadString(payload, "delta")
            ?? ReadString(payload, "token")
            ?? ReadString(payload, "text")
            ?? string.Empty;

        // Terminal-frame extras are read by shape rather than gated on the event name, so a server that
        // ends the stream with `done: true` and no label still hands the caller its reply.
        return new ChatChunk
        {
            Event = eventName,
            Delta = delta,
            TraceId = ReadString(payload, "traceId") ?? ReadString(payload, "trace_id"),
            SessionId = ReadString(payload, "sessionId") ?? ReadString(payload, "session_id"),
            Reply = ReadString(payload, "reply"),
            MemoryUpdates = ReadMemoryUpdates(payload),
            Citations = ReadCitations(payload),
            Done = done,
            Raw = payload,
        };
    }

    private static JsonElement DeltaObject(string data)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("delta", data);
            writer.WriteEndObject();
        }

        using var document = JsonDocument.Parse(buffer.WrittenMemory);
        return document.RootElement.Clone();
    }

    private static string? ReadString(JsonElement element, string name)
    {
        return
            element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(name, out var value)
            && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;
    }

    private static ExtractionResultJson? ReadMemoryUpdates(JsonElement payload)
    {
        if (
            payload.ValueKind != JsonValueKind.Object
            || !payload.TryGetProperty("memoryUpdates", out var element)
            || element.ValueKind != JsonValueKind.Object
        )
        {
            return null;
        }

        return TryDeserialize(element, AgentMemoryJsonContext.Default.ExtractionResultJson);
    }

    private static List<CitationJson>? ReadCitations(JsonElement payload)
    {
        if (
            payload.ValueKind != JsonValueKind.Object
            || !payload.TryGetProperty("citations", out var element)
            || element.ValueKind != JsonValueKind.Array
        )
        {
            return null;
        }

        return TryDeserialize(element, AgentMemoryJsonContext.Default.ListCitationJson);
    }

    private static T? TryDeserialize<T>(
        JsonElement element,
        System.Text.Json.Serialization.Metadata.JsonTypeInfo<T> typeInfo
    )
        where T : class
    {
        try
        {
            return element.Deserialize(typeInfo);
        }
        catch (Exception exception) when (AgentMemoryTransport.IsDecodeFailure(exception))
        {
            // The generated models validate required fields; a frame that does not match keeps its raw form.
            return null;
        }
    }
}
