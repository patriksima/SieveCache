using System.Collections.Concurrent;

namespace SieveCache;

public class SieveCache<TKey, TValue>(int capacity) : ICache<TKey, TValue>
    where TKey : notnull
{
    private readonly Dictionary<TKey, Node<TKey, TValue>?> _cache = new(capacity);
    private Node<TKey, TValue>? _head;
    private Node<TKey, TValue>? _tail;
    private Node<TKey, TValue>? _hand;
    private int _size;

    private void AddToHead(Node<TKey, TValue> node)
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

    private void RemoveNode(Node<TKey, TValue> node)
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

        _hand = node?.Prev ?? null;

        if (node == null) return;

        _cache.Remove(node.Key, out _);
        RemoveNode(node);
        _size--;
    }

    public TValue? Get(TKey key)
    {
        if (!_cache.TryGetValue(key, out var node) || node is null)
        {
            return default;
        }

        node.Visited = true;

        return node.Value;
    }

    public void Put(TKey key, TValue value)
    {
        if (_cache.TryGetValue(key, out var node))
        {
            if (node is not null && value is not null)
            {
                node.Visited = true;

                if (!value.Equals(node.Value))
                {
                    node.Value = value;
                }
            }
        }
        else
        {
            if (_size == capacity)
            {
                Evict();
            }

            var newNode = new Node<TKey, TValue>(key, value)
            {
                Visited = false
            };
            AddToHead(newNode);
            _cache[key] = newNode;
            _size++;
        }
    }

    public bool Contains(TKey key)
    {
        return _cache.ContainsKey(key);
    }

    public void Clear()
    {
        _cache.Clear();
        _head = null;
        _tail = null;
        _hand = null;
        _size = 0;
    }

    public int Count => _cache.Count;

    internal List<(TKey Key, TValue Value, bool Visited)> GetCacheContents()
    {
        var result = new List<(TKey, TValue, bool)>();
        var current = _head;
        while (current is not null)
        {
            result.Add((current.Key, current.Value, current.Visited));
            current = current.Next;
        }

        return result;
    }
}