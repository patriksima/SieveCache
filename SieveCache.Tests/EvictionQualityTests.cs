using FluentAssertions;
using Xunit;

namespace SieveCache.Tests;

/// <summary>
/// Guards that an implementation's eviction quality (hit ratio on a skewed Zipf workload)
/// stays close to the canonical <see cref="SieveCache{TKey,TValue}"/> reference. This is the
/// key safety net for a sharded variant: sharding must not meaningfully degrade the hit ratio.
/// </summary>
public abstract class EvictionQualityTests
{
    protected abstract ICache<string, string> Create(int capacity);

    [Theory]
    [InlineData(100, 50_000)]
    [InlineData(1_000, 100_000)]
    public void HitRatio_IsCloseToReferenceSieveCache(int capacity, int accessCount)
    {
        var workload = TestWorkloads.GenerateZipfStrings(capacity * 2, accessCount);

        var reference = MeasureHitRatio(new SieveCache<string, string>(capacity), workload);
        var actual = MeasureHitRatio(Create(capacity), workload);

        // Within 10% relative of the reference hit ratio.
        actual.Should().BeApproximately(reference, reference * 0.10);
    }

    private static double MeasureHitRatio(ICache<string, string> cache, List<string> workload)
    {
        var hits = 0;
        foreach (var key in workload)
        {
            if (cache.Contains(key))
            {
                hits++;
                cache.Get(key);
            }
            else
            {
                cache.Put(key, key);
            }
        }

        return (double)hits / workload.Count;
    }
}

public class SieveCacheCoreEvictionQualityTests : EvictionQualityTests
{
    protected override ICache<string, string> Create(int capacity) => new SieveCacheCore(capacity);
}

public class OptimizedSieveCacheEvictionQualityTests : EvictionQualityTests
{
    protected override ICache<string, string> Create(int capacity) => new OptimizedSieveCache<string, string>(capacity);
}
