using System.Collections.Concurrent;
using System.Threading.Channels;

namespace SieveCache;

public class SieveCacheActor<TKey, TValue> : IAsyncCache<TKey, TValue>
    where TKey : notnull
{
    private readonly SieveCache<TKey, TValue> _inner;
    private readonly Channel<CacheCommand> _channel;
    private readonly Task _processingTask;

    private abstract record CacheCommand;

    private record PutCommand(TKey Key, TValue Value, TaskCompletionSource Tcs) : CacheCommand;

    private record GetCommand(TKey Key, TaskCompletionSource<TValue?> Tcs) : CacheCommand;

    private record ContainsCommand(TKey Key, TaskCompletionSource<bool> Tcs) : CacheCommand;

    private record ClearCommand(TaskCompletionSource Tcs) : CacheCommand;

    private record CountCommand(TaskCompletionSource<int> Tcs) : CacheCommand;

    public SieveCacheActor(int capacity)
    {
        _inner = new SieveCache<TKey, TValue>(capacity);
        _channel = Channel.CreateUnbounded<CacheCommand>(new UnboundedChannelOptions
        {
            SingleReader = true,
            AllowSynchronousContinuations = false
        });

        _processingTask = Task.Run(ProcessCommandsAsync);
    }

    private async Task ProcessCommandsAsync()
    {
        await foreach (var command in _channel.Reader.ReadAllAsync())
        {
            switch (command)
            {
                case PutCommand put:
                    _inner.Put(put.Key, put.Value);
                    put.Tcs.SetResult();
                    break;

                case GetCommand get:
                    var value = _inner.Get(get.Key);
                    get.Tcs.SetResult(value);
                    break;

                case ContainsCommand contains:
                    var exists = _inner.Contains(contains.Key);
                    contains.Tcs.SetResult(exists);
                    break;

                case ClearCommand clear:
                    _inner.Clear();
                    clear.Tcs.SetResult();
                    break;

                case CountCommand count:
                    count.Tcs.SetResult(_inner.Count);
                    break;
            }
        }
    }

    public Task<TValue?> GetAsync(TKey key)
    {
        var tcs = new TaskCompletionSource<TValue?>(TaskCreationOptions.RunContinuationsAsynchronously);
        _channel.Writer.TryWrite(new GetCommand(key, tcs));
        return tcs.Task;
    }

    public Task PutAsync(TKey key, TValue value)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _channel.Writer.TryWrite(new PutCommand(key, value, tcs));
        return tcs.Task;
    }

    public Task<bool> ContainsAsync(TKey key)
    {
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        _channel.Writer.TryWrite(new ContainsCommand(key, tcs));
        return tcs.Task;
    }

    public Task ClearAsync()
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _channel.Writer.TryWrite(new ClearCommand(tcs));
        return tcs.Task;
    }

    public Task<int> CountAsync()
    {
        var tcs = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        _channel.Writer.TryWrite(new CountCommand(tcs));
        return tcs.Task;
    }

    internal List<(TKey Key, TValue Value, bool Visited)> GetCacheContents()
    {
        return _inner.GetCacheContents();
    }
}