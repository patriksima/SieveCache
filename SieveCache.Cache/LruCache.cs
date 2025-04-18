﻿namespace SieveCache;

public class LruCache<TKey, TValue>(int capacity) : ICache<TKey, TValue>
    where TKey : notnull
{
    private readonly Dictionary<TKey, LinkedListNode<(TKey Key, TValue Value)>> _cache = new();
    private readonly LinkedList<(TKey Key, TValue Value)> _order = [];

    public TValue? Get(TKey key)
    {
        if (_cache.TryGetValue(key, out var node))
        {
            // Move to front (most recently used)

            _order.Remove(node);
            _order.AddFirst(node);


            return node.Value.Value;
        }

        return default;
    }

    public void Put(TKey key, TValue value)
    {
        if (_cache.TryGetValue(key, out var existingNode))
        {
            // Update value and move to front
            _order.Remove(existingNode);
        }
        else if (_cache.Count == capacity)
        {
            // Remove least recently used
            var lastNode = _order.Last!;
            _order.RemoveLast();
            _cache.Remove(lastNode.Value.Key, out _);
        }

        var newNode = new LinkedListNode<(TKey, TValue)>((key, value));

        _order.AddFirst(newNode);

        _cache[key] = newNode;
    }

    public bool Contains(TKey key)
    {
        return _cache.ContainsKey(key);
    }

    public void Clear()
    {
        _order.Clear();
        _cache.Clear();
    }

    public int Count => _cache.Count;

    // Pro testování/debug
    internal List<TKey> GetCacheKeysInOrder()
    {
        return _order.Select(entry => entry.Key).ToList();
    }
}