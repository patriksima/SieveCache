using SieveCache;


var cache = new SieveCache<string, string>(capacity: 3);

cache.Put("a", "apple");
cache.Put("b", "banana");
cache.Put("c", "coconut");

var result = cache.Get("a"); // apple - marks it as visited
Console.WriteLine(result);

cache.Put("d", "dragonfruit"); // evicts the first unvisited item

var exists = cache.Contains("b");
var currentCount = cache.Count;

Console.WriteLine($"Does the key 'b'  exist? {exists}");
Console.WriteLine($"{currentCount} cache entries found");

foreach (var (key, value, visited) in cache.GetCacheContents())
{
    Console.WriteLine($"{key}: {value}: {visited}");
}
