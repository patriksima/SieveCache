namespace SieveCache;

public class Node<T>(T value)
{
    public T Value { get; set; } = value;
    public bool Visited { get; set; }
    public Node<T>? Prev { get; set; }
    public Node<T>? Next { get; set; }
}