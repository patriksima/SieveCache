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
