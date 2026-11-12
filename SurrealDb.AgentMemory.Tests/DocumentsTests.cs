using System.Net;
using System.Text;
using FluentAssertions;
using SurrealDb.AgentMemory.Model;
using SurrealDb.AgentMemory.Tests.Infrastructure;

namespace SurrealDb.AgentMemory.Tests;

/// <summary>
/// Mirrors <c>packages/tests/memory/unit/documents.test.ts</c>, plus reprocess, raw, listing and progress cases.
/// </summary>
public class DocumentsTests
{
    private static AgentMemoryClient Client(FakeHandler handler)
    {
        return TestClient.Create(handler, context: "c");
    }

    [Test]
    public async Task UploadSendsMultipartWithMetadataAndFileParts()
    {
        var handler = FakeHandler.Json(Fixtures.Upload("d1"));

        var response = await Client(handler)
            .Documents.UploadAsync(
                new DocumentUploadOptions
                {
                    File = new byte[] { 1, 2, 3 },
                    FileName = "a.txt",
                    ContentType = "text/plain",
                    Title = "Handbook",
                    Source = "https://example.test/a.txt",
                }
            );

        response.Id.Should().Be("d1");
        var request = handler.Last;
        request.Method.Should().Be("POST");
        request.UrlText.Should().EndWith("/api/v1/c/documents");
        request.Header("Idempotency-Key").Should().BeNull();

        // The server reads the fields in order, so the metadata part comes first.
        var parts = request.Parts!;
        parts.Select(part => part.Name).Should().Equal("metadata", "file");
        JsonAssert.Equivalent(
            System.Text.Json.Nodes.JsonNode.Parse(parts[0].Text),
            """{"title":"Handbook","source":"https://example.test/a.txt"}"""
        );
        parts[0].ContentType.Should().BeNull();
        parts[1].FileName.Should().Be("a.txt");
        parts[1].ContentType.Should().Be("text/plain");
        parts[1].Bytes.Should().Equal(1, 2, 3);
    }

    [Test]
    public async Task UploadOmitsTheMetadataPartWhenNoMetadataIsSupplied()
    {
        var handler = FakeHandler.Json(Fixtures.Upload("d2"));

        await Client(handler)
            .Documents.UploadAsync(new DocumentUploadOptions { File = new byte[] { 9 } });

        var parts = handler.Last.Parts!;
        parts.Should().ContainSingle();
        parts[0].Name.Should().Be("file");
        parts[0].FileName.Should().Be("upload");
        parts[0].ContentType.Should().Be("application/octet-stream");
    }

    [Test]
    public async Task UploadNormalizesScopesAndSendsLabelsInTheMetadata()
    {
        var handler = FakeHandler.Json(Fixtures.Upload("d3"));

        await Client(handler)
            .Documents.UploadAsync(
                new DocumentUploadOptions
                {
                    File = new MemoryStream([4, 5]),
                    Scopes = new[] { "team/eng", "org/acme" },
                    Labels = ["kind=handbook"],
                }
            );

        var parts = handler.Last.Parts!;
        JsonAssert.Equivalent(
            System.Text.Json.Nodes.JsonNode.Parse(parts[0].Text),
            """{"scopes":[["team/eng"],["org/acme"]],"labels":["kind=handbook"]}"""
        );
        parts[1].Bytes.Should().Equal(4, 5);
    }

