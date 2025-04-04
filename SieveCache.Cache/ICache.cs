namespace SieveCache;

public interface ICache<in T>
{
    void Access(T item);
    bool Contains(T item);
} 