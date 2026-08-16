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

## 🧩 Implementations

This repository ships several implementations of the same SIEVE algorithm, at different points on the simplicity / performance / concurrency spectrum:

| Cache | Thread-safe | Allocations | Best for |
|-------------------------------------|:-----------:|:-----------:|----------|
| `SieveCache<TKey, TValue>`          | No          | Node per entry | Single-threaded reference implementation |
| `OptimizedSieveCache<TKey, TValue>` | No          | Allocation-free (pooled struct nodes) | Single-threaded, allocation-sensitive |
| `ShardedSieveCache<TKey, TValue>`   | **Yes**     | Node per entry | Concurrent access from many threads |

## 🔒 Thread Safety

### Single-threaded caches
`SieveCache` and `OptimizedSieveCache` are **not thread-safe** by design. They manage a linked structure (`Next`, `Prev`, `Visited`, …) without locking, for maximum single-threaded performance. Using them concurrently can cause `NullReferenceException`, corruption of the internal node list, or incorrect eviction. If you need thread safety, either synchronize access externally or use `ShardedSieveCache`.

### ShardedSieveCache — thread-safe and scalable

`ShardedSieveCache` is a thread-safe SIEVE cache designed for concurrent workloads (a Redis-like "one cache, many threads" scenario — but in-process, with no network or command-queue overhead).

**Design**

- **Lock striping.** The keyspace is partitioned across independent shards, each with its own lock and SIEVE list, so writes to different shards never contend. This is the same technique used by Memcached and .NET's `ConcurrentDictionary`.
- **Lock-free reads.** SIEVE's core advantage is that a cache hit only needs to set a `Visited` flag — it never reorders the list. `Get` therefore takes no lock: it does a lock-free `ConcurrentDictionary` lookup and flips a volatile flag. Structural mutations (`Put`, eviction, `Clear`) take the per-shard lock.
- **Bounded size.** The shard count is a power of two, capped at the capacity and the core count, and capacity is distributed exactly across shards, so the total size never exceeds the requested capacity.

**Usage**

```csharp
var cache = new ShardedSieveCache<string, string>(capacity: 10_000);

// safe to call concurrently from any number of threads
cache.Put("a", "apple");
var value = cache.Get("a");
bool exists = cache.Contains("a");

// optionally pin the shard count (defaults to a power of two around the core count)
var tuned = new ShardedSieveCache<string, string>(capacity: 10_000, shardCount: 16);
```

**Scaling**

A single globally-locked SIEVE cache (here `SieveCacheCore`, guarded by one `ReaderWriterLockSlim`) doesn't just stop scaling — under contention it gets *slower* as threads are added (lock convoy). `ShardedSieveCache` distributes both the lock and the list, so it speeds up as threads are added, up to the core count.

BenchmarkDotNet, Intel Core i7-9700 (8 physical cores), .NET 9, 1,000,000 operations per invocation over a Zipf workload with ~5% writes. Mean time per invocation (lower is better); speedup is baseline ÷ sharded.

Capacity 1000:

| Threads | `SieveCacheCore` (global lock) | `ShardedSieveCache` | Speedup |
|--------:|-------------------------------:|--------------------:|--------:|
| 1       | 42.4 ms                        | 52.4 ms             | 0.81×   |
| 2       | 48.4 ms                        | 29.7 ms             | 1.63×   |
| 4       | 49.6 ms                        | 17.3 ms             | 2.87×   |
| 8       | 47.6 ms                        | 14.3 ms             | **3.33×** |

Capacity 100 shows the same shape (58.7 ms → 20.6 ms at 8 threads, **2.85×**). At one thread the sharding indirection costs ~15–25%; from two threads up the sharded cache pulls ahead and roughly halves its time each time the thread count doubles, while the globally-locked cache flatlines around 20 Mops/s. The trade-off is ~1.2–1.3× more allocation (extra `ConcurrentDictionary` lock stripes per shard).

Reproduce with `ParallelCacheBenchmark` in the `SieveCache.Benchmark` project (`dotnet run -c Release --project SieveCache.Benchmark`).
