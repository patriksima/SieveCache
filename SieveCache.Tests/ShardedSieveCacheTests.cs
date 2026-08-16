namespace SieveCache.Tests;

// Sharded cache has no single global list/order, so it derives from the black-box contract
// only (not SingleListSieveContractTests). Concurrency and eviction-quality coverage live in
// the dedicated suites below.
public class ShardedSieveCacheTests : CacheContractTests
{
    protected override ICache<string, string> Create(int capacity)
        => new ShardedSieveCache<string, string>(capacity);
}

public class ShardedSieveCacheConcurrencyTests : ConcurrentCacheInvariantTests
{
    protected override ICache<string, string> Create(int capacity)
        => new ShardedSieveCache<string, string>(capacity);

    protected override List<(string Key, string Value, bool Visited)> GetContents(ICache<string, string> cache)
        => ((ShardedSieveCache<string, string>)cache).GetCacheContents();
}

public class ShardedSieveCacheEvictionQualityTests : EvictionQualityTests
{
    protected override ICache<string, string> Create(int capacity)
        => new ShardedSieveCache<string, string>(capacity);
}
