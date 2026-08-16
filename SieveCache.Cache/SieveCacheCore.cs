using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace SieveCache;

public class SieveCacheCore(int capacity) : ICache<string, string>
{
    private readonly ConcurrentDictionary<string, Node> _cache = new(Environment.ProcessorCount, capacity);
    private readonly ReaderWriterLockSlim _lock = new();

    private Node? _head;
    private Node? _tail;
    private Node? _hand;
    private int _size;

    private long _hits;
    private long _misses;

    public long Hits => Interlocked.Read(ref _hits);
    public long Misses => Interlocked.Read(ref _misses);

    public double HitRatio => (_hits + _misses) == 0 ? 0 : (double)_hits / (_hits + _misses);
    public double MissRatio => (_hits + _misses) == 0 ? 0 : (double)_misses / (_hits + _misses);

    private class Node(string key, string value)
    {
        public string Key { get; } = key;
        public string Value { get; set; } = value;
        private int _visited;

        public bool Visited
        {
            get => Volatile.Read(ref _visited) != 0;
            set => Volatile.Write(ref _visited, value ? 1 : 0);
        }

        public Node? Prev { get; set; }
        public Node? Next { get; set; }
    }

    public string? Get(string key)
    {
        if (!_cache.TryGetValue(key, out var node))
        {
            Interlocked.Increment(ref _misses);
            return null;
        }

        Interlocked.Increment(ref _hits);
        node.Visited = true;

        return node.Value;
    }

    public void Put(string key, string value)
    {
        _lock.EnterWriteLock();
        try
        {
            // The existence check must happen under the same lock as the mutation.
            // Reading it beforehand allows two threads to both observe "missing" and
            // each insert the key, duplicating the node in the list.
            if (_cache.TryGetValue(key, out var node) && node != null)
            {
                node.Visited = true;

                if (value.Equals(node.Value))
                {
                    return;
                }

                node.Value = value;
            }
            else
            {
                if (_size == capacity)
                {
                    Evict();
                }

                var newNode = new Node(key, value)
                {
                    Visited = false
                };
                AddToHead(newNode);
                _cache[key] = newNode;
                _size++;
            }
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public bool Contains(string key)
    {
        return _cache.ContainsKey(key);
    }

    public void Clear()
    {
        _lock.EnterWriteLock();
        try
        {
            _cache.Clear();
            _head = null;
            _tail = null;
            _hand = null;
            _size = 0;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void ResetStats()
    {
        Interlocked.Exchange(ref _hits, 0);
        Interlocked.Exchange(ref _misses, 0);
    }

    public int Count => _cache.Count;

    #region Only for testing

    internal List<(string Key, string Value, bool Visited)> GetCacheContents()
    {
        var result = new List<(string, string, bool)>();
        var current = _head;
        while (current is not null)
        {
            result.Add((current.Key, current.Value, current.Visited));
            current = current.Next;
        }

        return result;
    }

    #endregion

    #region Private Methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddToHead(Node node)
    {
        node.Next = _head;
        node.Prev = null!;

        if (_head is not null)
        {
            _head.Prev = node;
        }

        _head = node;
        _tail ??= node;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RemoveNode(Node node)
    {
        if (node.Prev is not null)
        {
            node.Prev.Next = node.Next;
        }
        else
        {
            _head = node.Next;
        }

        if (node.Next is not null)
        {
            node.Next.Prev = node.Prev;
        }
        else
        {
            _tail = node.Prev;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void Evict()
    {
        var node = _hand ?? _tail;
        while (node is { Visited: true })
        {
            node.Visited = false;
            node = node.Prev ?? _tail;
        }

        _hand = node?.Prev ?? null;

        if (node == null) return;

        _cache.Remove(node.Key, out _);
        RemoveNode(node);
        _size--;
    }

    #endregion
}