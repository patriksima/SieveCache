namespace SieveCache;

public class UnsafeSieveCache<T>
    where T : notnull
{
    private readonly int _capacity;
    private readonly Dictionary<T, int> _lookup;
    private readonly Node[] _cache;
    private readonly Stack<int> _freeIndexes;

    private int _head = -1;
    private int _tail = -1;
    private int _hand = -1;
    private int _size;

    public UnsafeSieveCache(int capacity)
    {
        _capacity = capacity;
        _cache = new Node[capacity];
        _lookup = new Dictionary<T, int>(capacity);
        _freeIndexes = new Stack<int>(capacity);
        for (var i = 0; i < capacity; i++)
        {
            _freeIndexes.Push(i);
        }
    }

    private struct Node()
    {
        public T Value { get; set; } = default!;
        public bool Visited { get; set; } = false;
        public int Prev { get; set; } = -1;
        public int Next { get; set; } = -1;
    }

    private void AddToHead(int index)
    {
        _cache[index].Next = _head;
        _cache[index].Prev = -1;

        if (_head != -1)
        {
            _cache[_head].Prev = index;
        }

        _head = index;

        if (_tail == -1)
        {
            _tail = index;
        }
    }

    private void RemoveNode(int index)
    {
        if (_cache[index].Prev != -1)
        {
            _cache[_cache[index].Prev].Next = _cache[index].Next;
        }
        else
        {
            _head = _cache[index].Next;
        }

        if (_cache[index].Next != -1)
        {
            _cache[_cache[index].Next].Prev = _cache[index].Prev;
        }
        else
        {
            _tail = _cache[index].Prev;
        }
    }

    private void Evict()
    {
        var index = _hand != -1 ? _hand : _tail;

        if (index == -1)
        {
            return;
        }

        while (_cache[index].Visited)
        {
            _cache[index].Visited = false;
            index = _cache[index].Prev != -1 ? _cache[index].Prev : _tail;
        }

        _hand = _cache[index].Prev != -1 ? _cache[index].Prev : -1;

        if (index == -1)
        {
            return;
        }

        _lookup.Remove(_cache[index].Value);

        RemoveNode(index);

        _cache[index].Value = default!;
        _cache[index].Next = -1;
        _cache[index].Prev = -1;
        _cache[index].Visited = false;

        _freeIndexes.Push(index);

        _size--;
    }

    public void Access(T value)
    {
        if (_lookup.TryGetValue(value, out var index))
        {
            if (index != -1)
            {
                _cache[index].Visited = true;
            }
        }
        else
        {
            if (_size == _capacity)
            {
                Evict();
            }

            var i = _freeIndexes.Pop();

            _cache[i].Value = value;
            _cache[i].Visited = false;

            AddToHead(i);

            _lookup[value] = i;
            _size++;
        }
    }

    internal List<(T Value, bool Visited)> GetCacheContents()
    {
        var result = new List<(T, bool)>();
        var current = _head;
        while (current != -1)
        {
            result.Add((_cache[current].Value, _cache[current].Visited));
            current = _cache[current].Next;
        }

        return result;
    }

    internal void AssertListIsConsistent()
    {
        var seen = new HashSet<int>();
        int index = _head;
        int prev = -1;

        while (index != -1)
        {
            if (!seen.Add(index))
                throw new Exception($"Cycle detected at index {index}");

            var node = _cache[index];

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
}