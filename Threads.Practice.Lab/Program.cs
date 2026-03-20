using System.Diagnostics;

class Program
{

    // Test 1: Manual threads - see the overhead
    public void TestManualThreads(int requestCount)
    {
        var threads = new List<Thread>();
        var stopwatch = Stopwatch.StartNew();

        for (int i = 0; i < requestCount; i++)
        {
            var thread = new Thread(() => DoWork());
            thread.Start();
            threads.Add(thread);
        }

        foreach (var thread in threads)
            thread.Join();

        Console.WriteLine($"Manual threads: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine($"Threads created: {requestCount}");
        Console.WriteLine($"Memory: {GC.GetTotalMemory(false) / 1024} KB");

        // What to observe:
        // - Memory spikes (~1MB per thread)
        // - Slow startup time
        // - OS context switching overhead  
    }
    public void DoWork()
    {
        for (int i = int.MinValue; i <= int.MaxValue; ++i) { }
    }
    
}