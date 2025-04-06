using BenchmarkDotNet.Attributes;

namespace SieveCache;

[MemoryDiagnoser]
[RankColumn]
[SimpleJob(launchCount: 1, warmupCount: 3, iterationCount: 5)]
public class CacheBenchmark
{
    [Params(100, 1000)] public int Capacity;
    [Params(1000, 10000)] public int AccessCount;

    private List<string> _randomData = null!;

    [GlobalSetup]
    public void Setup()
    {
        _randomData = GenerateZipfStrings(Capacity * 2, AccessCount);
    }

    [Benchmark]
    public async Task AsyncSieveCache_RandomAccess() => await AccessAll(new SieveCacheActor<string, string>(Capacity), _randomData);

    //[Benchmark] public void OptimizedSieveCache_RandomAccess() => AccessAll(new OptimizedSieveCache<string, string>(Capacity), _randomData);

    [Benchmark]
    public void SieveCache_RandomAccess() => AccessAll(new SieveCache<string, string>(Capacity), _randomData);

    //[Benchmark] public void LruCache_RandomAccess() => AccessAll(new LruCache<string, string>(Capacity), _randomData);

    private static async Task AccessAll(IAsyncCache<string, string> cache, List<string> data)
    {
        await Parallel.ForEachAsync(data, async (key, token) =>
        {
            if (!await cache.ContainsAsync(key))
            {
                await cache.PutAsync(key, key);
            }
            else
            {
                await cache.GetAsync(key);
            }
        });
    }

    private static void AccessAll(ICache<string, string> cache, List<string> data)
    {
        foreach (var key in data)
        {
            if (!cache.Contains(key))
            {
                cache.Put(key, key);
            }
            else
            {
                cache.Get(key);
            }
        }
    }

    private static List<string> GenerateZipfStrings(int uniqueKeyCount, int totalSamples, double exponent = 1.0)
    {
        var rand = new Random(42);

        var probabilities = Enumerable.Range(1, uniqueKeyCount)
            .Select(i => 1.0 / Math.Pow(i, exponent))
            .ToArray();

        var sum = probabilities.Sum();

        var cumulative = new double[uniqueKeyCount];
        double cumulativeSum = 0;

        for (var i = 0; i < uniqueKeyCount; i++)
        {
            cumulativeSum += probabilities[i] / sum;
            cumulative[i] = cumulativeSum;
        }

        var samples = new List<string>(totalSamples);

        for (var j = 0; j < totalSamples; j++)
        {
            var r = rand.NextDouble();
            var idx = Array.FindIndex(cumulative, p => p >= r);
            samples.Add(idx.ToString());
        }

        return samples;
    }
}