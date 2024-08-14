﻿using System.Collections;
using System.Collections.Concurrent;
using ConcurrentCollections;

namespace SurrealDb.Net.Internals.Ws;

internal class WsResponseTaskHandler
    : IEnumerable<KeyValuePair<string, SurrealWsTaskCompletionSource>>
{
    private static readonly ConcurrentHashSet<string> _allResponseTaskIds = [];

    private readonly string _engineId;
    private readonly ConcurrentDictionary<
        string,
        SurrealWsTaskCompletionSource
    > _highResponseTasks = new();
    private readonly ConcurrentDictionary<
        string,
        SurrealWsTaskCompletionSource
    > _normalResponseTasks = new();
    private readonly ConcurrentDictionary<
        SurrealDbWsRequestPriority,
        TaskCompletionSource<bool>
    > _queueSources =
        new(
            [
                new(SurrealDbWsRequestPriority.High, new()),
                new(SurrealDbWsRequestPriority.Normal, new()),
            ]
        );

    private SurrealDbWsRequestPriority? _currentPriority;

    public WsResponseTaskHandler(string engineId)
    {
        _engineId = engineId;
    }

    public IEnumerator<KeyValuePair<string, SurrealWsTaskCompletionSource>> GetEnumerator()
    {
        foreach (var responseTask in _highResponseTasks)
        {
            yield return responseTask;
        }
        foreach (var responseTask in _normalResponseTasks)
        {
            yield return responseTask;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private ConcurrentDictionary<string, SurrealWsTaskCompletionSource> GetResponseTasksByPriority(
        SurrealDbWsRequestPriority priority
    )
    {
        return priority switch
        {
            SurrealDbWsRequestPriority.High => _highResponseTasks,
            SurrealDbWsRequestPriority.Normal => _normalResponseTasks,
            _ => throw new ArgumentOutOfRangeException(nameof(priority)),
        };
    }

    private void UpdateQueueSources()
    {
        SurrealDbWsRequestPriority? nextPriority = _highResponseTasks.IsEmpty
            ? _normalResponseTasks.IsEmpty
                ? null
                : SurrealDbWsRequestPriority.Normal
            : SurrealDbWsRequestPriority.High;

        if (nextPriority != _currentPriority)
        {
            if (nextPriority.HasValue)
            {
                _queueSources.GetValueOrDefault(nextPriority.Value)?.TrySetResult(true);
            }
            if (_currentPriority.HasValue)
            {
                _queueSources.TryRemove(_currentPriority.Value, out _);
                _queueSources.TryAdd(_currentPriority.Value, new());
            }

            _currentPriority = nextPriority;
        }
    }

    public bool TryAdd(
        string id,
        SurrealDbWsRequestPriority priority,
        SurrealWsTaskCompletionSource responseTaskCompletionSource
    )
    {
        var responseTasks = GetResponseTasksByPriority(priority);

        if (_allResponseTaskIds.Add(id))
        {
            if (responseTasks.TryAdd(id, responseTaskCompletionSource!))
            {
                UpdateQueueSources();
                return true;
            }

            _allResponseTaskIds.TryRemove(id);
            return false;
        }

        return false;
    }

    public bool TryRemove(string id, out SurrealWsTaskCompletionSource responseTaskCompletionSource)
    {
        bool result =
            (
                _normalResponseTasks.TryRemove(id, out responseTaskCompletionSource!)
                || _highResponseTasks.TryRemove(id, out responseTaskCompletionSource!)
            ) & _allResponseTaskIds.TryRemove(id);

        if (result)
        {
            UpdateQueueSources();
        }

        return result;
    }

    public async Task WaitUntilAsync(
        SurrealDbWsRequestPriority priority,
        CancellationToken cancellationToken
    )
    {
        var completionTokenSource = _queueSources.GetOrAdd(
            priority,
            _ => new TaskCompletionSource<bool>()
        );

        await completionTokenSource.Task.ConfigureAwait(false);
    }
}
