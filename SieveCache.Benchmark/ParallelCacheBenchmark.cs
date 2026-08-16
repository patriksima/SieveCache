using BenchmarkDotNet.Attributes;

namespace SieveCache;

[MemoryDiagnoser]
[RankColumn]
[ThreadingDiagnoser]
public class ParallelCacheBenchmark
{
    [Params(100, 1_000)] public int Capacity;
    [Params(99, 1_000_000)] public int AccessCount;
    [ParamsSource(nameof(ThreadCounts))] public int Threads;

    public static IEnumerable<int> ThreadCounts => GetThreadCounts();
    private List<string> _randomData = null!;

    private static IEnumerable<int> GetThreadCounts()
    {
        var coreCount = Environment.ProcessorCount;
        return Enumerable.Range(1, coreCount).Where(IsPowerOfTwo);
    }

    private static bool IsPowerOfTwo(int x) => (x & (x - 1)) == 0;

    [GlobalSetup]
    public void Setup()
    {
        _randomData = GenerateZipfStrings(Capacity * 2, AccessCount);
    }

    [Benchmark]
    public void SieveCache_ParallelAccess()
    {
        var cache = new SieveCacheCore(Capacity);

        Parallel.ForEach(
            Enumerable.Range(0, _randomData.Count),
            new ParallelOptions { MaxDegreeOfParallelism = Threads },
            i =>
            {
                var key = _randomData[i];
                var doesExist = cache.Contains(key);
                // read/write ratio
                if (i % 20 == 0) // každá 20. operace = zápis (5 %)
                {
                    cache.Put(key, key);
                }
                else
                {
                    _ = cache.Get(key);
                }
            });

        _ = cache.Count;
        cache.Clear();
        cache.ResetStats();
    }

    /*[Benchmark]
    public void LruCache_ParallelAccess()
    {
        var cache = new LruCache<string, string>(Capacity);

        Parallel.ForEach(
            Enumerable.Range(0, _randomData.Count),
            new ParallelOptions { MaxDegreeOfParallelism = Threads },
            i =>
            {
                var key = _randomData[i];
                var doesExist = cache.Contains(key);

                if (i % 5 == 0)
                {
                    cache.Put(key, key);
                }
                else
                {
                    _ = cache.Get(key);
                }
            });
    }*/

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