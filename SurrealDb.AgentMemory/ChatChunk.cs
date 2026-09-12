using System.Text.Json;
using SurrealDb.AgentMemory.Model;

namespace SurrealDb.AgentMemory;

/// <summary>
/// One incremental frame from a streaming <c>chat</c> call
/// (<see cref="ChatStreamingExtensions.StreamChatAsync"/>).
/// </summary>
public sealed class ChatChunk
{
    /// <summary>
    /// The SSE event name, when the server labels the frame (<c>meta</c>, <c>chunk</c>, <c>done</c>).
    /// This is how a metadata frame is told from a token frame without inspecting the payload.
    /// </summary>
    public string? Event { get; set; }

    /// <summary>Token delta for this frame (empty on metadata and terminal frames).</summary>
    public string Delta { get; set; } = string.Empty;

    /// <summary>Trace id, present once the server has assigned one.</summary>
    public string? TraceId { get; set; }

    /// <summary>Session id the conversation is attached to.</summary>
    public string? SessionId { get; set; }

    /// <summary>
    /// The complete reply, on the terminal frame. Identical to the concatenated deltas;
    /// prefer it over an accumulator when only the final text matters.
    /// </summary>
    public string? Reply { get; set; }

    /// <summary>What the turn wrote to memory, on the terminal frame.</summary>
    public ExtractionResultJson? MemoryUpdates { get; set; }

    /// <summary>Sources the reply cited, on the terminal frame, one per marker.</summary>
    public System.Collections.Generic.List<CitationJson>? Citations { get; set; }

    /// <summary><see langword="true"/> on the terminal frame.</summary>
    public bool Done { get; set; }

    /// <summary>The raw decoded frame payload.</summary>
    public JsonElement Raw { get; set; }
}
