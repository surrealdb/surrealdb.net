using System.Text.Json;
using SurrealDb.AgentMemory.Model;

namespace SurrealDb.AgentMemory;

/// <summary>
/// One incremental frame from a streaming chat call (<see cref="AgentMemoryClient.StreamChatAsync"/>).
/// </summary>
public sealed class ChatChunk
{
    /// <summary>
    /// The SSE event name, when the server labels the frame.
    /// </summary>
    /// <remarks>
    /// The chat stream labels every frame (<c>meta</c>, <c>chunk</c>, <c>done</c>),
    /// so this is how a metadata frame is told from a token frame without inspecting the payload.
    /// </remarks>
    public string? Event { get; init; }

    /// <summary>
    /// Token delta for this frame (empty on metadata and terminal frames).
    /// </summary>
    public string Delta { get; init; } = string.Empty;

    /// <summary>
    /// Trace id, present once the server has assigned one.
    /// </summary>
    public string? TraceId { get; init; }

    /// <summary>
    /// Session id the conversation is attached to.
    /// </summary>
    public string? SessionId { get; init; }

    /// <summary>
    /// The complete reply, on the terminal frame.
    /// </summary>
    /// <remarks>
    /// Already sanitized by the server, and identical to the concatenated deltas.
    /// Prefer it over an accumulator when only the final text matters.
    /// </remarks>
    public string? Reply { get; init; }

    /// <summary>
    /// What the turn wrote to memory, on the terminal frame.
    /// </summary>
    /// <remarks>
    /// The same payload the non-streaming response carries.
    /// <see langword="null"/> when the frame has none, or when it does not match the model; <see cref="Raw"/> always has it.
    /// </remarks>
    public ExtractionResultJson? MemoryUpdates { get; init; }

    /// <summary>
    /// Sources the reply cited, on the terminal frame, one per marker.
    /// </summary>
    public IReadOnlyList<CitationJson>? Citations { get; init; }

    /// <summary>
    /// <see langword="true"/> on the terminal frame.
    /// </summary>
    public bool Done { get; init; }

    /// <summary>
    /// The raw decoded frame payload (an empty object for the <c>[DONE]</c> sentinel).
    /// </summary>
    public JsonElement Raw { get; init; }
}
