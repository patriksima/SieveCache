using FluentAssertions;
using Xunit;

namespace SieveCache.Tests;

public class LruCacheTests
{
    [Fact]
    public void Put_AddsItems_AndPreservesLruOrder()
    {
        var cache = new LruCache<int, string>(3);

        cache.Put(1, "One");
        cache.Put(2, "Two");
        cache.Put(3, "Three");

        var keys = cache.GetCacheKeysInOrder();

        keys.Should().ContainInOrder(3, 2, 1);
    }

    [Fact]
    public void Get_MovesAccessedItemToFront()
    {
        var cache = new LruCache<int, string>(3);

        cache.Put(1, "One");
        cache.Put(2, "Two");
        cache.Put(3, "Three");

        cache.Get(1); // Access 1, move to front

        var keys = cache.GetCacheKeysInOrder();

        keys.Should().ContainInOrder(1, 3, 2);
    }

    [Fact]
    public void Put_EvictsLeastRecentlyUsedItem()
    {
        var cache = new LruCache<int, string>(2);

        cache.Put(1, "One");
        cache.Put(2, "Two");
        cache.Put(3, "Three"); // should evict 1

        cache.Contains(1).Should().BeFalse();
        cache.Contains(2).Should().BeTrue();
        cache.Contains(3).Should().BeTrue();
    }

    [Fact]
    public void Put_UpdatesExistingValue_AndMovesToFront()
    {
        var cache = new LruCache<int, string>(2);

        cache.Put(1, "One");
        cache.Put(2, "Two");

        cache.Put(1, "Uno"); // update value

        var value = cache.Get(1);
        value.Should().Be("Uno");

        var keys = cache.GetCacheKeysInOrder();
        keys.First().Should().Be(1);
    }

    [Fact]
    public void Get_ReturnsNull_WhenKeyDoesNotExist()
    {
        var cache = new LruCache<int, string>(2);

        var result = cache.Get(42);

        result.Should().BeNull();
    }

    [Fact]
    public void Contains_ReturnsTrue_IfKeyExists()
    {
        var cache = new LruCache<string, string>(2);

        cache.Put("x", "value");

        cache.Contains("x").Should().BeTrue();
    }

    [Fact]
    public void Contains_ReturnsFalse_IfKeyDoesNotExist()
    {
        var cache = new LruCache<int, int>(2);

        cache.Contains(999).Should().BeFalse();
    }

    [Fact]
    public void Clear_RemovesAllItems()
    {
        var cache = new LruCache<int, int>(3);

        cache.Put(1, 1);
        cache.Put(2, 2);
        cache.Clear();

        cache.Count.Should().Be(0);
        cache.Contains(1).Should().BeFalse();
    }

    [Fact]
    public void Count_ReturnsCorrectNumberOfItems()
    {
        var cache = new LruCache<int, int>(3);

        cache.Put(1, 1);
        cache.Put(2, 2);

        cache.Count.Should().Be(2);
    }

    [Fact(Skip = "Not implemented yet")]
    public void ThreadSafety_LruCache_MultipleThreadsWorkSafely()
    {
        var cache = new LruCache<int, int>(capacity: 200);

        Parallel.For(0, 10000, i =>
        {
            var key = i % 500;
            cache.Put(key, i);
            _ = cache.Get(key);
            cache.Contains(key);
        });
    }
}