using System.Collections.Concurrent;
using FluentAssertions;
using Xunit;

namespace SieveCache.Tests;

/// <summary>
/// Concurrency tests for thread-safe cache implementations. Unlike a plain "does not throw"
/// stress test, these assert structural invariants after the parallel load completes:
/// the size stays within capacity, the internal list and the lookup agree, keys are unique,
/// and stored values are not torn. A sharded implementation can plug in by deriving from this.
/// </summary>
public abstract class ConcurrentCacheInvariantTests
{
    protected abstract ICache<string, string> Create(int capacity);

    /// <summary>Head-to-tail snapshot of the internal list; must be called with no concurrent access.</summary>
    protected abstract List<(string Key, string Value, bool Visited)> GetContents(ICache<string, string> cache);

    private static ParallelOptions Parallelism => new() { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 };

    [Fact]
    public void Stress_MaintainsInvariants_UnderParallelLoad()
    {
        const int capacity = 500;
        const int keyspace = 1000; // 2x capacity forces continuous eviction
        var cache = Create(capacity);
        var exceptions = new ConcurrentBag<Exception>();

        Parallel.For(0, 200_000, Parallelism, i =>
        {
            try
            {
                var key = (i % keyspace).ToString();
                switch (i % 4)
                {
                    case 0:
                        cache.Put(key, key);
                        break;
                    case 1:
                        cache.Get(key);
                        break;
                    default:
                        cache.Contains(key);
                        break;
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        AssertInvariants(cache, capacity, exceptions);
    }

    [Fact]
    public void EvictionStorm_MaintainsInvariants()
    {
        const int capacity = 8;
        const int keyspace = 2000; // tiny cache, huge keyspace -> eviction on almost every put
        var cache = Create(capacity);
        var exceptions = new ConcurrentBag<Exception>();

        Parallel.For(0, 200_000, Parallelism, i =>
        {
            try
            {
                var key = (i % keyspace).ToString();
                if (i % 3 == 0)
                {
                    cache.Put(key, key);
                }
                else
                {
                    cache.Get(key);
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        AssertInvariants(cache, capacity, exceptions);
    }

    [Fact]
    public void ConcurrentPut_SameKey_StoredExactlyOnce()
    {
        var cache = Create(100);
        var exceptions = new ConcurrentBag<Exception>();

        Parallel.For(0, 50_000, Parallelism, i =>
        {
            try
            {
                cache.Put("k", "k");
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        exceptions.Should().BeEmpty();
        cache.Count.Should().Be(1);

        var contents = GetContents(cache);
        contents.Should().ContainSingle(x => x.Key == "k");
        cache.Get("k").Should().Be("k");
    }

    /// <summary>
    /// Deterministically targets the check-then-insert window: every thread blocks on a barrier
    /// and then inserts the same brand-new key at once. A cache that reads "does this key exist?"
    /// outside its write lock will insert the key more than once and corrupt the list.
    /// Barrier + dedicated threads are used instead of Parallel.For, whose thread ramp-up
    /// lets the first insert win before the others start and hides the race.
    /// </summary>
    [Fact]
    public void ConcurrentPut_SameNewKey_UnderBarrier_NeverDuplicates()
    {
        var threadCount = Math.Max(4, Environment.ProcessorCount * 2);

        for (var round = 0; round < 200; round++)
        {
            var cache = Create(100);
            var key = "race-" + round; // brand-new key each round -> everyone hits the insert path
            var barrier = new Barrier(threadCount);
            var exceptions = new ConcurrentBag<Exception>();

            var threads = Enumerable.Range(0, threadCount).Select(_ => new Thread(() =>
            {
                try
                {
                    barrier.SignalAndWait();
                    cache.Put(key, key);
                }
                catch (Exception ex)
                {
                    exceptions.Add(ex);
                }
            })).ToList();

            foreach (var t in threads) t.Start();
            foreach (var t in threads) t.Join();

            exceptions.Should().BeEmpty($"round {round}");
            cache.Count.Should().Be(1, $"round {round}");

            var contents = GetContents(cache);
            contents.Should().HaveCount(cache.Count, $"round {round}: list and lookup disagree");
            contents.Select(x => x.Key).Should().OnlyHaveUniqueItems($"round {round}: duplicate node in list");
        }
    }

    private void AssertInvariants(ICache<string, string> cache, int capacity, ConcurrentBag<Exception> exceptions)
    {
        exceptions.Should().BeEmpty();
        cache.Count.Should().BeLessThanOrEqualTo(capacity);

        var contents = GetContents(cache);

        // The internal list and the lookup must agree on size.
        contents.Should().HaveCount(cache.Count);

        // No key may appear twice in the list.
        contents.Select(x => x.Key).Should().OnlyHaveUniqueItems();

        // Every stored value was written as value == key; a mismatch means a torn/misplaced write.
        contents.Should().OnlyContain(x => x.Value == x.Key);
    }
}

public class SieveCacheCoreConcurrencyTests : ConcurrentCacheInvariantTests
{
    protected override ICache<string, string> Create(int capacity)
        => new SieveCacheCore(capacity);

    protected override List<(string Key, string Value, bool Visited)> GetContents(ICache<string, string> cache)
        => ((SieveCacheCore)cache).GetCacheContents();
}
