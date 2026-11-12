using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using SurrealDb.AgentMemory.Tests.Infrastructure;

namespace SurrealDb.AgentMemory.Tests;

/// <summary>
/// Server-sent-event parsing for the streaming chat. The JavaScript suite has no streaming test;
/// these follow the Python client's <c>test_streaming.py</c> and the JavaScript parser's behaviour.
/// </summary>
public class StreamingTests
{
    private static async Task<List<ChatChunk>> ParseAsync(string body)
    {
        var chunks = new List<ChatChunk>();
        await foreach (
            var chunk in ChatStreamParser.ParseAsync(new MemoryStream(Encoding.UTF8.GetBytes(body)))
        )
        {
            chunks.Add(chunk);
        }
        return chunks;
    }

    [Test]
    public async Task ParsesDeltaChunksAndTheDoneSentinel()
    {
        var chunks = await ParseAsync(
            "data: {\"delta\": \"Hello\"}\n\ndata: {\"delta\": \" world\"}\n\ndata: [DONE]\n\n"
        );

        chunks.Select(chunk => chunk.Delta).Should().Equal("Hello", " world", "");
        chunks[^1].Done.Should().BeTrue();
        chunks[^1].Raw.ValueKind.Should().Be(JsonValueKind.Object);
        chunks[^1].Raw.EnumerateObject().Should().BeEmpty();
    }

    [Test]
    public async Task ParsesTheChatStreamsEventLabelsAndTerminalFrame()
    {
        var chunks = await ParseAsync(
            "event: meta\ndata: {\"traceId\":\"tr:1\",\"sessionId\":\"s:1\"}\n\n"
                + "event: chunk\ndata: {\"text\":\"Hi\"}\n\n"
                + $"event: done\ndata: {{\"reply\":\"Hi\",\"traceId\":\"tr:1\",\"sessionId\":\"s:1\",\"memoryUpdates\":{Fixtures.ExtractionResult},\"citations\":[{Fixtures.Citation}]}}\n\n"
        );

        chunks.Select(chunk => chunk.Event).Should().Equal("meta", "chunk", "done");
        chunks[0].TraceId.Should().Be("tr:1");
        chunks[0].SessionId.Should().Be("s:1");
        chunks[0].Done.Should().BeFalse();
        chunks[1].Delta.Should().Be("Hi");

        var done = chunks[2];
        done.Done.Should().BeTrue();
        done.Reply.Should().Be("Hi");
        done.MemoryUpdates!.TurnId.Should().Be("turn:1");
        done.Citations.Should().ContainSingle().Which.Marker.Should().Be("S1");
        done.Raw.GetProperty("reply").GetString().Should().Be("Hi");
    }

    [Test]
    public async Task ADoneFlagInTheDataIsTerminal()
    {
        var chunks = await ParseAsync(
            "data: {\"done\": true, \"traceId\": \"tr\"}\n\ndata: {\"delta\":\"ignored\"}\n\n"
        );

        chunks.Should().ContainSingle();
        chunks[0].Done.Should().BeTrue();
        chunks[0].TraceId.Should().Be("tr");
    }

    [Test]
    public async Task ANonJsonPayloadIsADelta()
    {
        var chunks = await ParseAsync("data: just text\n\n");

        chunks.Should().ContainSingle().Which.Delta.Should().Be("just text");
        chunks[0].Raw.GetProperty("delta").GetString().Should().Be("just text");
    }

    [Test]
    public async Task IgnoresCommentLinesAndFramesWithoutData()
    {
        var chunks = await ParseAsync(
            ": keep-alive\n\nevent: ping\n\n: comment\ndata: {\"delta\": \"hi\"}\n\n"
        );

        chunks.Should().ContainSingle();
        chunks[0].Delta.Should().Be("hi");
        chunks[0].Event.Should().BeNull();
    }

    [Test]
    public async Task JoinsMultiLineData()
    {
        var chunks = await ParseAsync("data: {\"delta\":\ndata:  \"split\"}\n\n");

        chunks.Should().ContainSingle().Which.Delta.Should().Be("split");
    }

    [Test]
    public async Task ReadsTheTokenAndSnakeCaseFallbacks()
    {
        var chunks = await ParseAsync(
            "data: {\"token\":\"a\",\"trace_id\":\"tr:2\",\"session_id\":\"s:2\"}\n\n"
        );

        chunks[0].Delta.Should().Be("a");
        chunks[0].TraceId.Should().Be("tr:2");
        chunks[0].SessionId.Should().Be("s:2");
    }

    [Test]
    public async Task HandlesCrLfLineEndings()
    {
        var chunks = await ParseAsync("event: chunk\r\ndata: {\"text\":\"a\"}\r\n\r\n");

        chunks.Should().ContainSingle().Which.Delta.Should().Be("a");
        chunks[0].Event.Should().Be("chunk");
    }

