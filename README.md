# SIEVE Cache for .NET
A Simple, Efficient, and Scalable Eviction Algorithm

This project is a .NET implementation of SIEVE — a surprisingly effective yet simple cache eviction algorithm. It is designed to outperform LRU and other complex algorithms in real-world scenarios with minimal implementation overhead.

## 🔍 About SIEVE

SIEVE stands for Simpler than LRU: an Efficient Turn-Key Eviction Algorithm. It combines lazy promotion with quick demotion, making it both efficient and easy to implement.

## Why SIEVE?

- 📉 Up to 63% lower miss ratio than ARC
- ⚡ Twice the throughput of optimized LRU at 16 threads
- 🔁 Lock-free hits for better concurrency
- 🧼 <20 lines of code change in most cache libraries
- 🔧 Can be used as a cache primitive to build more advanced eviction policies

Paper from Yazhuo Zhang, Juncheng Yang, Yao Yue, Ymir Vigfusson, K. V. Rashmi https://junchengyang.com/publication/nsdi24-SIEVE.pdf

https://cachemon.github.io/SIEVE-website/

## Interface

```csharp
public interface ICache<TKey, TValue>
    where TKey : notnull
{
    TValue? Get(TKey key);
    void Put(TKey key, TValue value);
    bool Contains(TKey key);
    void Clear();
    int Count { get; }
}
```

## Usage example

```csharp
var cache = new SieveCache<string, string>(capacity: 3);

cache.Put("a", "apple");
cache.Put("b", "banana");
cache.Put("c", "coconut");

var result = cache.Get("a"); // "apple" – marks it as visited

cache.Put("d", "dragonfruit"); // evicts the first unvisited item

bool exists = cache.Contains("b"); // true or false depending on eviction
int currentCount = cache.Count;
```

## Benchmark

| Method                  | Capacity | AccessCount | Mean      | Error    | StdDev   | Rank | Gen0    | Gen1    | Allocated |
|------------------------ |--------- |------------ |----------:|---------:|---------:|-----:|--------:|--------:|----------:|
| SieveCache_RandomAccess | 100      | 1000        |  46.88 us | 0.925 us | 1.467 us |    1 |  5.9814 |  0.4883 |  36.91 KB |
| LruCache_RandomAccess   | 100      | 1000        |  63.42 us | 0.637 us | 0.596 us |    2 |  3.4180 |  0.1221 |  21.23 KB |
| SieveCache_RandomAccess | 100      | 10000       | 431.11 us | 4.972 us | 4.650 us |    4 | 26.3672 |  1.9531 | 162.54 KB |
| LruCache_RandomAccess   | 100      | 10000       | 640.76 us | 4.459 us | 3.723 us |    6 | 15.6250 |       - | 101.46 KB |
| SieveCache_RandomAccess | 1000     | 1000        |  65.64 us | 1.302 us | 1.948 us |    2 | 18.0664 |  3.6621 | 111.25 KB |
| LruCache_RandomAccess   | 1000     | 1000        |  73.17 us | 0.992 us | 0.928 us |    3 |  7.0801 |  0.8545 |  43.68 KB |
| SieveCache_RandomAccess | 1000     | 10000       | 482.33 us | 5.456 us | 5.104 us |    5 | 50.2930 | 22.9492 | 308.93 KB |
| LruCache_RandomAccess   | 1000     | 10000       | 710.96 us | 4.457 us | 4.169 us |    7 | 30.2734 | 10.7422 | 190.56 KB |
