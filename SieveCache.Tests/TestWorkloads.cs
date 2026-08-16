namespace SieveCache.Tests;

/// <summary>
/// Deterministic workload generators shared across test suites.
/// Mirrors the Zipf generator used in SieveCache.Benchmark so eviction-quality
/// tests exercise the same skewed access pattern as the benchmarks.
/// </summary>
internal static class TestWorkloads
{
    public static List<string> GenerateZipfStrings(int uniqueKeyCount, int totalSamples, double exponent = 1.0, int seed = 42)
    {
        var rand = new Random(seed);

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
