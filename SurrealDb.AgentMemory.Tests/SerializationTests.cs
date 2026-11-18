using System.Globalization;
using System.Text.Json;
using FluentAssertions;
using SurrealDb.AgentMemory.Client;
using SurrealDb.AgentMemory.Model;
using SurrealDb.AgentMemory.Tests.Infrastructure;

namespace SurrealDb.AgentMemory.Tests;

/// <summary>
/// What the generated models read and write, where the generator's defaults do not match the service.
/// </summary>
public class SerializationTests
{
    private static readonly DateTime Instant = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Test]
    [Arguments("2026-01-01T00:00:00Z")]
    [Arguments("2026-01-01T00:00:00+00:00")]
    [Arguments("2026-01-01T01:00:00+01:00")]
    [Arguments("2025-12-31T19:00:00-05:00")]
    [Arguments("2026-01-01T00:00:00")]
    public async Task ATimestampIsUtcHoweverItsOffsetIsWritten(string text)
    {
        // The same instant must not come back as local time, or without a kind, depending on how the
        // service spelled it: comparisons between the two ignore the kind.
        var handler = FakeHandler.Json(
            Fixtures.Document("document:1").Replace(Fixtures.Timestamp, text)
        );

        var document = await TestClient.Create(handler).Documents.GetAsync("document:1");

        document.CreatedAt.Should().Be(Instant);
        document.CreatedAt.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Test]
    [Arguments("th-TH")]
    [Arguments("ar-SA")]
    [Arguments("fa-IR")]
    [Arguments("en-US")]
    public async Task ABatchTimestampIsWrittenInTheGregorianCalendarWhateverTheCulture(
        string culture
    )
    {
        // The generator formats with the current culture: under th-TH the year 2026 went out as 2569,
        // and the service stored it.
        var handler = FakeHandler.Json(Fixtures.FactsBatchResponse);
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo(culture);
        try
        {
            await TestClient
                .Create(handler)
                .RememberManyAsync(
                    [
                        new BatchMessage("hi", TurnRole.User)
                        {
                            Ts = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc),
                        },
                    ]
                );
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }

        handler.Last.Json!["messages"]![0]!["ts"]!
            .GetValue<string>()
            .Should()
            .Be("2026-01-01T10:00:00.0000000Z");
    }

    [Test]
    public async Task ALocalBatchTimestampCarriesItsOffset()
    {
        var handler = FakeHandler.Json(Fixtures.FactsBatchResponse);
        var local = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Local);

        await TestClient
            .Create(handler)
            .RememberManyAsync([new BatchMessage("hi", TurnRole.User) { Ts = local }]);

        var sent = handler.Last.Json!["messages"]![0]!["ts"]!.GetValue<string>();
        DateTimeOffset
            .Parse(sent, CultureInfo.InvariantCulture)
            .Should()
            .Be(new DateTimeOffset(local));
        sent.Should().MatchRegex(@"(Z|[+-]\d\d:\d\d)$");
    }

    [Test]
    public async Task ABatchTimestampWithoutAKindIsRefusedBeforeItIsSent()
    {
        // It would go out without an offset, and the service refuses the whole batch for it.
        var handler = FakeHandler.Json(Fixtures.FactsBatchResponse);

        var act = () =>
            TestClient
                .Create(handler)
                .RememberManyAsync(
                    [
                        new BatchMessage("first", TurnRole.User),
                        new BatchMessage("second", TurnRole.User)
                        {
                            Ts = new DateTime(2026, 1, 1, 10, 0, 0),
                        },
                    ]
                );

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*Unspecified*");
        handler.Requests.Should().BeEmpty();
    }

    [Test]
    public async Task ANullMessageInABatchIsRefused()
    {
        var handler = FakeHandler.Json(Fixtures.FactsBatchResponse);

        var act = () =>
            TestClient
                .Create(handler)
                .RememberManyAsync([new BatchMessage("first", TurnRole.User), null!]);

        await act.Should().ThrowAsync<ArgumentException>();
        handler.Requests.Should().BeEmpty();
    }

    [Test]
    public async Task ABatchIsEnumeratedOnce()
    {
        var handler = FakeHandler.Json(Fixtures.FactsBatchResponse);
        var enumerations = 0;

        IEnumerable<BatchMessage> Messages()
        {
            enumerations++;
            yield return new BatchMessage("only", TurnRole.User);
        }

        await TestClient.Create(handler).RememberManyAsync(Messages());

        enumerations.Should().Be(1);
        handler.Last.Json!["messages"]!.AsArray().Should().ContainSingle();
    }

    [Test]
    public async Task ARequiredAverageThatTheServiceWritesAsNullIsReadAsNaN()
    {
        // The mean of no rows is NaN, which the service's JSON encoder writes as null. On a context
        // with no traces yet that used to fail the whole response.
        const string stats =
            """{"totalQueries":0,"cacheHits":0,"avgLatencyMs":null,"tierCounts":{"direct":0,"hybrid":0,"escalated":0},"responseTracesTotal":0,"responseTracesCached":0,"cacheHitRate":0.0,"windowHours":24,"sourceKindDistribution":[],"contradiction":{"reconciliations":0,"contradictions":0,"contradictionRate":0.0},"supersession":{"supersessionEvents":0,"entitiesChurned":0,"churnPerEntity":0.0},"retrieval":{"traces":0,"avgCandidateSet":null,"maxCandidateSet":0}}""";
        var handler = FakeHandler.Json(stats);

        var result = await TestClient.Create(handler).Traces.StatsAsync();

        result.TotalQueries.Should().Be(0);
        double.IsNaN(result.AvgLatencyMs).Should().BeTrue();
        double.IsNaN(result.Retrieval.AvgCandidateSet).Should().BeTrue();
        result.CacheHitRate.Should().Be(0.0);
    }

    [Test]
    public void AnOptionalNumberKeepsItsNull()
    {
        // Only a number the model requires is read as NaN; an optional one is still absent.
        var citation = JsonSerializer.Deserialize(
            """{"id":"c1","kind":"fact","marker":"[1]","score":null,"snippet":"s"}""",
            AgentMemoryJsonContext.Default.CitationJson
        )!;
        var fsck = JsonSerializer.Deserialize(
            """{"duplicateThreshold":null}""",
            AgentMemoryJsonContext.Default.FsckRequestJson
        )!;

        float.IsNaN(citation.Score).Should().BeTrue();
        fsck.DuplicateThreshold.Should().BeNull();
    }

    [Test]
    public void AFreeFormValueThatWasReadCanBeWrittenBack()
    {
        // A free-form property comes back as a JsonElement, which the context could not write.
        var request = JsonSerializer.Deserialize(
            """{"metadata":{"channel":"web","tags":["a","b"]}}""",
            AgentMemoryJsonContext.Default.CreateSessionRequestJson
        )!;

        var json = JsonSerializer.Serialize(
            request,
            AgentMemoryJsonContext.Default.CreateSessionRequestJson
        );

        JsonAssert.Equivalent(
            System.Text.Json.Nodes.JsonNode.Parse(json),
            """{"metadata":{"channel":"web","tags":["a","b"]}}"""
        );
    }

    [Test]
    public void AGeneratedHelperWorksWithOptionsThatWereNeverUsed()
    {
        // Such options have no metadata yet; they get the generated context rather than a failure.
        var options = new JsonSerializerOptions { Converters = { new PageMetaJsonConverter() } };

        var read = ClientUtils.TryDeserialize<PageMeta>(
            """{"hasMore":true,"nextCursor":"c2"}""",
            options,
            out var page
        );

        read.Should().BeTrue();
        page!.HasMore.Should().BeTrue();
        page.NextCursor.Should().Be("c2");
    }
}
