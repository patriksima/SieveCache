using FluentAssertions;
using Xunit;

namespace SieveCache.Tests;

/// <summary>
/// Black-box behaviour that must hold for ANY <see cref="ICache{TKey,TValue}"/> implementation,
/// regardless of eviction policy or internal layout. Concrete implementations plug in via
/// <see cref="Create"/>. Keys/values are strings so string-only implementations
/// (e.g. <c>SieveCacheCore</c>) satisfy the same contract.
/// </summary>
public abstract class CacheContractTests
{
    protected abstract ICache<string, string> Create(int capacity);

    [Fact]
    public void Get_ReturnsValue_AfterPut()
    {
        var cache = Create(2);
        cache.Put("a", "Apple");

        cache.Get("a").Should().Be("Apple");
    }

    [Fact]
    public void Get_ReturnsNull_WhenKeyDoesNotExist()
    {
        var cache = Create(2);

        cache.Get("missing").Should().BeNull();
    }

    [Fact]
    public void Put_UpdatesValue_IfKeyExists()
    {
        var cache = Create(2);
        cache.Put("a", "Apple");
        cache.Put("a", "Avocado");

        cache.Get("a").Should().Be("Avocado");
        cache.Count.Should().Be(1);
    }

    [Fact]
    public void Contains_ReturnsTrue_IfKeyExists()
    {
        var cache = Create(2);
        cache.Put("1", "One");

        cache.Contains("1").Should().BeTrue();
    }

    [Fact]
    public void Contains_ReturnsFalse_IfKeyDoesNotExist()
    {
        var cache = Create(2);

        cache.Contains("42").Should().BeFalse();
    }

    [Fact]
    public void Count_ReturnsCorrectNumberOfItems()
    {
        var cache = Create(3);
        cache.Put("1", "1");
        cache.Put("2", "2");

        cache.Count.Should().Be(2);
    }

    [Fact]
    public void Clear_RemovesAllItems()
    {
        var cache = Create(3);
        cache.Put("1", "1");
        cache.Put("2", "2");

        cache.Clear();

        cache.Count.Should().Be(0);
        cache.Contains("1").Should().BeFalse();
        cache.Contains("2").Should().BeFalse();
    }

    [Fact]
    public void Count_NeverExceedsCapacity_UnderSequentialOverflow()
    {
        const int capacity = 10;
        var cache = Create(capacity);

        for (var i = 0; i < 1000; i++)
        {
            cache.Put(i.ToString(), i.ToString());
            cache.Count.Should().BeLessThanOrEqualTo(capacity);
        }
    }

    [Fact]
    public void Capacity1_KeepsOnlyMostRecentInsert()
    {
        var cache = Create(1);

        cache.Put("a", "1");
        cache.Put("b", "2");

        cache.Count.Should().Be(1);
        cache.Contains("b").Should().BeTrue();
        cache.Contains("a").Should().BeFalse();
        cache.Get("b").Should().Be("2");
    }
}

/// <summary>
/// Additional behaviour specific to SIEVE implementations backed by a single global list:
/// insertion order, lazy promotion via the visited flag, and hand-based eviction.
/// These do NOT apply to sharded/segmented variants (no single global ordering),
/// so such variants should derive from <see cref="CacheContractTests"/> only.
/// </summary>
public abstract class SingleListSieveContractTests : CacheContractTests
{
    /// <summary>Head-to-tail snapshot of the internal SIEVE list for the given cache.</summary>
    protected abstract List<(string Key, string Value, bool Visited)> GetContents(ICache<string, string> cache);

    [Fact]
    public void Access_AddsItems_AndPreservesOrder()
    {
        var cache = Create(3);
        cache.Put("1", "1");
        cache.Put("2", "2");
        cache.Put("3", "3");

        GetContents(cache).Select(x => x.Value).Should().ContainInOrder("3", "2", "1");
    }

    [Fact]
    public void Access_MarksExistingItemAsVisited()
    {
        var cache = Create(3);
        cache.Put("1", "1");
        cache.Put("2", "2");
        cache.Put("1", "1"); // again -> visited

        GetContents(cache).Should().ContainSingle(x => x.Value == "1" && x.Visited);
    }

    [Fact]
    public void Get_MarksItemAsVisited()
    {
        var cache = Create(2);
        cache.Put("a", "Apple");
        cache.Put("b", "Banana");

        cache.Get("a").Should().Be("Apple");

        GetContents(cache).Should().Contain(x => x.Key == "a" && x.Visited);
    }

    [Fact]
    public void Evict_RemovesUnvisitedItem_WhenCacheIsFull()
    {
        var cache = Create(2);
        cache.Put("1", "1"); // unvisited
        cache.Put("2", "2"); // unvisited
        cache.Put("3", "3"); // should evict 1

        var contents = GetContents(cache);
        contents.Select(x => x.Value).Should().BeEquivalentTo("3", "2");
        contents.Should().NotContain(x => x.Value == "1");
    }

    [Fact]
    public void Evict_SkipsVisitedItems()
    {
        var cache = Create(2);
        cache.Put("1", "1");
        cache.Put("2", "2");
        cache.Put("1", "1"); // mark 1 visited
        cache.Put("3", "3"); // should evict 2

        var contents = GetContents(cache);
        contents.Select(x => x.Value).Should().BeEquivalentTo("3", "1");
        contents.Should().NotContain(x => x.Value == "2");
    }

    [Fact]
    public void Evict_ResetsVisitedFlag()
    {
        var cache = Create(2);
        cache.Put("1", "1");
        cache.Put("2", "2");
        cache.Put("1", "1"); // visited
        cache.Put("2", "2"); // visited
        cache.Put("3", "3"); // triggers full eviction scan

        GetContents(cache).Where(x => x.Value != "3")
            .Should().OnlyContain(x => !x.Visited);
    }
}
