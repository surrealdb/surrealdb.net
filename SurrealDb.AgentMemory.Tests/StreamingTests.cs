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
        var chunks = await ParseAsync("data: just text\n\ndata: [DONE]\n\n");

        chunks.Select(chunk => chunk.Delta).Should().Equal("just text", "");
        chunks[0].Raw.GetProperty("delta").GetString().Should().Be("just text");
    }

    [Test]
    public async Task IgnoresCommentLinesAndFramesWithoutData()
    {
        var chunks = await ParseAsync(
            ": keep-alive\n\nevent: ping\n\n: comment\ndata: {\"delta\": \"hi\"}\n\ndata: [DONE]\n\n"
        );

        chunks.Select(chunk => chunk.Delta).Should().Equal("hi", "");
        chunks[0].Event.Should().BeNull();
    }

    [Test]
    public async Task JoinsMultiLineData()
    {
        var chunks = await ParseAsync("data: {\"delta\":\ndata:  \"split\"}\n\ndata: [DONE]\n\n");

        chunks[0].Delta.Should().Be("split");
    }

    [Test]
    public async Task ReadsTheTokenAndSnakeCaseFallbacks()
    {
        var chunks = await ParseAsync(
            "data: {\"token\":\"a\",\"trace_id\":\"tr:2\",\"session_id\":\"s:2\"}\n\ndata: [DONE]\n\n"
        );

        chunks[0].Delta.Should().Be("a");
        chunks[0].TraceId.Should().Be("tr:2");
        chunks[0].SessionId.Should().Be("s:2");
    }

    [Test]
    public async Task HandlesCrLfLineEndings()
    {
        var chunks = await ParseAsync(
            "event: chunk\r\ndata: {\"text\":\"a\"}\r\n\r\ndata: [DONE]\r\n\r\n"
        );

        chunks.Select(chunk => chunk.Delta).Should().Equal("a", "");
        chunks[0].Event.Should().Be("chunk");
    }

    [Test]
    public async Task DoesNotDispatchAnIncompleteFinalFrameAndReportsTheStreamEndingEarly()
    {
        var chunks = new List<ChatChunk>();
        var act = async () =>
        {
            await foreach (
                var chunk in ChatStreamParser.ParseAsync(
                    new MemoryStream(
                        Encoding.UTF8.GetBytes(
                            "data: {\"delta\":\"a\"}\n\ndata: {\"delta\":\"b\"}\n"
                        )
                    )
                )
            )
            {
                chunks.Add(chunk);
            }
        };

        var error = await act.Should().ThrowAsync<AgentMemoryStreamException>();
        error.Which.Title.Should().Be("Chat stream ended early");
        chunks.Select(chunk => chunk.Delta).Should().Equal("a");
    }

    [Test]
    public async Task AStreamThatClosesBeforeItsTerminalFrameIsAnError()
    {
        // A truncated reply must not read as a complete one.
        var act = () => ParseAsync("event: chunk\ndata: {\"text\":\"Hel\"}\n\n");

        var error = await act.Should().ThrowAsync<AgentMemoryStreamException>();
        error.Which.Detail.Should().Be("The stream closed before its terminal frame.");
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

    [Test]
    public async Task StreamChatReportsADroppedConnectionAsAConnectionError()
    {
        var firstFrame = Encoding.UTF8.GetBytes("event: chunk\ndata: {\"text\":\"Hel\"}\n\n");
        var client = TestClient.Create(
            new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new DroppingStream(firstFrame)),
            })
        );
        var deltas = new List<string>();

        var act = async () =>
        {
            await foreach (var chunk in client.StreamChatAsync("hi"))
            {
                deltas.Add(chunk.Delta);
            }
        };

        var error = await act.Should().ThrowAsync<AgentMemoryConnectionException>();
        error.Which.Title.Should().Be("Chat stream interrupted");
        error.Which.InnerException.Should().BeAssignableTo<IOException>();
        deltas.Should().Equal("Hel");
    }

    [Test]
    public async Task StreamChatReportsAStreamThatEndsBeforeItsTerminalFrame()
    {
        var client = TestClient.Create(
            new FakeHandler(_ =>
                Responses.Sse(
                    "event: meta\ndata: {\"sessionId\":\"s1\"}\n\nevent: chunk\ndata: {\"text\":\"Hel\"}\n\n"
                )
            )
        );

        var act = async () =>
        {
            await foreach (var _ in client.StreamChatAsync("hi")) { }
        };

        await act.Should().ThrowAsync<AgentMemoryStreamException>();
    }

    [Test]
    public async Task StreamChatHonoursCancellationMidStream()
    {
        var firstFrame = Encoding.UTF8.GetBytes("event: chunk\ndata: {\"text\":\"Hel\"}\n\n");
        var client = TestClient.Create(
            new FakeHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new DroppingStream(firstFrame, stallInstead: true)),
            })
        );
        using var cancellation = new CancellationTokenSource();

        var act = async () =>
        {
            await foreach (
                var _ in client.StreamChatAsync("hi", cancellationToken: cancellation.Token)
            )
            {
                await cancellation.CancelAsync();
            }
        };

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    /// Serves one block of bytes, then fails like a dropped connection (or stalls until cancelled).
    /// </summary>
    private sealed class DroppingStream(byte[] first, bool stallInstead = false) : Stream
    {
        private bool _sent;

        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override async ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default
        )
        {
            if (!_sent)
            {
                _sent = true;
                first.CopyTo(buffer);
                return first.Length;
            }

            if (stallInstead)
            {
                await Task.Delay(Timeout.Infinite, cancellationToken);
            }

            throw new IOException("The response ended prematurely.");
        }

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken
        )
        {
            return ReadAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return ReadAsync(buffer, offset, count, CancellationToken.None)
                .GetAwaiter()
                .GetResult();
        }

        public override void Flush() { }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
