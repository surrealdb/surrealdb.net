using System.Runtime.CompilerServices;
using SurrealDb.Net.Exceptions;
using SurrealDb.Net.Models.Response;

namespace SurrealDb.Net.Internals.Helpers;

/// <summary>
/// Turns the answer of a buffered query into the frames of a streaming query,
/// for connections that cannot stream.
/// </summary>
internal static class QueryStreamFallback
{
    private const int CBOR_MAJOR_TYPE_ARRAY = 4;

    public static async IAsyncEnumerable<SurrealDbQueryStreamFrame> ToFramesAsync(
        Func<CancellationToken, Task<SurrealDbResponse>> query,
        [EnumeratorCancellation] CancellationToken cancellationToken = default
    )
    {
        var response = await query(cancellationToken).ConfigureAwait(false);

        foreach (var frame in ToFrames(response))
        {
            yield return frame;
        }
    }

    public static IEnumerable<SurrealDbQueryStreamFrame> ToFrames(SurrealDbResponse response)
    {
        yield return new SurrealDbQueryStreamBeginFrame(response.Count);

        var totalTime = TimeSpan.Zero;

        for (int index = 0; index < response.Count; index++)
        {
            switch (response[index])
            {
                case SurrealDbOkResult okResult:
                {
                    var value = okResult.RawResult;
                    // 💡 A buffered answer does not say whether a statement returns a single value,
                    // so an array is taken for rows, as it is for most statements that return one
                    bool isList = !value.IsEmpty && value.Span[0] >> 5 == CBOR_MAJOR_TYPE_ARRAY;

                    yield return isList
                        ? new SurrealDbQueryStreamRowsFrame(index, value, okResult.CborOptions!)
                        : new SurrealDbQueryStreamValueFrame(index, value, okResult.CborOptions!);
                    yield return new SurrealDbQueryStreamFinishedFrame(
                        index,
                        okResult.Time,
                        okResult.Type,
                        !isList,
                        null
                    );
                    totalTime += okResult.Time;
                    break;
                }
                case ISurrealDbErrorResult errorResult:
                {
                    var time = errorResult is SurrealDbErrorResult result
                        ? result.Time
                        : TimeSpan.Zero;

                    yield return new SurrealDbQueryStreamFinishedFrame(
                        index,
                        time,
                        SurrealDbResponseType.Other,
                        false,
                        new SurrealDbErrorResultException(errorResult)
                    );
                    totalTime += time;
                    break;
                }
            }
        }

        yield return new SurrealDbQueryStreamEndFrame(response.Count, totalTime, null);
    }
}
