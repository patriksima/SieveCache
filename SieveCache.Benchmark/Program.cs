using BenchmarkDotNet.Running;
using SieveCache;

BenchmarkRunner.Run<CacheBenchmark>();
BenchmarkRunner.Run<ParallelCacheBenchmark>();