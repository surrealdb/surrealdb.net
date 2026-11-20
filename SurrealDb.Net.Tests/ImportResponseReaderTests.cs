using System.Net;
using System.Net.Http.Headers;
using Dahomey.Cbor;
using SurrealDb.Net.Exceptions.Methods;
using SurrealDb.Net.Internals.Cbor;
using SurrealDb.Net.Internals.Http;

namespace SurrealDb.Net.Tests;

public class ImportResponseReaderTests
{
    [Test]
    [Arguments("application/cbor")]
    [Arguments("Application/CBOR")]
    public async Task ShouldReadFailedStatementsWhateverTheMediaTypeCase(string mediaType)
    {
        var results = new List<Dictionary<string, string>>
        {
            new()
            {
                { "status", "OK" },
                { "time", "1ms" },
                { "result", "ok" },
            },
            new()
            {
                { "status", "ERR" },
                { "time", "1ms" },
                { "result", "boom" },
            },
        };

        using var stream = new MemoryStream();
        await Dahomey.Cbor.Cbor.SerializeAsync(results, stream, new CborOptions());

        using var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new ByteArrayContent(stream.ToArray()),
        };
        response.Content.Headers.ContentType = new MediaTypeHeaderValue(mediaType);

        Func<Task> func = () =>
            ImportResponseReader.EnsureSuccessAsync(
                response,
                SurrealDbCborOptions.Default.Value,
                CancellationToken.None
            );

        var exception = await func.Should().ThrowAsync<SurrealDbImportException>();
        exception.Which.Errors.Should().HaveCount(1);
        exception.Which.Message.Should().Contain("boom");
    }
}
