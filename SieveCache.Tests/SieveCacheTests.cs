using FluentAssertions;
using Xunit;

namespace SieveCache.Tests;

public class SieveCacheTests
{
    [Fact]
    public void Access_AddsItems_AndPreservesOrder()
    {
        var cache = new SieveCache<int>(3);

        cache.Access(1);
        cache.Access(2);
        cache.Access(3);

        var contents = cache.GetCacheContents();

        contents.Select(x => x.Value).Should().ContainInOrder(3, 2, 1);
    }

    [Fact]
    public void Access_MarksExistingItemAsVisited()
    {
        var cache = new SieveCache<int>(3);

        cache.Access(1);
        cache.Access(2);
        cache.Access(1); // again

        var contents = cache.GetCacheContents();

        contents.Should().ContainSingle(x => x.Value == 1 && x.Visited);
    }

    [Fact]
    public void Evict_RemovesUnvisitedItem_WhenCacheIsFull()
    {
        var cache = new SieveCache<int>(2);

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
        var cache = new SieveCache<int>(2);

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
        var cache = new SieveCache<int>(2);

        cache.Access(1);
        cache.Access(2);
        cache.Access(1); // visited
        cache.Access(2); // visited

        cache.Access(3); // triggers full eviction scan

        var contents = cache.GetCacheContents();

        contents.Where(x => x.Value != 3)
            .Should().OnlyContain(x => !x.Visited);
    }
}