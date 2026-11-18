using System.Net;

namespace SurrealDb.AgentMemory;

/// <summary>
/// Progress of a request body being sent, reported while an upload is in flight.
/// </summary>
/// <param name="Loaded">Bytes handed to the network so far.</param>
/// <param name="Total">Total bytes to send, when known.</param>
public readonly record struct UploadProgress(long Loaded, long? Total);

/// <summary>
/// Wraps request content and reports how many bytes of it have been written.
/// </summary>
internal sealed class ProgressContent : HttpContent
{
    private const int BufferSize = 81920;

    private readonly HttpContent _inner;
    private readonly IProgress<UploadProgress> _progress;

    public ProgressContent(HttpContent inner, IProgress<UploadProgress> progress)
    {
        _inner = inner;
        _progress = progress;

        foreach (var header in inner.Headers.NonValidated)
        {
            // The length is reported through TryComputeLength, from the inner content.
            if (!string.Equals(header.Key, "Content-Length", StringComparison.OrdinalIgnoreCase))
            {
                Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }
    }

    protected override Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        return SerializeToStreamAsync(stream, context, CancellationToken.None);
    }

    protected override async Task SerializeToStreamAsync(
        Stream stream,
        TransportContext? context,
        CancellationToken cancellationToken
    )
    {
        var total = _inner.Headers.ContentLength;
        await using var source = await _inner
            .ReadAsStreamAsync(cancellationToken)
            .ConfigureAwait(false);

        var buffer = new byte[BufferSize];
        long loaded = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
        {
            await stream
                .WriteAsync(buffer.AsMemory(0, read), cancellationToken)
                .ConfigureAwait(false);
            loaded += read;
            _progress.Report(new UploadProgress(loaded, total));
        }
    }

    protected override bool TryComputeLength(out long length)
    {
        var contentLength = _inner.Headers.ContentLength;
        length = contentLength ?? 0;
        return contentLength is not null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _inner.Dispose();
        }
        base.Dispose(disposing);
    }
}
