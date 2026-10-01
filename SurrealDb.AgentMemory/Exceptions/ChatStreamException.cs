namespace SurrealDb.AgentMemory;

/// <summary>
/// Raised when the server ends a streaming chat with an <c>error</c> frame after the response headers were already sent.
/// </summary>
public sealed class ChatStreamException(string message) : Exception(message);
