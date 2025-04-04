namespace SieveCache;

public class LruCache<T>(int capacity) : ICache<T> where T : notnull
{
    private readonly Dictionary<T, LinkedListNode<T>> _cache = new();
    private readonly LinkedList<T> _order = [];

    public void Access(T item)
    {
        if (_cache.TryGetValue(item, out var node))
        {
            _order.Remove(node);
            _order.AddFirst(node);
        }
        else
        {
            if (_cache.Count == capacity)
            {
                var last = _order.Last!;
                _cache.Remove(last.Value);
                _order.RemoveLast();
            }

            var newNode = new LinkedListNode<T>(item);
            _order.AddFirst(newNode);
            _cache[item] = newNode;
        }
    }

    public bool Contains(T item)
    {
        return _cache.ContainsKey(item);
    }
}