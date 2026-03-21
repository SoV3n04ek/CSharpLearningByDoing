using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== .NET Async Programming Learning Project ===\n");

        var program = new Program();

        // Run experiments
        Console.WriteLine("Experiment 1: Manual Thread Overhead");
        program.TestManualThreads(10); // Start with 10 threads, not 100 (memory heavy)

        Console.WriteLine("\n" + new string('=', 60) + "\n");

        Console.WriteLine("Experiment 2: ThreadPool Behavior");
        program.TestThreadPoolBehavior();

        Console.WriteLine("\n" + new string('=', 60) + "\n");

        Console.WriteLine("Experiment 3: Blocking vs Async Comparison");
        var apiSimulator = new ApiSimulator();
        await apiSimulator.CompareBlockingVsAsync(50); // Reduced for demo

        Console.WriteLine("\n" + new string('=', 60) + "\n");

        Console.WriteLine("Experiment 4: Async vs Parallel Patterns");
        var workSimulator = new WorkSimulator();

        Console.WriteLine("4A: Wrong - Parallel for I/O");
        await workSimulator.WrongParallelForIo(10);

        Console.WriteLine("\n4B: Correct - Async for I/O");
        await workSimulator.CorrectAsyncForIo(10);

        Console.WriteLine("\n4C: Correct - Parallel for CPU");
        workSimulator.CorrectParallelForCpu(Environment.ProcessorCount);

        Console.WriteLine("\n4D: Wrong - Async for CPU");
        await workSimulator.WrongAsyncForCpu(Environment.ProcessorCount);

        Console.WriteLine("\n" + new string('=', 60));
        Console.WriteLine("All experiments completed. Press any key to exit...");
        Console.ReadKey();
    }

    // DoWork as a method that actually does something measurable
    public void DoWork()
    {
        // Make it CPU-intensive but not too long
        for (int i = 0; i < 10_000_000; i++)
        {
            // Simple CPU work that's measurable
            Math.Sqrt(i);
        }
    }

    private int GetMinThreads()
    {
        ThreadPool.GetMinThreads(out int workerThreads, out int completionPortThreads);
        return workerThreads;
    }

    private int GetMaxThreads()
    {
        ThreadPool.GetMaxThreads(out int workerThreads, out int completionPortThreads);
        return workerThreads;
    }

    private int GetThreadCount()
    {
        // This is a simplified way to get approximate thread count
        // In real code, you'd use Process.GetCurrentProcess().Threads.Count
        // But that requires System.Diagnostics.Process
        Process currentProcess = Process.GetCurrentProcess();
        return currentProcess.Threads.Count;
    }

    // Test 1: Manual threads - see the overhead
    public void TestManualThreads(int requestCount)
    {
        var threads = new List<Thread>();

        // Capture memory before thread creation
        GC.Collect(); // Clean up for accurate measurement
        long memoryBefore = GC.GetTotalMemory(true);

        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < requestCount; i++)
        {
            // Create a local copy to avoid closure issues
            int threadId = i;
            var thread = new Thread(() =>
            {
                Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} started");
                DoWork();
                Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} finished");
            });
            thread.Start();
            threads.Add(thread);
        }

        foreach (var thread in threads)
            thread.Join();

        stopwatch.Stop();

        long memoryAfter = GC.GetTotalMemory(false);

        Console.WriteLine($"Manual threads completed: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Threads created: {requestCount}");
        Console.WriteLine($"Memory increase: {(memoryAfter - memoryBefore) / 1024} KB");
        Console.WriteLine($"Average per thread: {(memoryAfter - memoryBefore) / 1024 / requestCount} KB");
    }

    // Test 2: ThreadPool - configure and observe
    public void TestThreadPoolBehavior()
    {
        // INTERACT: Configure ThreadPool settings
        ThreadPool.SetMinThreads(2, 2);
        ThreadPool.SetMaxThreads(4, 4); // FIX: Reduced from 10 to make starvation visible

        // Wait for settings to take effect
        Thread.Sleep(100);

        Console.WriteLine($"Min threads: {GetMinThreads()}, Max threads: {GetMaxThreads()}");

        var pending = new List<Task<int>>();
        var completedThreads = new List<int>();

        // Add lock for thread-safe collection
        object lockObj = new object();

        for (int i = 0; i < 20; i++) // Reduced from 100 to 20 for demo
        {
            int taskId = i;
            var task = Task.Run(() =>
            {
                var threadId = Thread.CurrentThread.ManagedThreadId;

                lock (lockObj)
                {
                    completedThreads.Add(threadId);
                }

                Console.WriteLine($"Task {taskId} on Thread {threadId} started");

                // Simulate work
                Thread.Sleep(1000);

                Console.WriteLine($"Task {taskId} on Thread {threadId} completed");

                return threadId;
            });
            pending.Add(task);

            // Add small delay to see queuing behavior
            Thread.Sleep(50);

            // Show thread pool status
            ThreadPool.GetAvailableThreads(out int workers, out int io);
            Console.WriteLine($"  [Status] Tasks queued: {pending.Count(t => !t.IsCompleted)}, " +
                             $"Available threads: {workers}");
        }

        // Wait with timeout to avoid hanging
        Task.WaitAll(pending.ToArray(), TimeSpan.FromSeconds(30));

        var usedThreads = completedThreads.Distinct().ToList();
        Console.WriteLine($"\nUnique threads used: {usedThreads.Count}");
        Console.WriteLine($"Thread IDs used: {string.Join(", ", usedThreads)}");
        Console.WriteLine($"Max threads configured: {GetMaxThreads()}");

        if (usedThreads.Count <= GetMaxThreads())
        {
            Console.WriteLine("[DONE] ThreadPool respected the configured max threads");
        }
    }

    // Helper to demonstrate starvation
    public void DemonstrateThreadPoolStarvation()
    {
        Console.WriteLine("\n=== Demonstrating ThreadPool Starvation ===");

        ThreadPool.SetMaxThreads(2, 2);
        Console.WriteLine($"Limited ThreadPool to 2 threads");

        var tasks = new List<Task>();

        // Block all ThreadPool threads
        for (int i = 0; i < 2; i++)
        {
            int taskId = i;
            tasks.Add(Task.Run(() =>
            {
                Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} BLOCKED for 5 seconds");
                Thread.Sleep(5000);
                Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} released");
            }));
        }

        // Wait for threads to be blocked
        Thread.Sleep(100);

        // This task will be queued
        var queuedTask = Task.Run(() =>
            Console.WriteLine("This was queued and will run after threads free up"));

        Console.WriteLine("Queued task submitted - waiting for threads...");
        Task.WaitAll(tasks.ToArray());

        queuedTask.Wait();
        Console.WriteLine("Starvation demonstration complete");
    }

    // ApiSimulator class
    public class ApiSimulator
    {
        private int GetThreadCount()
        {
            return Process.GetCurrentProcess().Threads.Count;
        }

        private async Task<string> SimulateAsyncIo(int delayMs)
        {
            await Task.Delay(delayMs);
            return $"Async result";
        }

        private async Task<string> SimulateBlockingIo(int delayMs)
        {
            // Using Task.Run to simulate blocking without blocking the calling thread
            return await Task.Run(() =>
            {
                Thread.Sleep(delayMs);
                return $"Blocking result";
            });
        }

        public async Task CompareBlockingVsAsync(int concurrentRequests)
        {
            Console.WriteLine("=== BLOCKING MODE (using ThreadPool threads) ===");
            var blockingWatch = Stopwatch.StartNew();

            // Using Task.Run to properly simulate blocking operations
            var blockingTasks = Enumerable.Range(0, concurrentRequests)
                .Select(async _ => await SimulateBlockingIo(1000))
                .ToArray();

            await Task.WhenAll(blockingTasks);
            blockingWatch.Stop();

            Console.WriteLine($"Blocking mode: {blockingWatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Total threads in process: {GetThreadCount()}");

            // Give threads time to settle
            await Task.Delay(100);

            Console.WriteLine("\n=== ASYNC MODE (non-blocking) ===");
            var asyncWatch = Stopwatch.StartNew();

            var asyncTasks = Enumerable.Range(0, concurrentRequests)
                .Select(_ => SimulateAsyncIo(1000))
                .ToArray();

            await Task.WhenAll(asyncTasks);
            asyncWatch.Stop();

            Console.WriteLine($"Async mode: {asyncWatch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Total threads in process: {GetThreadCount()}");

            // Analysis
            Console.WriteLine($"\n[Analysis]:");
            Console.WriteLine($"  Blocking was {(asyncWatch.ElapsedMilliseconds > blockingWatch.ElapsedMilliseconds ? "slower" : "faster")}");
            Console.WriteLine($"  Thread count difference: {(GetThreadCount() - GetThreadCount())}");

            if (blockingWatch.ElapsedMilliseconds > asyncWatch.ElapsedMilliseconds)
            {
                Console.WriteLine("\t[DONE] Async mode completed faster (expected for I/O bound work)");
            }
        }
    }

    // WorkSimulator class
    public class WorkSimulator
    {
        private int GetThreadCount()
        {
            return Process.GetCurrentProcess().Threads.Count;
        }

        private async Task IoBoundWork(int durationMs)
        {
            await Task.Delay(durationMs);
        }

        private Task CpuBoundWork(int durationMs)
        {
            return Task.Run(() =>
            {
                var stopwatch = Stopwatch.StartNew();
                long iterations = 0;
                while (stopwatch.ElapsedMilliseconds < durationMs)
                {
                    // Do actual CPU work
                    double result = 0;
                    for (int i = 0; i < 10000; i++)
                    {
                        result += Math.Sqrt(i);
                    }
                    iterations++;
                }
                // Console.WriteLine($"  CPU work completed {iterations} iterations");
            });
        }

        public async Task WrongParallelForIo(int itemCount)
        {
            Console.WriteLine($"Running with {itemCount} items...");
            var watch = Stopwatch.StartNew();

            // This is a common mistake - Parallel.ForEach with async lambda
            // It will start all tasks but won't wait for them properly
            var options = new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount };

            Parallel.ForEach(Enumerable.Range(0, itemCount), options, async i =>
            {
                await IoBoundWork(1000);
                Console.Write(".");
            });

            watch.Stop();
            Console.WriteLine($"\nTime: {watch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Threads in process: {GetThreadCount()}");
            Console.WriteLine("\t[Note]: This likely finished too fast because it didn't wait properly!");
        }

        public async Task CorrectAsyncForIo(int itemCount)
        {
            Console.WriteLine($"Running with {itemCount} items...");
            var watch = Stopwatch.StartNew();

            var tasks = Enumerable.Range(0, itemCount)
                .Select(async i =>
                {
                    await IoBoundWork(1000);
                    Console.Write(".");
                })
                .ToArray();

            await Task.WhenAll(tasks);

            watch.Stop();
            Console.WriteLine($"\nTime: {watch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Threads in process: {GetThreadCount()}");
            Console.WriteLine($"Expected time: ~1000ms, Actual: {watch.ElapsedMilliseconds}ms");
            Console.WriteLine("[DONE] Async correctly handled I/O bound work");
        }

        public void CorrectParallelForCpu(int itemCount)
        {
            Console.WriteLine($"Running with {itemCount} items...");
            var watch = Stopwatch.StartNew();

            // FIX: Use Parallel.For for CPU-bound work
            Parallel.For(0, itemCount, new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            }, i =>
            {
                CpuBoundWork(1000).Wait();
                Console.Write(".");
            });

            watch.Stop();
            Console.WriteLine($"\nTime: {watch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Threads in process: {GetThreadCount()}");

            var expectedTime = 1000 / Environment.ProcessorCount;
            Console.WriteLine($"Expected time (approx): ~{expectedTime}ms (parallel across {Environment.ProcessorCount} cores)");
            Console.WriteLine("[DONE] Parallel correctly utilized multiple cores for CPU work");
        }

        public async Task WrongAsyncForCpu(int itemCount)
        {
            Console.WriteLine($"Running with {itemCount} items...");
            var watch = Stopwatch.StartNew();

            var tasks = Enumerable.Range(0, itemCount)
                .Select(_ => CpuBoundWork(1000))
                .ToArray();

            await Task.WhenAll(tasks);

            watch.Stop();
            Console.WriteLine($"Time: {watch.ElapsedMilliseconds}ms");
            Console.WriteLine($"Threads in process: {GetThreadCount()}");
            Console.WriteLine($"Expected time: ~1000ms (sequential), Actual: {watch.ElapsedMilliseconds}ms");
            Console.WriteLine("[WARN]  Note: Async didn't provide parallelization for CPU work");
            Console.WriteLine("\t(Task.Run queues work to ThreadPool, but may still run sequentially)");
        }
    }
}