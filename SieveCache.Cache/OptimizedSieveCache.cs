using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SieveCache;

public class OptimizedSieveCache<TKey, TValue> : IDisposable, ICache<TKey, TValue> where TKey : notnull
{
    private readonly int _capacity;
    private readonly Dictionary<TKey, int> _lookup;
    private readonly Stack<int> _freeIndexes;
    private readonly Node[] _array;
    private readonly ArrayPool<Node> _pool;

    private int _head = -1;
    private int _tail = -1;
    private int _hand = -1;
    private int _size;

    public OptimizedSieveCache(int capacity)
    {
        _capacity = capacity;
        _lookup = new Dictionary<TKey, int>(capacity);
        _freeIndexes = new Stack<int>(capacity);
        _pool = ArrayPool<Node>.Shared;
        _array = _pool.Rent(capacity);

        for (var i = capacity - 1; i >= 0; i--)
        {
            _freeIndexes.Push(i);
        }
    }

    private struct Node(TKey key, TValue value)
    {
        public TKey Key = key;
        public TValue Value = value;
        public bool Visited;
        public int Prev;
        public int Next;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ref Node GetNode(int index)
    {
        ref var baseRef = ref MemoryMarshal.GetArrayDataReference(_array);
        return ref Unsafe.Add(ref baseRef, index);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddToHead(int index)
    {
        ref var node = ref GetNode(index);
        node.Next = _head;
        node.Prev = -1;

        if (_head != -1)
        {
            GetNode(_head).Prev = index;
        }

        _head = index;

        if (_tail == -1)
        {
            _tail = index;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void RemoveNode(int index)
    {
        ref var node = ref GetNode(index);

        if (node.Prev != -1)
        {
            GetNode(node.Prev).Next = node.Next;
        }
        else
        {
            _head = node.Next;
        }

        if (node.Next != -1)
        {
            GetNode(node.Next).Prev = node.Prev;
        }
        else
        {
            _tail = node.Prev;
        }
    }

    private void Evict()
    {
        var index = _hand != -1 ? _hand : _tail;
        if (index == -1) return;

        while (GetNode(index).Visited)
        {
            GetNode(index).Visited = false;
            index = GetNode(index).Prev != -1 ? GetNode(index).Prev : _tail;
        }

        if (index == -1) return;

        var key = GetNode(index).Key;

        RemoveNode(index);
        _lookup.Remove(key);

        ref var node = ref GetNode(index);
        node = default;
        _freeIndexes.Push(index);

        _hand = node.Prev != -1 ? node.Prev : -1;
        _size--;
    }

    public TValue? Get(TKey key)
    {
        if (!_lookup.TryGetValue(key, out var index) || index == -1)
        {
            return default;
        }

        ref var node = ref GetNode(index);

        node.Visited = true;

        return node.Value;
    }

    public void Put(TKey key, TValue value)
    {
        if (_lookup.TryGetValue(key, out var index))
        {
            ref var node = ref GetNode(index);
            node.Visited = true;

            if (!EqualityComparer<TValue>.Default.Equals(value, node.Value))
            {
                node.Value = value;
            }
        }
        else
        {
            if (_size == _capacity)
            {
                Evict();
            }

            var freeIndex = _freeIndexes.Pop();
            ref var node = ref GetNode(freeIndex);

            node.Key = key;
            node.Value = value;
            node.Visited = false;
            node.Prev = -1;
            node.Next = -1;

            AddToHead(freeIndex);
            _lookup[key] = freeIndex;
            _size++;
        }
    }

    public bool Contains(TKey key)
    {
        return _lookup.ContainsKey(key);
    }

    public void Clear()
    {
        _lookup.Clear();
        _freeIndexes.Clear();
        _hand = -1;
        _head = -1;
        _tail = -1;
        _size = 0;

        for (var i = _capacity - 1; i >= 0; i--)
        {
            _freeIndexes.Push(i);
        }
    }


    internal List<(TKey Key, TValue Value, bool Visited)> GetCacheContents()
    {
        var result = new List<(TKey, TValue, bool)>();
        var index = _head;
        while (index != -1)
        {
            ref var node = ref GetNode(index);
            result.Add((node.Key, node.Value, node.Visited));
            index = node.Next;
        }

        return result;
    }

    public int Count => _size;

    ~OptimizedSieveCache()
    {
        Dispose();
    }

    public void Dispose()
    {
        _pool.Return(_array);
        GC.SuppressFinalize(this);
    }
}