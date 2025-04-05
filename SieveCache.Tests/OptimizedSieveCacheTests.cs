using FluentAssertions;
using Xunit;

namespace SieveCache.Tests;
/*
public class OptimizedSieveCacheTests
{
    [Fact]
    public void Access_AddsItems_AndPreservesOrder()
    {
        var cache = new OptimizedSieveCache<int>(3);

        cache.Access(1);
        cache.Access(2);
        cache.Access(3);

        var contents = cache.GetCacheContents();

        contents.Select(x => x.Value).Should().ContainInOrder(3, 2, 1);
    }

    [Fact]
    public void Access_MarksExistingItemAsVisited()
    {
        var cache = new OptimizedSieveCache<int>(3);

        cache.Access(1);
        cache.Access(2);
        cache.Access(1); // again

        var contents = cache.GetCacheContents();

        contents.Should().ContainSingle(x => x.Value == 1 && x.Visited);
    }

    [Fact]
    public void Evict_RemovesUnvisitedItem_WhenCacheIsFull()
    {
        var cache = new OptimizedSieveCache<int>(2);

        cache.Access(1); // unvisited
        cache.Access(2); // unvisited
        cache.Access(3); // should evict 1

        var contents = cache.GetCacheContents();

        contents.Select(x => x.Value).Should().BeEquivalentTo(new[] { 3, 2 });
        contents.Should().NotContain(x => x.Value == 1);
    }

    [Fact]
    public void Evict_SkipsVisitedItems()
    {
        var cache = new OptimizedSieveCache<int>(2);

        cache.Access(1);
        cache.Access(2);
        cache.Access(1); // mark 1 visited

        cache.Access(3); // should evict 2

        var contents = cache.GetCacheContents();

        contents.Select(x => x.Value).Should().BeEquivalentTo(new[] { 3, 1 });
        contents.Should().NotContain(x => x.Value == 2);
    }

    [Fact]
    public void Evict_ResetsVisitedFlag()
    {
        var cache = new OptimizedSieveCache<int>(2);

        cache.Access(1);
        cache.AssertListIsConsistent();
        cache.Access(2);
        cache.AssertListIsConsistent();
        cache.Access(1); // visited
        cache.AssertListIsConsistent();
        cache.Access(2); // visited
        cache.AssertListIsConsistent();

        cache.Access(3); // triggers full eviction scan
        cache.AssertListIsConsistent();

        var contents = cache.GetCacheContents();

        contents.Where(x => x.Value != 3)
            .Should().OnlyContain(x => !x.Visited);
    }
}*/