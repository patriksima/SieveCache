namespace SieveCache;

public class FifoCache<T>(int capacity) : ICache<T>
{
    private readonly Queue<T> _queue = new();
    private readonly HashSet<T> _set = [];

    public void Access(T item)
    {
        if (_set.Contains(item)) return;

        if (_queue.Count == capacity)
        {
            var removed = _queue.Dequeue();
            _set.Remove(removed);
        }

        _queue.Enqueue(item);
        _set.Add(item);
    }

    public bool Contains(T item)
    {
        return _set.Contains(item);
    }
}