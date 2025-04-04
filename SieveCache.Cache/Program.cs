using SieveCache;

var cache = new UnsafeSieveCache<string>(3);
cache.Access("A");
cache.Access("B");
cache.Access("C");
cache.Access("D");
//cache.ShowCache();
foreach (var (value, visited) in cache.GetCacheContents())
{
    Console.WriteLine($"{value}:{visited}");
}