    [Test]
    public async Task DoesNotDispatchAnIncompleteFinalFrame()
    {
        var chunks = await ParseAsync("data: {\"delta\":\"a\"}\n\ndata: {\"delta\":\"b\"}\n");

        chunks.Select(chunk => chunk.Delta).Should().Equal("a");
    }

    [Test]
    public async Task RaisesAnErrorFrameAsAStreamException()
    {
        var act = () =>
            ParseAsync(
                "data: {\"delta\":\"a\"}\n\nevent: error\ndata: {\"error\":\"model unavailable\"}\n\n"
            );

        var error = await act.Should().ThrowAsync<AgentMemoryStreamException>();
        error.Which.Status.Should().Be(0);
        error.Which.Title.Should().Be("Chat stream failed");
        error.Which.Detail.Should().Be("model unavailable");
    }

    [Test]
    public async Task RaisesAnUnlabelledFrameCarryingAnError()
    {
        var act = () => ParseAsync("data: {\"error\":\"budget exceeded\"}\n\n");

        var error = await act.Should().ThrowAsync<AgentMemoryStreamException>();
        error.Which.Detail.Should().Be("budget exceeded");
    }

    [Test]
    public async Task RaisesAnErrorLabelWithoutADetail()
    {
        var act = () => ParseAsync("event: error\ndata: oops\n\n");

        var error = await act.Should().ThrowAsync<AgentMemoryStreamException>();
        error.Which.Detail.Should().Be("The server ended the stream with an error.");
    }

    [Test]
    public async Task KeepsATerminalFrameThatDoesNotMatchTheModelsInRaw()
    {
        var chunks = await ParseAsync(
            "event: done\ndata: {\"reply\":\"x\",\"memoryUpdates\":{\"unexpected\":1}}\n\n"
        );

        chunks[0].MemoryUpdates.Should().BeNull();
        chunks[0]
            .Raw.GetProperty("memoryUpdates")
            .GetProperty("unexpected")
            .GetInt32()
            .Should()
            .Be(1);
    }

    [Test]
    public async Task StreamChatSendsAStreamingRequestAndYieldsTheFrames()
    {
        var handler = new FakeHandler(_ =>
            Responses.Sse(
                "event: meta\ndata: {\"sessionId\":\"s1\"}\n\nevent: chunk\ndata: {\"text\":\"Hel\"}\n\nevent: chunk\ndata: {\"text\":\"lo\"}\n\nevent: done\ndata: {\"reply\":\"Hello\"}\n\n"
            )
        );
        var client = TestClient.Create(handler);

        var text = new StringBuilder();
        ChatChunk? last = null;
        await foreach (
            var chunk in client.StreamChatAsync(
                "Tell me a story",
                new ChatOptions { SessionId = "s1", SuppressMarkers = true }
            )
        )
        {
            text.Append(chunk.Delta);
            last = chunk;
        }

        text.ToString().Should().Be("Hello");
        last!.Done.Should().BeTrue();
        last.Reply.Should().Be("Hello");

        var request = handler.Last;
        request.Method.Should().Be("POST");
        request.Path.Should().Be("/api/v1/ctx-1/chat");
        request.Header("Accept").Should().Be("text/event-stream");
        request.Header("Idempotency-Key").Should().BeNull();
        JsonAssert.Equivalent(
            request.Json,
            """{"message":"Tell me a story","sessionId":"s1","suppressMarkers":true,"stream":true}"""
        );
    }

    [Test]
    public async Task StreamChatSurfacesAnHttpErrorAsATypedError()
    {
        var client = TestClient.Create(
            FakeHandler.Json("""{"title":"Forbidden"}""", HttpStatusCode.Forbidden)
        );

        var act = async () =>
        {
            await foreach (var _ in client.StreamChatAsync("hi")) { }
        };

        await act.Should().ThrowAsync<AgentMemoryScopeException>();
    }

    [Test]
    public async Task StreamChatSurfacesAMidStreamFailure()
    {
        var client = TestClient.Create(
            new FakeHandler(_ =>
                Responses.Sse(
                    "event: chunk\ndata: {\"text\":\"a\"}\n\nevent: error\ndata: {\"error\":\"boom\"}\n\n"
                )
            )
        );
        var deltas = new List<string>();

        var act = async () =>
        {
            await foreach (var chunk in client.StreamChatAsync("hi"))
            {
                deltas.Add(chunk.Delta);
            }
        };

        await act.Should().ThrowAsync<AgentMemoryStreamException>();
        deltas.Should().Equal("a");
    }
}
