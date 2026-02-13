using System;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

/*
 * TOPIC: MemoryCache Policies and Expiration Logic.
 * * LEARNING GOAL: 
 * 1. Implement Absolute vs Sliding expiration.
 * 2. Handle Post-Eviction callbacks for monitoring.
 * 3. Use the "Cache-Aside" pattern to optimize data retrieval.
 */

public record UserProfile(int Id, string Name, string Role);

public class DatabaseService
{
    // Simulates a very slow Database call
    public async Task<UserProfile> GetUserFromDbAsync(int id)
    {
        Console.WriteLine($"[DB] Querying database for User {id}...");
        await Task.Delay(2000); // Heavy simulation
        return new UserProfile(id, "Senior Dev", "Architect");
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== MemoryCache Performance Lab ===\n");

        // 1. Setup Cache with a Size Limit (Senior practice to prevent OutOfMemory)
        var cacheOptions = new MemoryCacheOptions
        {
            SizeLimit = 100 // We can hold 100 "units" of data
        };
        var cache = new MemoryCache(cacheOptions);
        var db = new DatabaseService();

        int userId = 42;

        // 2. First Call (Cache Miss)
        await GetUserData(userId, cache, db);

        // 3. Second Call (Cache Hit - Should be instant)
        await GetUserData(userId, cache, db);

        // 4. Demonstrate Expiration
        Console.WriteLine("\nWaiting 6 seconds for Sliding Expiration (set to 5s)...");
        await Task.Delay(6000);

        // 5. Third Call (Cache Miss - Expired)
        await GetUserData(userId, cache, db);
    }

    static async Task GetUserData(int id, IMemoryCache cache, DatabaseService db)
    {
        string cacheKey = $"user_{id}";

        // The "Cache-Aside" Pattern
        if (!cache.TryGetValue(cacheKey, out UserProfile user))
        {
            // Cache MISS
            user = await db.GetUserFromDbAsync(id);

            // Configure Cache Entry
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetSize(1) // Each entry takes 1 unit of our 100-unit limit
                .SetSlidingExpiration(TimeSpan.FromSeconds(5)) // Reset timer on access
                .SetAbsoluteExpiration(TimeSpan.FromSeconds(15)) // Hard death after 15s
                .SetPriority(CacheItemPriority.High)
                .RegisterPostEvictionCallback(OnCacheEvicted); // Monitor why it leaves

            cache.Set(cacheKey, user, cacheEntryOptions);
            Console.WriteLine($"[APP] Data retrieved from DB and cached.");
        }
        else
        {
            // Cache HIT
            Console.WriteLine($"[APP] Data retrieved from CACHE: {user.Name}");
        }
    }

    // This is vital for debugging memory issues in production
    private static void OnCacheEvicted(object key, object value, EvictionReason reason, object state)
    {
        Console.WriteLine($" >>> [EVENT] Cache key '{key}' was removed. Reason: {reason}");
    }
}