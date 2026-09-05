using FluentAssertions;
using Xunit;

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

public class ShardedSieveCacheShardCountTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(3, 1)]
    [InlineData(15, 1)]
    [InlineData(16, 2)]
    public void DefaultShardCount_KeepsSmallCachesUnsharded(int capacity, int expectedShards)
    {
        // Sharding a tiny cache would let one hot shard evict while the cache is still below
        // capacity; the default therefore only splits when every shard gets MinShardCapacity.
        var cache = new ShardedSieveCache<string, string>(capacity);

        cache.ShardCount.Should().Be(Math.Min(expectedShards, Environment.ProcessorCount));
    }

    [Fact]
    public void DefaultShardCount_NeverExceedsCoreCount()
    {
        var cache = new ShardedSieveCache<string, string>(1_000_000);

        cache.ShardCount.Should().BeLessThanOrEqualTo(Environment.ProcessorCount);
    }

    [Fact]
    public void ExplicitShardCount_IsHonoured()
    {
        var cache = new ShardedSieveCache<string, string>(capacity: 3, shardCount: 2);

        cache.ShardCount.Should().Be(2);
    }
}
