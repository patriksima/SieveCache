namespace SieveCache.Tests;

public class SieveCacheCoreTests : SingleListSieveContractTests
{
    protected override ICache<string, string> Create(int capacity)
        => new SieveCacheCore(capacity);

    protected override List<(string Key, string Value, bool Visited)> GetContents(ICache<string, string> cache)
        => ((SieveCacheCore)cache).GetCacheContents();
}