    [Test]
    public async Task UploadStreamsAFileFromDiskUnderItsOwnName()
    {
        var path = Path.Combine(Path.GetTempPath(), $"agent-memory-{Guid.NewGuid():N}.md");
        await File.WriteAllTextAsync(path, "# Handbook");
        try
        {
            var handler = FakeHandler.Json(Fixtures.Upload("d4"));

            await Client(handler)
                .Documents.UploadAsync(
                    new DocumentUploadOptions { File = AgentMemoryFile.FromPath(path) }
                );

            var file = handler.Last.Parts!.Single();
            file.FileName.Should().Be(Path.GetFileName(path));
            file.Text.Should().Be("# Handbook");
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Test]
    public async Task UploadForwardsACallersCancellation()
    {
        // A large upload is otherwise unstoppable: multipart sends carry no deadline.
        using var cancellation = new CancellationTokenSource();
        var client = Client(FakeHandler.Stalled());

        var pending = client.Documents.UploadAsync(
            new DocumentUploadOptions { File = new byte[] { 9 } },
            cancellation.Token
        );
        await cancellation.CancelAsync();

        var act = () => pending;
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task UploadIsBoundedWhenGivenAnExplicitTimeout()
    {
        var client = Client(FakeHandler.Stalled());

        var act = () =>
            client.Documents.UploadAsync(
                new DocumentUploadOptions
                {
                    File = new byte[] { 9 },
                    Timeout = TimeSpan.FromMilliseconds(20),
                }
            );

        var error = await act.Should().ThrowAsync<Exception>();
        error.Which.Should().BeOfType<AgentMemoryConnectionException>();
    }

    [Test]
    public async Task UploadReportsProgressWhenAskedTo()
    {
        var handler = FakeHandler.Json(Fixtures.Upload("d5"));
        var progress = new RecordingProgress<UploadProgress>();

        await Client(handler)
            .Documents.UploadAsync(
                new DocumentUploadOptions { File = new byte[200_000], UploadProgress = progress }
            );

        progress.Reports.Count.Should().BeGreaterThan(1);
        progress.Reports[^1].Loaded.Should().Be(progress.Reports[^1].Total);
        handler.Last.Body!.Length.Should().BeGreaterThan(200_000);
    }

    [Test]
    public async Task ReprocessPutsAndFallsBackToAQueuedHandleOnAnEmptyBody()
    {
        var handler = new FakeHandler(_ => Responses.Empty(HttpStatusCode.Accepted));

        var response = await Client(handler)
            .Documents.ReprocessAsync(
                "document:1",
                new DocumentUploadOptions { File = new byte[] { 1 } }
            );

        handler.Last.Method.Should().Be("PUT");
        handler.Last.UrlText.Should().EndWith("/api/v1/c/documents/document%3A1");
        response.Id.Should().Be("document:1");
        response.Status.Should().Be(DocumentStatus.Queued);
        response.ContentHash.Should().BeEmpty();
        response.Deduplicated.Should().BeFalse();
    }

    [Test]
    public async Task RawReturnsTheOriginalBytes()
    {
        var handler = new FakeHandler(_ => Responses.Bytes([7, 8, 9]));

        var bytes = await Client(handler).Documents.RawAsync("document:1");

        bytes.Should().Equal(7, 8, 9);
        handler.Last.UrlText.Should().EndWith("/api/v1/c/documents/document%3A1/raw");
    }

    [Test]
    public async Task ListPassesFiltersAndCountReadsTheTotal()
    {
        var handler = FakeHandler.Json(
            $$"""{"documents":[{{Fixtures.Document("document:1")}}],"page":{{Fixtures.Page(true, "c2", 12)}}}"""
        );

        var page = await Client(handler)
            .Documents.ListAsync(
                new DocumentListOptions { Status = DocumentStatuses.Ready, MimeType = "text/plain" }
            );
        var total = await Client(handler)
            .Documents.CountAsync(new DocumentFilters { Status = "ready" });

        page.Documents.Should().ContainSingle().Which.Status.Should().Be(DocumentStatus.Ready);
        handler
            .Requests[0]
            .UrlText.Should()
            .Contain("status=ready")
            .And.Contain("mimeType=text%2Fplain");
        total.Should().Be(12);
        handler.Requests[1].UrlText.Should().Contain("limit=1").And.Contain("count=true");
    }

    [Test]
    public async Task ListAcceptsTheDeprecatedOffsetParametersButNeverWithACursor()
    {
        var handler = FakeHandler.Json($$"""{"documents":[],"page":{{Fixtures.Page()}}}""");
        var client = Client(handler);

        await client.Documents.ListAsync(new DocumentListOptions { Page = 2, PageSize = 20 });
        var act = () =>
            client.Documents.ListAsync(new DocumentListOptions { Page = 2, Cursor = "c" });

        handler.Last.UrlText.Should().Contain("page=2").And.Contain("pageSize=20");
        await act.Should().ThrowAsync<ArgumentException>();
        handler.Requests.Should().HaveCount(1);
    }

    [Test]
    public async Task AllChunksFollowsTheCursorAndStopsAtMax()
    {
        var handler = new FakeHandler(request =>
            Responses.Json(
                $$"""{"chunks":[{{Fixtures.Chunk("chunk:" + request.UrlText.Length)}}],"page":{{Fixtures.Page(true, "c" + request.UrlText.Length)}}}"""
            )
        );

        var chunks = await Client(handler).Documents.AllChunksAsync("document:1", limit: 1, max: 3);

        chunks.Should().HaveCount(3);
        handler
            .Requests[0]
            .UrlText.Should()
            .Contain("/api/v1/c/documents/document%3A1/chunks?limit=1");
    }

    [Test]
    public async Task QueryPostsToDocumentsQuery()
    {
        var handler = FakeHandler.Json(Fixtures.QueryResponse);

        var response = await Client(handler)
            .Documents.QueryAsync(new DocumentQueryOptions { Query = "hi", K = 5 });

        response.Results.Should().BeEmpty();
        handler.Last.UrlText.Should().EndWith("/api/v1/c/documents/query");
        handler.Last.Method.Should().Be("POST");
        // No lens given, so none is sent: the server then reads the whole granted region.
        JsonAssert.Equivalent(handler.Last.Json, """{"query":"hi","k":5}""");
        handler.Last.Header("Idempotency-Key").Should().HaveLength(64);
    }

    [Test]
    public async Task QueryNormalizesAReadLensIntoTheScopeSetsWireShape()
    {
        var handler = FakeHandler.Json(Fixtures.QueryResponse);
        var client = Client(handler);

        await client.Documents.QueryAsync(
            new DocumentQueryOptions { Query = "hi", Lens = "team/beta/*" }
        );
        JsonAssert.Equivalent(handler.Last.Json, """{"query":"hi","lens":[["team/beta/*"]]}""");

        await client.Documents.QueryAsync(
            new DocumentQueryOptions
            {
                Query = "hi",
                Lens = new[] { new[] { "team/alpha/*" }, new[] { "team/beta", "tier/gold" } },
            }
        );
        JsonAssert.Equivalent(
            handler.Last.Json,
            """{"query":"hi","lens":[["team/alpha/*"],["team/beta","tier/gold"]]}"""
        );
    }

    [Test]
    public async Task QueryOmitsALensThatNormalizesToNothing()
    {
        var handler = FakeHandler.Json(Fixtures.QueryResponse);

        await Client(handler)
            .Documents.QueryAsync(
                new DocumentQueryOptions { Query = "hi", Lens = Array.Empty<string>() }
            );

        JsonAssert.Equivalent(handler.Last.Json, """{"query":"hi"}""");
    }

    [Test]
    public async Task QuerySendsTheRetrievalSwitches()
    {
        var handler = FakeHandler.Json(Fixtures.QueryResponse);

        await Client(handler)
            .Documents.QueryAsync(
                new DocumentQueryOptions
                {
                    Query = "hi",
                    Mode = QueryModes.HybridGraph,
                    Threshold = 0.2,
                    UseHyde = true,
                    UseReranker = false,
                    GraphEdges = [GraphEdgeKind.SectionMatch],
                    Filter = new QueryFilter(documentIds: new List<string> { "document:1" }),
                }
            );

        JsonAssert.Equivalent(
            handler.Last.Json,
            """{"query":"hi","mode":"hybrid_graph","threshold":0.2,"useHyde":true,"useReranker":false,"graphEdges":["section_match"],"filter":{"documentIds":["document:1"]}}"""
        );
    }

    [Test]
    public async Task KeywordSearchPostsWithANormalizedLens()
    {
        var handler = FakeHandler.Json(Fixtures.QueryResponse);

        var response = await Client(handler)
            .Documents.Keywords.SearchAsync(
                new KeywordSearchOptions
                {
                    Query = "needle",
                    K = 3,
                    Threshold = 0.2,
                    Lens = new[] { "team/alpha/*", "team/beta/*" },
                }
            );

        response.Results.Should().BeEmpty();
        handler.Last.UrlText.Should().EndWith("/api/v1/c/documents/keywords/search");
        JsonAssert.Equivalent(
            handler.Last.Json,
            """{"query":"needle","k":3,"threshold":0.2,"lens":[["team/alpha/*"],["team/beta/*"]]}"""
        );
    }

    [Test]
    public async Task KeywordSearchOmitsTheLensWhenNoneIsGiven()
    {
        var handler = FakeHandler.Json(Fixtures.QueryResponse);

        await Client(handler)
            .Documents.Keywords.SearchAsync(new KeywordSearchOptions { Query = "needle" });

        JsonAssert.Equivalent(handler.Last.Json, """{"query":"needle"}""");
    }

    [Test]
    public async Task KeywordReadsUnwrapAndEncodeTheirPaths()
    {
        var handler = new FakeHandler(request =>
            Responses.Json(
                request.Path.EndsWith("/keywords", StringComparison.Ordinal)
                && request.Path.Contains("/documents/document", StringComparison.Ordinal)
                    ? $$"""{"keywords":[{{Fixtures.DocumentKeyword("keyword:1")}}]}"""
                    : $$"""{"keywords":[{{Fixtures.Keyword("keyword:1")}}],"page":{{Fixtures.Page()}}}"""
            )
        );
        var client = Client(handler);

        var linked = await client.Documents.Keywords.ForDocumentAsync("document:1");
        var all = await client.Documents.Keywords.ListAllAsync(
            new KeywordFilters { Q = "nee", MinDocumentCount = 2 }
        );

        linked.Should().ContainSingle().Which.Normalised.Should().Be("needle");
        handler.Requests[0].UrlText.Should().EndWith("/api/v1/c/documents/document%3A1/keywords");
        all.Should().ContainSingle();
        handler.Requests[1].UrlText.Should().Contain("q=nee").And.Contain("minDocumentCount=2");
    }

    [Test]
    public async Task DeleteAndRecomputeLinksUseTheirRoutes()
    {
        var handler = new FakeHandler(request =>
            request.Method == "DELETE"
                ? Responses.Empty()
                : Responses.Json("""{"linksEmitted":2}""")
        );
        var client = Client(handler);

        await client.Documents.DeleteAsync("document:1");
        var links = await client.Documents.RecomputeLinksAsync();

        handler.Requests[0].UrlText.Should().EndWith("/api/v1/c/documents/document%3A1");
        links.LinksEmitted.Should().Be(2);
        handler.Requests[1].UrlText.Should().EndWith("/api/v1/c/documents/recompute-links");
        handler.Requests[1].BodyText.Should().Be("{}");
    }

    [Test]
    public async Task UploadSendsANonAsciiFileNameAsRawUtf8()
    {
        // Browsers send the name as UTF-8; the service's multipart parser does not decode RFC 2047 ("=?utf-8?B?...?=").
        var handler = FakeHandler.Json(Fixtures.Upload("d6"));

        await Client(handler)
            .Documents.UploadAsync(
                new DocumentUploadOptions { File = new byte[] { 1 }, FileName = "résumé.pdf" }
            );

        var file = handler.Last.Parts!.Single(part => part.Name == "file");
        file.Disposition.Should().Be("form-data; name=\"file\"; filename=\"résumé.pdf\"");
        file.FileName.Should().Be("résumé.pdf");
        var wire = Encoding.Latin1.GetString(handler.Last.Body!);
        wire.Should().Contain(Encoding.Latin1.GetString(Encoding.UTF8.GetBytes("résumé.pdf")));
        wire.Should().NotContain("=?utf-8?");
    }

    [Test]
    public async Task UploadEscapesQuotesAndLineBreaksInTheFileNameLikeBrowsersDo()
    {
        var handler = FakeHandler.Json(Fixtures.Upload("d7"));

        await Client(handler)
            .Documents.UploadAsync(
                new DocumentUploadOptions { File = new byte[] { 1 }, FileName = "a\"b\r\nc\\d.txt" }
            );

        handler
            .Last.Parts!.Single(part => part.Name == "file")
            .FileName.Should()
            .Be("a%22b%0D%0Ac\\d.txt");
    }

    [Test]
    public async Task UploadWithProgressSendsTheSameEncodingAndAnAccurateLength()
    {
        var handler = FakeHandler.Json(Fixtures.Upload("d8"));
        var progress = new RecordingProgress<UploadProgress>();

        await Client(handler)
            .Documents.UploadAsync(
                new DocumentUploadOptions
                {
                    File = new byte[100_000],
                    FileName = "Präsentation.pdf",
                    Title = "Übersicht",
                    UploadProgress = progress,
                }
            );

        var parts = handler.Last.Parts!;
        parts.Single(part => part.Name == "file").FileName.Should().Be("Präsentation.pdf");
        progress.Reports[^1].Total.Should().Be(handler.Last.Body!.Length);
        progress.Reports[^1].Loaded.Should().Be(handler.Last.Body!.Length);
    }

    [Test]
    public async Task UploadRejectsAnInvalidContentTypeBeforeReadingTheFile()
    {
        var handler = FakeHandler.Json(Fixtures.Upload("d9"));
        var source = new TrackingStream([1, 2, 3]);

        var act = () =>
            Client(handler)
                .Documents.UploadAsync(
                    new DocumentUploadOptions
                    {
                        File = AgentMemoryFile.FromStream(source),
                        ContentType = "not a media type",
                    }
                );

        await act.Should().ThrowAsync<FormatException>();
        source.Reads.Should().Be(0);
        handler.Requests.Should().BeEmpty();
    }

    [Test]
    public async Task ReadsTheNanosecondTimestampsTheServiceWrites()
    {
        var handler = FakeHandler.Json(Fixtures.Document("document:1"));

        var document = await Client(handler).Documents.GetAsync("document:1");

        document
            .CreatedAt.Should()
            .Be(new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddTicks(1_234_567));
        document.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Test]
    public async Task AMalformedTimestampIsReportedAsAnUnexpectedResponse()
    {
        var handler = FakeHandler.Json(
            Fixtures.Document("document:1").Replace(Fixtures.Timestamp, "yesterday")
        );

        var act = () => Client(handler).Documents.GetAsync("document:1");

        var error = await act.Should().ThrowExactlyAsync<AgentMemoryException>();
        error.Which.Title.Should().Be("Unexpected response body");
    }

    private sealed class TrackingStream(byte[] content) : MemoryStream(content)
    {
        public int Reads { get; private set; }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Reads++;
            return base.Read(buffer, offset, count);
        }

        public override ValueTask<int> ReadAsync(
            Memory<byte> buffer,
            CancellationToken cancellationToken = default
        )
        {
            Reads++;
            return base.ReadAsync(buffer, cancellationToken);
        }

        public override Task<int> ReadAsync(
            byte[] buffer,
            int offset,
            int count,
            CancellationToken cancellationToken
        )
        {
            Reads++;
            return base.ReadAsync(buffer, offset, count, cancellationToken);
        }
    }

    [Test]
    public async Task AStreamBackedFileCanBeUploadedMoreThanOnce()
    {
        var handler = FakeHandler.Json(Fixtures.Upload("d10"));
        var source = new TrackingStream([1, 2, 3]);
        var file = AgentMemoryFile.FromStream(source);

        await Client(handler).Documents.UploadAsync(new DocumentUploadOptions { File = file });
        var readsAfterFirstUpload = source.Reads;
        await Client(handler)
            .Documents.ReprocessAsync("d10", new DocumentUploadOptions { File = file });

        handler
            .Requests.Select(request => request.Parts!.Single(part => part.Name == "file").Bytes)
            .Should()
            .AllSatisfy(bytes => bytes.Should().Equal(1, 2, 3));
        source.Reads.Should().Be(readsAfterFirstUpload);
    }

    [Test]
    public async Task AnUploadTimeoutBeyondTheTimerLimitMeansNoDeadline()
    {
        var handler = FakeHandler.Json(Fixtures.Upload("d11"));

        var response = await Client(handler)
            .Documents.UploadAsync(
                new DocumentUploadOptions { File = new byte[] { 1 }, Timeout = TimeSpan.MaxValue }
            );

        response.Id.Should().Be("d11");
    }

    [Test]
    public async Task RefusesPathSegmentsThatWouldAddressAnotherRoute()
    {
        // ".." would be resolved away, so DELETE /documents/.. would reach /api/v1/c/.
        var handler = FakeHandler.Json("{}");
        var client = Client(handler);

        var parent = () => client.Documents.DeleteAsync("..");
        var current = () => client.Entities.GetAsync("person", ".");
        var empty = () => client.Keys.DeleteAsync("");

        await parent.Should().ThrowAsync<ArgumentException>();
        await current.Should().ThrowAsync<ArgumentException>();
        await empty.Should().ThrowAsync<ArgumentException>();
        handler.Requests.Should().BeEmpty();
    }

    [Test]
    public async Task StillEncodesSegmentsThatOnlyContainDots()
    {
        var handler = FakeHandler.Json(Fixtures.Document("..."));

        await Client(handler).Documents.GetAsync("...");

        handler.Last.UrlText.Should().EndWith("/api/v1/c/documents/...");
    }
}
