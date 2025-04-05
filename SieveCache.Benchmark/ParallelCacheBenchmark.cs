using BenchmarkDotNet.Attributes;

namespace SieveCache;

[MemoryDiagnoser]
[RankColumn]
public class ParallelCacheBenchmark
{
    [Params(100, 1000)] public int Capacity;
    [Params(1000, 10000)] public int AccessCount;
    [Params(4, 8)] public int ThreadCount;

    private List<string> _randomData = null!;

    [GlobalSetup]
    public void Setup()
    {
        _randomData = GenerateZipfStrings(Capacity * 2, AccessCount);
    }

    [Benchmark]
    public void SieveCache_ParallelAccess()
    {
        var cache = new SieveCache<string, string>(Capacity);

        Parallel.ForEach(
            _randomData,
            new ParallelOptions { MaxDegreeOfParallelism = ThreadCount },
            key =>
            {
                if (!cache.Contains(key))
                {
                    cache.Put(key, key);
                }
                else
                {
                    _ = cache.Get(key);
                }
            });
    }

    [Benchmark]
    public void LruCache_ParallelAccess()
    {
        var cache = new LruCache<string, string>(Capacity);

        Parallel.ForEach(
            _randomData,
            new ParallelOptions { MaxDegreeOfParallelism = ThreadCount },
            key =>
            {
                if (!cache.Contains(key))
                {
                    cache.Put(key, key);
                }
                else
                {
                    _ = cache.Get(key);
                }
            });
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