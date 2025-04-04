using System.Collections.Concurrent;

namespace SieveCache;

public class SieveCache<T>(int capacity)
    where T : notnull
{
    private readonly Dictionary<T, Node<T>?> _cache = new(capacity);
    private Node<T>? _head;
    private Node<T>? _tail;
    private Node<T>? _hand;
    private int _size;

    private void AddToHead(Node<T> node)
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

    private void RemoveNode(Node<T> node)
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

        //_cache.TryRemove(node.Value, out _);
        _cache.Remove(node.Value);
        RemoveNode(node);
        _size--;
    }

    public void Access(T value)
    {
        if (_cache.TryGetValue(value, out var node))
        {
            if (node is not null)
            {
                node.Visited = true;
            }
        }
        else
        {
            if (_size == capacity)
            {
                Evict();
            }

            var newNode = new Node<T>(value);
            AddToHead(newNode);
            _cache[value] = newNode;
            _size++;
            newNode.Visited = false;
        }
    }
    
    internal List<(T Value, bool Visited)> GetCacheContents()
    {
        var result = new List<(T, bool)>();
        var current = _head;
        while (current is not null)
        {
            result.Add((current.Value, current.Visited));
            current = current.Next;
        }
        return result;
    }
}