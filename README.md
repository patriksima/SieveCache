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
