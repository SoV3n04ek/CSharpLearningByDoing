# 🚀 MemoryCache Mastery: High-Load Simulation

## 📌 Topic Overview

In high-performance .NET applications, **Caching** is the primary strategy for reducing latency and database pressure. While `MemoryCache` seems simple, improper configuration leads to **Memory Leaks**, **Stale Data**, and **Cache Stampedes**. This lab focuses on mastering **In-Memory Caching** using the `Microsoft.Extensions.Caching.Memory` library in a pure Console Environment.

---

## 🎯 Learning Goals

1. **Cache-Aside Pattern:** Implementing the industry-standard logic: *Check Cache -> Fetch from DB -> Update Cache*.
2. **Expiration Policies:** Distinguishing between **Absolute** (hard limit) and **Sliding** (activity-based) expiration.
3. **Resource Governance:** Using `SizeLimit` and `CacheItemPriority` to prevent `OutOfMemoryException`.
4. **Observability:** Using `PostEvictionCallbacks` to audit why data is leaving the memory.
5. **Race Condition Awareness:** Understanding why multiple threads might accidentally hit the database simultaneously (Cache Stampede).

---

## 🛠 Detailed Technical Task

### 1. The Infrastructure

* **The Mock Database:** Implement a `DatabaseService` that simulates a slow I/O operation (2 seconds) to retrieve a `UserProfile`.
* **The Cache Container:** Initialize a `MemoryCache` instance with a global `SizeLimit`.

### 2. The Cache-Aside Implementation

Create a method `GetUserDataAsync(int userId)` that:

1. Checks the cache using a unique string key (e.g., `user_{id}`).
2. If data exists (Cache **Hit**), returns it immediately.
3. If data is missing (Cache **Miss**), calls the `DatabaseService`.
4. Stores the result in the cache with specific **Entry Options**.

### 3. Expiration Logic Requirements

To simulate a real-world scenario, apply both:

* **Sliding Expiration (5 seconds):** If the user is inactive for 5 seconds, remove them.
* **Absolute Expiration (15 seconds):** Regardless of activity, refresh the data every 15 seconds to ensure it isn't stale.

### 4. Eviction Monitoring

Register a callback that prints a detailed message to the Console whenever an item is evicted.

* *Possible reasons to track:* `Expired`, `TokenExpired`, `Capacity` (Cache was full), `Removed` (Manual), or `Replaced`.

---

## 💻 The Solution

### Project Setup

Add the required NuGet package:

```bash
dotnet add package Microsoft.Extensions.DependencyInjection
dotnet add package Microsoft.Extensions.Caching.Memory

```

---

## 🎓 Senior Interview Q&A

**Q: What is a "Cache Stampede" and how do you prevent it?**
**A:** A Cache Stampede occurs when a high-traffic item expires, and 100 threads simultaneously see a "Cache Miss." All 100 threads then hit the database at once. To prevent this, we use **Locking (SemaphoreSlim)** or the `GetOrCreateAsync` pattern to ensure only the first thread queries the DB while others wait for the result.

**Q: Why use `SetSize` on cache entries?**
**A:** `MemoryCache` doesn't automatically know how much RAM an object takes. By setting a size (e.g., `1`), we tell the cache how to calculate its capacity. If the `SizeLimit` is reached, the cache will automatically evict items with `CacheItemPriority.Low` to make room.

**Q: When would you use Redis instead of MemoryCache?**
**A:** Use `MemoryCache` for single-node applications where speed is the absolute priority. Use **Redis (Distributed Cache)** if you have multiple server instances (Load Balancing) and want them to share the same cache, or if the cache needs to survive a server restart.
