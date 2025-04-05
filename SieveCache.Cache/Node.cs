namespace SieveCache;

public class Node<TKey, TValue>(TKey key, TValue value)
{
    public TKey Key { get; } = key;
    public TValue Value { get; set; } = value;
    public bool Visited { get; set; }
    public Node<TKey, TValue>? Prev { get; set; }
    public Node<TKey, TValue>? Next { get; set; }
}