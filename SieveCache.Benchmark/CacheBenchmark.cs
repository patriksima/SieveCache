using BenchmarkDotNet.Attributes;

namespace SieveCache;

[MemoryDiagnoser]
public class CacheBenchmark
{
    private UnsafeSieveCache<int> _unsafeSieve = null!;
    private SieveCache<int> _sieve = null!;
    private LruCache<int> _lru = null!;
    private FifoCache<int> _fifo = null!;

    private List<int> _data = null!;

    [Params(100, 1000)] public int Capacity;

    [Params(1000, 10000)] public int AccessCount;

    [GlobalSetup]
    public void Setup()
    {
        _unsafeSieve = new UnsafeSieveCache<int>(Capacity);
        _sieve = new SieveCache<int>(Capacity);
        _lru = new LruCache<int>(Capacity);
        _fifo = new FifoCache<int>(Capacity);

        var rand = new Random(42);
        _data = Enumerable.Range(0, AccessCount).Select(_ => rand.Next(0, Capacity * 2)).ToList();
    }

    [Benchmark]
    public void UnsafeSieveCacheAccess()
    {
        foreach (var x in _data)
        {
            _unsafeSieve.Access(x);
        }
    }

    [Benchmark]
    public void SieveCacheAccess()
    {
        foreach (var x in _data)
        {
            _sieve.Access(x);
        }
    }

    [Benchmark]
    public void LruCacheAccess()
    {
        foreach (var x in _data)
        {
            _lru.Access(x);
        }
    }

    [Benchmark]
    public void FifoCacheAccess()
    {
        foreach (var x in _data)
        {
            _fifo.Access(x);
        }
    }
}