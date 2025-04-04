using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SieveCache;

public class OptimizedSieveCache<T> : IDisposable, ICache<T> where T : notnull
{
    private readonly int _capacity;
    private readonly Dictionary<T, int> _lookup;
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
        _lookup = new Dictionary<T, int>(capacity);
        _freeIndexes = new Stack<int>(capacity);
        _pool = ArrayPool<Node>.Shared;
        _array = _pool.Rent(capacity);

        for (var i = capacity - 1; i >= 0; i--)
        {
            _freeIndexes.Push(i);
        }
    }

    private struct Node
    {
        public T Value;
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

        var value = GetNode(index).Value;

        RemoveNode(index);
        _lookup.Remove(value);

        ref var node = ref GetNode(index);
        node = default;
        _freeIndexes.Push(index);

        _hand = node.Prev != -1 ? node.Prev : -1;
        _size--;
    }

    public void Access(T value)
    {
        if (_lookup.TryGetValue(value, out var index))
        {
            GetNode(index).Visited = true;
            return;
        }

        if (_size == _capacity)
        {
            Evict();
        }

        var i = _freeIndexes.Pop();
        ref var node = ref GetNode(i);

        node.Value = value;
        node.Visited = false;
        node.Prev = -1;
        node.Next = -1;

        AddToHead(i);
        _lookup[value] = i;
        _size++;
    }

    public bool Contains(T item)
    {
        return _lookup.ContainsKey(item);
    }

    internal List<(T Value, bool Visited)> GetCacheContents()
    {
        var result = new List<(T, bool)>();
        var index = _head;
        while (index != -1)
        {
            ref var node = ref GetNode(index);
            result.Add((node.Value, node.Visited));
            index = node.Next;
        }

        return result;
    }

    internal void AssertListIsConsistent()
    {
        var seen = new HashSet<int>();
        var index = _head;
        var prev = -1;

        while (index != -1)
        {
            if (!seen.Add(index))
                throw new Exception($"Cycle detected at index {index}");

            ref var node = ref GetNode(index);

            if (index == node.Next)
                throw new Exception($"Node[{index}] points to itself via Next");

            if (index == node.Prev)
                throw new Exception($"Node[{index}] points to itself via Prev");

            if (node.Prev != prev)
                throw new Exception($"Broken Prev linkage at index {index}");

            prev = index;
            index = node.Next;
        }
    }

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