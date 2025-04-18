﻿namespace SieveCache;

public interface ICache<TKey, TValue>
    where TKey : notnull
{
    TValue? Get(TKey key);
    void Put(TKey key, TValue value);
    bool Contains(TKey key);
    void Clear();
    int Count { get; }
}
