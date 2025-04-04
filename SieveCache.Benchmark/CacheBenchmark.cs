using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

namespace SieveCache;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class CacheBenchmark
{
    [Params(100, 1000)] public int Capacity;
    [Params(1000, 10000)] public int AccessCount;

    private List<int> _randomData = null!;

    [GlobalSetup]
    public void Setup()
    {
        var rand = new Random(42);

        _randomData = Enumerable.Range(0, AccessCount)
            .Select(_ => rand.Next(0, Capacity * 2))
            .ToList();
    }

    // ---------- Performance Benchmarks ----------

    [Benchmark]
    public void OptimizedSieveCache_RandomAccess() => AccessAll(new OptimizedSieveCache<int>(Capacity), _randomData);

    [Benchmark]
    public void SieveCache_RandomAccess() => AccessAll(new SieveCache<int>(Capacity), _randomData);

    [Benchmark]
    public void LruCache_RandomAccess() => AccessAll(new LruCache<int>(Capacity), _randomData);

    [Benchmark]
    public void FifoCache_RandomAccess() => AccessAll(new FifoCache<int>(Capacity), _randomData);

    // ---------- Helpers ----------

    private static void AccessAll(ICache<int> cache, List<int> data)
    {
        foreach (var x in data)
        {
            cache.Access(x);
        }
    }
}