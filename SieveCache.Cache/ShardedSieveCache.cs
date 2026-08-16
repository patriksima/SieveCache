using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace SieveCache;

/// <summary>
/// Thread-safe SIEVE cache that scales writes by partitioning the keyspace across independent
/// shards (lock striping, as in Memcached and <see cref="ConcurrentDictionary{TKey,TValue}"/>),
/// so writes to different shards never contend. Reads are lock-free: a hit only flips a volatile
/// visited flag and returns the node's value, never taking the shard lock.
///
/// Each key owns a distinct node, so a lock-free reader either misses (key already evicted from
/// the map) or reads that key's own node — it can never observe another key's value, which is why
/// no re-validation is needed on the read path.
///
/// The shard is chosen by masking a spread hash, so the shard count is a power of two. It is capped
/// at the capacity (and the core count) so every shard holds at least one entry and the total size
/// never exceeds <c>capacity</c>.
/// </summary>
public sealed class ShardedSieveCache<TKey, TValue> : ICache<TKey, TValue>
    where TKey : notnull
{
    private readonly Shard[] _shards;
    private readonly int _mask;

    public ShardedSieveCache(int capacity) : this(capacity, DefaultShardCount(capacity))
    {
    }

    public ShardedSieveCache(int capacity, int shardCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(shardCount, 1);

        // Power of two so the shard index is a cheap mask; never more shards than entries,
        // otherwise a shard would get zero capacity and its keys would never be cached.
        var count = LargestPowerOfTwoAtMost(Math.Min(shardCount, capacity));
        _mask = count - 1;
        _shards = new Shard[count];

        // Distribute capacity exactly so the shard capacities sum to `capacity`.
        var baseCapacity = capacity / count;
        var remainder = capacity % count;
        for (var i = 0; i < count; i++)
        {
            _shards[i] = new Shard(baseCapacity + (i < remainder ? 1 : 0));
        }
    }

    public int ShardCount => _shards.Length;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Shard GetShard(TKey key) => _shards[Spread(key.GetHashCode()) & _mask];

    public TValue? Get(TKey key) => GetShard(key).Get(key);

    public void Put(TKey key, TValue value) => GetShard(key).Put(key, value);

    public bool Contains(TKey key) => GetShard(key).Contains(key);

    public void Clear()
    {
        foreach (var shard in _shards)
        {
            shard.Clear();
        }
    }

    public int Count
    {
        get
        {
            var total = 0;
            foreach (var shard in _shards)
            {
                total += shard.Count;
            }

            return total;
        }
    }

    internal List<(TKey Key, TValue Value, bool Visited)> GetCacheContents()
    {
        var result = new List<(TKey, TValue, bool)>();
        foreach (var shard in _shards)
        {
            shard.CollectContents(result);
        }

        return result;
    }

    private static int DefaultShardCount(int capacity)
        => LargestPowerOfTwoAtMost(Math.Min(Environment.ProcessorCount, capacity));

    private static int LargestPowerOfTwoAtMost(int value)
    {
        var result = 1;
        while (result * 2 <= value)
        {
            result <<= 1;
        }

        return result;
    }

    // Avalanche the hash so shard selection stays uniform even for keys whose GetHashCode has
    // weak low bits (e.g. identity-hashed integers). Cheap ALU-only mix.
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Spread(int hash)
    {
        var x = (uint)hash;
        x ^= x >> 16;
        x *= 0x7feb352d;
        x ^= x >> 15;
        return (int)x;
    }

    /// <summary>
    /// A single independent SIEVE cache. Structural mutations take <see cref="_lock"/>; reads go
    /// straight through the <see cref="ConcurrentDictionary{TKey,TValue}"/> and only touch the
    /// node's volatile visited flag, so they never block behind a writer.
    /// </summary>
    private sealed class Shard
    {
        private readonly int _capacity;
        private readonly ConcurrentDictionary<TKey, Node> _cache;
        private readonly object _lock = new();

        private Node? _head;
        private Node? _tail;
        private Node? _hand;
        private int _size;

        public Shard(int capacity)
        {
            _capacity = capacity;
            _cache = new ConcurrentDictionary<TKey, Node>();
        }

        private sealed class Node(TKey key, TValue value)
        {
            public readonly TKey Key = key;
            public TValue Value = value;
            private int _visited;

            public bool Visited
            {
                get => Volatile.Read(ref _visited) != 0;
                set => Volatile.Write(ref _visited, value ? 1 : 0);
            }

            public Node? Prev;
            public Node? Next;
        }

        public TValue? Get(TKey key)
        {
            if (_cache.TryGetValue(key, out var node))
            {
                node.Visited = true;
                return node.Value;
            }

            return default;
        }

        public void Put(TKey key, TValue value)
        {
            lock (_lock)
            {
                // Existence check and mutation share one lock; checking beforehand would let two
                // threads both see "missing" and insert the key twice, duplicating the list node.
                if (_cache.TryGetValue(key, out var node))
                {
                    node.Visited = true;

                    if (!EqualityComparer<TValue>.Default.Equals(value, node.Value))
                    {
                        node.Value = value;
                    }

                    return;
                }

                if (_size == _capacity)
                {
                    Evict();
                }

                var newNode = new Node(key, value);
                AddToHead(newNode);
                _cache[key] = newNode; // publish last: readers only reach the node through the map
                _size++;
            }
        }

        public bool Contains(TKey key) => _cache.ContainsKey(key);

        public int Count => _cache.Count;

        public void Clear()
        {
            lock (_lock)
            {
                _cache.Clear();
                _head = null;
                _tail = null;
                _hand = null;
                _size = 0;
            }
        }

        public void CollectContents(List<(TKey Key, TValue Value, bool Visited)> result)
        {
            lock (_lock)
            {
                var current = _head;
                while (current is not null)
                {
                    result.Add((current.Key, current.Value, current.Visited));
                    current = current.Next;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AddToHead(Node node)
        {
            node.Prev = null;
            node.Next = _head;

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

        private void Evict()
        {
            var node = _hand ?? _tail;
            while (node is { Visited: true })
            {
                node.Visited = false;
                node = node.Prev ?? _tail;
            }

            _hand = node?.Prev;

            if (node is null)
            {
                return;
            }

            _cache.TryRemove(node.Key, out _);
            RemoveNode(node);
            _size--;
        }
    }
}
