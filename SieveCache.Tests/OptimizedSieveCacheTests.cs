namespace SieveCache.Tests;

public class OptimizedSieveCacheTests : SingleListSieveContractTests
{
    protected override ICache<string, string> Create(int capacity)
        => new OptimizedSieveCache<string, string>(capacity);

    protected override List<(string Key, string Value, bool Visited)> GetContents(ICache<string, string> cache)
        => ((OptimizedSieveCache<string, string>)cache).GetCacheContents();
}
