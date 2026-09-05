# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

.NET 9 implementation of the SIEVE cache eviction algorithm (https://cachemon.github.io/SIEVE-website/), benchmarked against LRU/FIFO baselines. All three projects use the root namespace `SieveCache` regardless of project name.

## Commands

```powershell
dotnet build                                                  # build solution
dotnet test SieveCache.Tests/SieveCache.Tests.csproj          # run all tests
dotnet test SieveCache.Tests/SieveCache.Tests.csproj --filter "FullyQualifiedName~SieveCacheCoreTests"   # one test class
dotnet test SieveCache.Tests/SieveCache.Tests.csproj --filter "DisplayName~Put_should"                   # tests by name
dotnet run --configuration Release --project SieveCache.Benchmark/SieveCache.Benchmark.csproj            # benchmarks
```

Tests are xUnit + FluentAssertions. CI (`.github/workflows/dotnet-tests.yml`) runs the test project in Release on push/PR to master. Benchmarks run manually via workflow_dispatch (`benchmark.yml`). Publishing a GitHub release (tag `vX.Y.Z`) triggers `release.yml`, which builds, tests, packs with the version taken from the tag and attaches the `.nupkg`/`.snupkg` to the release; pushing to nuget.org is a commented-out step waiting for a `NUGET_API_KEY` secret. `<Version>` in `SieveCache.Cache.csproj` is only the local fallback — bump it together with the tag.

Benchmarks must be run in Release; BenchmarkDotNet results land in `BenchmarkDotNet.Artifacts/`. `SieveCache.Benchmark/Program.cs` hardcodes which benchmark class runs (`BenchmarkRunner.Run<T>()`) — edit it to switch between `CacheBenchmark` (sequential) and `ParallelCacheBenchmark` (multi-threaded, Zipf workload). Individual implementations are enabled/disabled by commenting `[Benchmark]` methods in and out; this is the established workflow here.

## Architecture

`SieveCache.Cache` is the library, packed as NuGet package `PatrikSima.SieveCache` (plain `SieveCache` is taken on nuget.org by someone else). `SieveCache.Demo` is a tiny console demo referencing it. It contains several parallel implementations of the same algorithm, at different points on the simplicity/performance/concurrency spectrum:

- **`SieveCache<TKey,TValue>`** (`SieveCache.cs`) — canonical reference implementation: `Dictionary` + doubly-linked `Node<TKey,TValue>` list (`Node.cs`). Not thread-safe by design (see README's Thread Safety section — locking is deliberately omitted for performance).
- **`OptimizedSieveCache<TKey,TValue>`** — allocation-free variant: struct nodes in an `ArrayPool` array, index-based (int) linked list, `Unsafe`/`MemoryMarshal` ref access. `IDisposable` (returns the pooled array). Also not thread-safe.
- **`SieveCacheCore`** (string-only) — thread-safe single-list variant: `ConcurrentDictionary` for lock-free `Get`/`Contains` (hits only touch the `Visited` flag), `ReaderWriterLockSlim` write lock around `Put`/`Clear` list mutations. Tracks hit/miss stats via `Interlocked`. A single global write lock means writes stop scaling past ~2 threads.
- **`ShardedSieveCache<TKey,TValue>`** — thread-safe *and scalable* variant: partitions the keyspace across power-of-two shards (lock striping), each an independent SIEVE cache (`ConcurrentDictionary` + per-shard `lock`). Lock-free `Get`; per-shard lock only for structural mutation. Shard count capped at `min(capacity, ProcessorCount)` and capacity distributed exactly, so `Count <= capacity`. This is the recommended cache for concurrent use; it scales with thread count (~3× `SieveCacheCore` at 8 threads on an 8-core box) whereas the single-lock caches degrade under contention.
- **`SieveCacheActor<TKey,TValue>`** — async facade implementing `IAsyncCache`: serializes all operations through a single-reader `Channel` onto one processing task wrapping a plain `SieveCache`. Known to be slow (commit "async cache (very slow)"); it exists for comparison.
- **`LruCache`**, **`FifoCache`** — baselines for benchmarks.

Sync implementations share `ICache<TKey,TValue>`; async ones `IAsyncCache<TKey,TValue>`.

The SIEVE algorithm itself: new entries go to the list head; `Get` only sets a `Visited` flag (lazy promotion); eviction walks a persistent `_hand` pointer from tail toward head, clearing `Visited` flags until it finds an unvisited node to remove. When changing one implementation's algorithm logic, check whether the same change applies to the other implementations — they intentionally mirror each other.

Tests inspect internal list order via `GetCacheContents()`, an `internal` test-only hook exposed through `InternalsVisibleTo("SieveCache.Tests")`.

Test structure (see `SieveCache.Tests/`):
- `CacheContractTests` — black-box behaviour for any `ICache<string,string>` (get/put/update/contains/clear, `Count <= capacity`, capacity-1 edge). `SingleListSieveContractTests` adds SIEVE list-order/eviction/visited assertions. Each concrete cache is a thin subclass supplying a `Create(capacity)` factory (and `GetContents` for single-list caches). A new implementation adds a subclass, not a copied test file — sharded-style caches derive from `CacheContractTests` only (no single global order).
- `ConcurrentCacheInvariantTests` — post-load structural invariants (size ≤ capacity, list/lookup agree, unique keys, `value == key`) plus a **barrier-synchronized** race test that deterministically hammers the check-then-insert window (plain `Parallel.For` misses it due to thread ramp-up).
- `EvictionQualityTests` — hit ratio on a Zipf workload must stay within ±10% of the reference `SieveCache`; the key guard that a thread-safe/sharded rewrite hasn't degraded eviction quality.
- `TestWorkloads.GenerateZipfStrings` — shared deterministic (seed 42) Zipf generator mirroring the benchmark.
