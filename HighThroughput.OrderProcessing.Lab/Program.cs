using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Runtime.Intrinsics.Arm;
using System.Security.Claims;
using System.Threading;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FlashSale.Processor.Benchmarks;

public record RawOrder(int OrderId, string Category, string UserTransactionId);

public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("Initializing flash sale processor");

        // Setup mock data: 10,000 orders with 20% of duplicates
        var rawOrders = GenerateMockOrders(10000);

        // State storage (thread-safe)
        // Use byte as a dummy value to save memory
        // Only care about the Key(orderId)
        var seenOrders = new ConcurrentDictionary<int, byte>();
        var categoryCounts = new ConcurrentDictionary<string, int>();

        int rejectedCount = 0;
        int peakThreads = 0;

        var sw = Stopwatch.StartNew();

        // engine configuration
        var parallelOptions = new ParallelOptions
        {
            // this is out 'Throttle'. 
            // the 'api' only allows 4 concurrent requests
            MaxDegreeOfParallelism = 4
        };

        Console.WriteLine($"Processing {rawOrders.Count} orders...");

        // execution
        Parallel.ForEach(rawOrders, parallelOptions, order =>
        {
            // update peak thread telemetry
            int currentThreads = Process.GetCurrentProcess().Threads.Count;
            Interlocked.Exchange(ref peakThreads, Math.Max(peakThreads, currentThreads));

            // deduplication
            // TryAdd is atomic. If another thread added this id 1 nanosacond ago,
            // this returns false immediately without a heavy lock
            if (!seenOrders.TryAdd(order.OrderId, 0))
            {
                Interlocked.Increment(ref rejectedCount);
                return; // skip duplicate
            }

            // category counting
            // AddOrUpdate is the way to handle counters.
            // It handles the 'get, increment, save' logic safely in one call.
            categoryCounts.AddOrUpdate(
                order.Category,
                1, // Initial value if key is missing
                (key, oldValue) => oldValue + 1 // Update logic if key exists
            );

            // throttled validation
            ValidateAddressWithLegacyApi(order).Wait();
            // Note: In Parallel.ForEach, we block the specific task for 200ms, 
            // but ParallelOptions ensures only 4 of these are 'active' at a time.
        });

        sw.Stop();

        // 5. REPORTING
        PrintReport(sw.ElapsedMilliseconds, rejectedCount, categoryCounts, peakThreads);
    }

    private static async Task ValidateAddressWithLegacyApi(RawOrder order)
    {
        // Simulate external API latency
        await Task.Delay(200);
    }

    private static void PrintReport(long elapsed, int rejected, ConcurrentDictionary<string, int> counts, int peak)
    {
        Console.WriteLine("\n\t PROCESSING REPORT");
        Console.WriteLine($"Total Time: {elapsed}ms");
        Console.WriteLine($"Rejected Duplicates: {rejected}");
        Console.WriteLine($"Peak Threads Observed: {peak}");
        Console.WriteLine("Sales by Category:");
        foreach (var kvp in counts)
        {
            Console.WriteLine($"   - {kvp.Key}: {kvp.Value}");
        }
    }

    private static List<RawOrder> GenerateMockOrders(int count)
    {
        var categories = new[] { "Electronics", "Home", "Fashion", "Toys" };
        var random = new Random();
        return Enumerable.Range(1, count)
            .Select(i => new RawOrder(
                OrderId: random.Next(1, (int)(count * 0.8)), // Creates ~20% duplicates
                Category: categories[random.Next(categories.Length)],
                UserTransactionId: Guid.NewGuid().ToString()
            )).ToList();
    }
}