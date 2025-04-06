using System.Collections.Concurrent;
using FluentAssertions;
using Xunit;

namespace SieveCache.Tests;

public class SieveCacheTests
{
    [Fact]
    public void Access_AddsItems_AndPreservesOrder()
    {
        var cache = new SieveCache<int, int>(3);

        cache.Put(1, 1);
        cache.Put(2, 2);
        cache.Put(3, 3);

        var contents = cache.GetCacheContents();

        contents.Select(x => x.Value).Should().ContainInOrder(3, 2, 1);
    }

    [Fact]
    public void Access_MarksExistingItemAsVisited()
    {
        var cache = new SieveCache<int, int>(3);

        cache.Put(1, 1);
        cache.Put(2, 2);
        cache.Put(1, 1); // again

        var contents = cache.GetCacheContents();

        contents.Should().ContainSingle(x => x.Value == 1 && x.Visited);
    }

    [Fact]
    public void Evict_RemovesUnvisitedItem_WhenCacheIsFull()
    {
        var cache = new SieveCache<int, int>(2);

        cache.Put(1, 1); // unvisited
        cache.Put(2, 2); // unvisited
        cache.Put(3, 3); // should evict 1

        var contents = cache.GetCacheContents();

        contents.Select(x => x.Value).Should().BeEquivalentTo(new[] { 3, 2 });
        contents.Should().NotContain(x => x.Value == 1);
    }

    [Fact]
    public void Evict_SkipsVisitedItems()
    {
        var cache = new SieveCache<int, int>(2);

        cache.Put(1, 1);
        cache.Put(2, 2);
        cache.Put(1, 1); // mark 1 visited

        cache.Put(3, 3); // should evict 2

        var contents = cache.GetCacheContents();

        contents.Select(x => x.Value).Should().BeEquivalentTo(new[] { 3, 1 });
        contents.Should().NotContain(x => x.Value == 2);
    }

    [Fact]
    public void Evict_ResetsVisitedFlag()
    {
        var cache = new SieveCache<int, int>(2);

        cache.Put(1, 1);
        cache.Put(2, 2);
        cache.Put(1, 1); // visited
        cache.Put(2, 2); // visited

        cache.Put(3, 3); // triggers full eviction scan

        var contents = cache.GetCacheContents();

        contents.Where(x => x.Value != 3)
            .Should().OnlyContain(x => !x.Visited);
    }

    [Fact]
    public void Get_ReturnsCorrectValue_AndMarksAsVisited()
    {
        var cache = new SieveCache<string, string>(2);
        cache.Put("a", "Apple");
        cache.Put("b", "Banana");

        var result = cache.Get("a");

        result.Should().Be("Apple");

        var contents = cache.GetCacheContents();
        contents.Should().Contain(x => x.Key == "a" && x.Visited);
    }

    [Fact]
    public void Put_UpdatesValue_IfKeyExists()
    {
        var cache = new SieveCache<string, string>(2);
        cache.Put("a", "Apple");
        cache.Put("a", "Avocado");

        var result = cache.Get("a");

        result.Should().Be("Avocado");
    }

    [Fact]
    public void Get_ReturnsNull_WhenKeyDoesNotExist()
    {
        var cache = new SieveCache<int, string>(2);

        var result = cache.Get(99);

        result.Should().BeNull();
    }

    [Fact]
    public void Contains_ReturnsTrue_IfKeyExists()
    {
        var cache = new SieveCache<int, string>(2);
        cache.Put(1, "One");

        cache.Contains(1).Should().BeTrue();
    }

    [Fact]
    public void Contains_ReturnsFalse_IfKeyDoesNotExist()
    {
        var cache = new SieveCache<int, string>(2);

        cache.Contains(42).Should().BeFalse();
    }

    [Fact]
    public void Count_ReturnsCorrectNumberOfItems()
    {
        var cache = new SieveCache<int, int>(3);

        cache.Put(1, 1);
        cache.Put(2, 2);

        cache.Count.Should().Be(2);
    }

    [Fact]
    public void Clear_RemovesAllItems()
    {
        var cache = new SieveCache<int, int>(3);

        cache.Put(1, 1);
        cache.Put(2, 2);
        cache.Clear();

        cache.Count.Should().Be(0);
        cache.Contains(1).Should().BeFalse();
        cache.Contains(2).Should().BeFalse();
    }

    [Fact(Skip = "Not implemented yet")]
    public void ThreadSafety_ShouldNotThrowOrCorruptStateUnderHeavyParallelLoad()
    {
        var cache = new SieveCache<int, int>(500);

        var exceptions = new ConcurrentBag<Exception>();

        Parallel.For(0, 100_000, i =>
        {
            try
            {
                var key = i % 1000;
                cache.Put(key, key);
                cache.Get(key);
                cache.Contains(key);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        exceptions.Should().BeEmpty();
    }
}