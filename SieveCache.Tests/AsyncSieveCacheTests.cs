using System.Collections.Concurrent;
using FluentAssertions;
using Xunit;

namespace SieveCache.Tests;

public class AsyncSieveCacheTests
{
    [Fact]
    public async Task Access_AddsItems_AndPreservesOrder()
    {
        var cache = new SieveCacheActor<int, int>(3);

        await cache.PutAsync(1, 1);
        await cache.PutAsync(2, 2);
        await cache.PutAsync(3, 3);

        var contents = cache.GetCacheContents();

        contents.Select(x => x.Value).Should().Contain([1, 2, 3]);
    }

    [Fact]
    public async Task Access_MarksExistingItemAsVisited()
    {
        var cache = new SieveCacheActor<int, int>(3);

        await cache.PutAsync(1, 1);
        await cache.PutAsync(2, 2);
        await cache.PutAsync(1, 1); // again

        var contents = cache.GetCacheContents();

        contents.Should().ContainSingle(x => x.Value == 1 && x.Visited);
    }

    [Fact]
    public async Task Evict_RemovesUnvisitedItem_WhenCacheIsFull()
    {
        var cache = new SieveCacheActor<int, int>(2);

        await cache.PutAsync(1, 1); // unvisited
        await cache.PutAsync(2, 2); // unvisited
        await cache.PutAsync(3, 3); // should evict 1

        var contents = cache.GetCacheContents();

        contents.Select(x => x.Value).Should().BeEquivalentTo(new[] { 3, 2 });
        contents.Should().NotContain(x => x.Value == 1);
    }

    [Fact]
    public async Task Evict_SkipsVisitedItems()
    {
        var cache = new SieveCacheActor<int, int>(2);

        await cache.PutAsync(1, 1);
        await cache.PutAsync(2, 2);
        await cache.PutAsync(1, 1); // mark 1 visited

        await cache.PutAsync(3, 3); // should evict 2

        var contents = cache.GetCacheContents();

        contents.Select(x => x.Value).Should().BeEquivalentTo(new[] { 3, 1 });
        contents.Should().NotContain(x => x.Value == 2);
    }

    [Fact]
    public async Task Evict_ResetsVisitedFlag()
    {
        var cache = new SieveCacheActor<int, int>(2);

        await cache.PutAsync(1, 1);
        await cache.PutAsync(2, 2);
        await cache.PutAsync(1, 1); // visited
        await cache.PutAsync(2, 2); // visited

        await cache.PutAsync(3, 3); // triggers full eviction scan

        var contents = cache.GetCacheContents();

        contents.Where(x => x.Value != 3)
            .Should().OnlyContain(x => !x.Visited);
    }

    [Fact]
    public async Task Get_ReturnsCorrectValue_AndMarksAsVisited()
    {
        var cache = new SieveCacheActor<string, string>(2);
        await cache.PutAsync("a", "Apple");
        await cache.PutAsync("b", "Banana");

        var result = await cache.GetAsync("a");

        result.Should().Be("Apple");

        var contents = cache.GetCacheContents();
        contents.Should().Contain(x => x.Key == "a" && x.Visited);
    }

    [Fact]
    public async Task Put_UpdatesValue_IfKeyExists()
    {
        var cache = new SieveCacheActor<string, string>(2);
        await cache.PutAsync("a", "Apple");
        await cache.PutAsync("a", "Avocado");

        var result = await cache.GetAsync("a");

        result.Should().Be("Avocado");
    }

    [Fact]
    public async Task Get_ReturnsNull_WhenKeyDoesNotExist()
    {
        var cache = new SieveCacheActor<int, string>(2);

        var result = await cache.GetAsync(99);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Contains_ReturnsTrue_IfKeyExists()
    {
        var cache = new SieveCacheActor<int, string>(2);
        await cache.PutAsync(1, "One");

        (await cache.ContainsAsync(1)).Should().BeTrue();
    }

    [Fact]
    public async Task Contains_ReturnsFalse_IfKeyDoesNotExist()
    {
        var cache = new SieveCacheActor<int, string>(2);

        (await cache.ContainsAsync(42)).Should().BeFalse();
    }

    [Fact]
    public async Task Count_ReturnsCorrectNumberOfItems()
    {
        var cache = new SieveCacheActor<int, int>(3);

        await cache.PutAsync(1, 1);
        await cache.PutAsync(2, 2);

        (await cache.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Clear_RemovesAllItems()
    {
        var cache = new SieveCacheActor<int, int>(3);

        await cache.PutAsync(1, 1);
        await cache.PutAsync(2, 2);
        await cache.ClearAsync();

        (await cache.CountAsync()).Should().Be(0);
        (await cache.ContainsAsync(1)).Should().BeFalse();
        (await cache.ContainsAsync(2)).Should().BeFalse();
    }
    
    [Fact]
    public void ThreadSafety_ShouldNotThrowOrCorruptStateUnderHeavyParallelLoad()
    {
        var cache = new SieveCacheActor<int, int>(500);

        var exceptions = new ConcurrentBag<Exception>();

        Parallel.ForAsync(0, 100_000, async (i, token) => 
        {
            try
            {
                var key = i % 1000;
                await cache.PutAsync(key, key);
                await cache.GetAsync(key);
                await cache.ContainsAsync(key);
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        });

        exceptions.Should().BeEmpty();
    }
}