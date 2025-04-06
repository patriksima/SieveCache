namespace SieveCache;

public interface IAsyncCache<in TKey, TValue>
{
    Task<TValue?> GetAsync(TKey key);
    Task PutAsync(TKey key, TValue value);
    Task<bool> ContainsAsync(TKey key);
    Task ClearAsync();
    Task<int> CountAsync();
}