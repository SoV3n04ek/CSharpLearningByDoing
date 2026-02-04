using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

/*
 * TOPIC: .NET Concurrency Mastery: Async/Await, Parallelism, and Thread Synchronization.
 * * LEARNING GOAL: 
 * Understand the boundary between I/O-bound operations (Async) and CPU-bound operations (Parallel).
 * Master thread safety using primitive locks and modern asynchronous semaphores.
 * Observe the behavior of the .NET ThreadPool and the State Machine behind async/await.
 * * TECHNICAL TASK:
 * 1. Simulate an asynchronous I/O data fetch (GetOrdersAsync) and observe thread switching.
 * 2. Execute a heavy CPU-bound calculation (CalculateTaxes) using all available processor cores.
 * 3. Safely manage a shared integer resource (StockCount) using both 'lock' and 'SemaphoreSlim'.
 * 4. Demonstrate why thread-safe collections (ConcurrentBag) are mandatory in parallel loops.
 */

public class Order
{
    public int Id { get; set; }
    public decimal Price { get; set; }

    public Order(int id, decimal price)
    {
        Id = id;
        Price = price;
    }
}

class Program
{
    // Shared resource (Critical Section data)
    private static int _stockCount = 100;

    // Synchronization primitives
    private static readonly object _lockObject = new();
    private static readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

    // Thread-safe collection to gather results from multiple threads
    private static readonly ConcurrentBag<decimal> _taxResults = new();

    static async Task Main(string[] args)
    {
        Console.WriteLine("=== START OF MULTI-THREADING SIMULATION ===\n");
        LogThreadInfo("Main method started");

        // --- PHASE 1: ASYNC I/O ---
        // We simulate fetching data from a database or API.
        List<Order> orders = await GetOrdersAsync();

        // --- PHASE 2: PARALLEL PROCESSING (CPU Bound) ---
        // We process the orders using Parallel.ForEach to utilize all CPU cores.
        ProcessOrdersInParallel(orders);

        // --- PHASE 3: SYNCHRONIZATION TEST ---
        // We will now simulate multiple concurrent attempts to decrease stock.
        await RunSynchronizationTests();

        Console.WriteLine("\n=== SIMULATION ENDED ===");
        LogThreadInfo("Main method finished");
    }

    /// <summary>
    /// Demonstrates Async/Await and the behavior of the ThreadPool.
    /// </summary>
    static async Task<List<Order>> GetOrdersAsync()
    {
        LogThreadInfo("GetOrdersAsync: Before 'await'");

        // Task.Delay is non-blocking. The thread is returned to the ThreadPool.
        // When the timer finishes, a thread (likely a different one) resumes the work.
        await Task.Delay(1000);

        LogThreadInfo("GetOrdersAsync: After 'await'");

        var rand = new Random();
        return Enumerable.Range(1, 10).Select(i => new Order(i, rand.Next(10, 100))).ToList();
    }

    /// <summary>
    /// Demonstrates Data Parallelism using Parallel.ForEach.
    /// </summary>
    static void ProcessOrdersInParallel(List<Order> orders)
    {
        Console.WriteLine("\nStarting Parallel.ForEach (CPU-bound work)...");

        // Parallel.ForEach partitions the collection and processes it on multiple threads simultaneously.
        Parallel.ForEach(orders, order =>
        {
            // Thread.SpinWait mimics heavy mathematical work without putting the thread to sleep.
            Thread.SpinWait(5_000_000);

            decimal tax = order.Price * 0.2m;

            // We use a ConcurrentBag because a standard List<T> is NOT thread-safe for Add() operations.
            _taxResults.Add(tax);

            Console.WriteLine($" > Order {order.Id}: Tax {tax:C} processed on Thread {Thread.CurrentThread.ManagedThreadId}");
        });

        Console.WriteLine($"Parallel processing complete. Taxes calculated: {_taxResults.Count}");
    }

    /// <summary>
    /// Manually triggers concurrent calls to test Lock and Semaphore mechanisms.
    /// </summary>
    static async Task RunSynchronizationTests()
    {
        Console.WriteLine($"\nInitial Stock: {_stockCount}");

        // Testing LOCK (Synchronous)
        // We use Task.Run to simulate calls coming from different threads.
        var lockTasks = Enumerable.Range(1, 5).Select(_ => Task.Run(() => DecreaseStockWithLock(10)));
        await Task.WhenAll(lockTasks);

        // Testing SEMAPHORE (Asynchronous)
        var semaphoreTasks = Enumerable.Range(1, 5).Select(_ => DecreaseStockWithSemaphoreAsync(10));
        await Task.WhenAll(semaphoreTasks);

        Console.WriteLine($"Final Stock after all operations: {_stockCount}");
    }

    /// <summary>
    /// Standard 'lock' usage. Best for short-lived, synchronous critical sections.
    /// </summary>
    public static void DecreaseStockWithLock(int amount)
    {
        // lock(obj) is syntax sugar for Monitor.Enter/Monitor.Exit.
        // It ensures only ONE thread can enter this block at a time.
        lock (_lockObject)
        {
            if (_stockCount >= amount)
            {
                // We use Thread.Sleep here only to prove that other threads are waiting.
                Thread.Sleep(50);
                _stockCount -= amount;
                Console.WriteLine($"[Lock] Reduced by {amount}. Remaining: {_stockCount} (Thread {Thread.CurrentThread.ManagedThreadId})");
            }
        }
    }

    /// <summary>
    /// SemaphoreSlim usage. The ONLY way to protect a critical section that contains 'await'.
    /// </summary>
    public static async Task DecreaseStockWithSemaphoreAsync(int amount)
    {
        // WaitAsync() does not block the thread; it yields control until the semaphore is free.
        await _semaphore.WaitAsync();
        try
        {
            if (_stockCount >= amount)
            {
                // Unlike 'lock', SemaphoreSlim allows us to use 'await' inside the protected area.
                await Task.Delay(50);
                _stockCount -= amount;
                Console.WriteLine($"[Semaphore] Reduced by {amount}. Remaining: {_stockCount} (Thread {Thread.CurrentThread.ManagedThreadId})");
            }
        }
        finally
        {
            // Release() must always be in a 'finally' block to prevent deadlocks if an error occurs.
            _semaphore.Release();
        }
    }

    static void LogThreadInfo(string message)
    {
        var t = Thread.CurrentThread;
        Console.WriteLine($" >>> {message.PadRight(30)} | ID: {t.ManagedThreadId} | IsPool: {t.IsThreadPoolThread}");
    }
}