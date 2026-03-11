using System.Diagnostics;

// Comparing how "Legacy" code (blocking) and "Modern" code (async) 
// handle high traffic. Watch the "Peak Thread Count" closely!

Console.WriteLine("Starting .NET 10 Concurrency Stress Test");
Console.WriteLine($"Baseline Thread Count: {Process.GetCurrentProcess().Threads.Count}");
Console.WriteLine("------------------------------------------------------------\n");

var totalRequests = 100;    // Total "users" hitting our server
var maxConcurrency = 3;     // Only 3 "seats" available at a time

// SCENARIO 1: The "Traffic Jam" (Legacy) ---
// This approach forces threads to sit and wait in line. It's like a 
// coffee shop where customers must stand at the counter until their drink is ready.
Console.WriteLine("Test 1: Legacy 'lock' with Blocking .Wait()");

// You have 100 threads, but they are all "frozen"
// in a state where they cannot be reused for other work
// (like handling a UI click or a health check).
ThreadPool.SetMinThreads(100, 100);
var legacyGate = new LegacySyncGate(maxConcurrency);
await RunStressTest(() => Task.Run(() => legacyGate.HandleRequest()), totalRequests);

Console.WriteLine("\n" + new string('-', 60) + "\n");

// SCENARIO 2: The "Smart Queue" (Modern) ---
// This approach lets threads go do other work while they wait. 
// It's like a coffee shop that gives you a pager; you're free to sit down 
// until it buzzes, keeping the aisle (the CPU) clear.
Console.WriteLine("Test 2: Modern SemaphoreSlim with WaitAsync()");
var modernGate = new ModernAsyncGate(maxConcurrency);
await RunStressTest(() => modernGate.HandleRequestAsync(), totalRequests);

async Task RunStressTest(Func<Task> work, int count)
{
    var timer = Stopwatch.StartNew();
    var allTasks = new List<Task>();

    for (int i = 0; i < count; i++)
    {
        allTasks.Add(work());
    }

    // This background worker monitors how many threads the OS is forced to create.
    var threadMonitor = Task.Run(async () => {
        int peak = 0;
        while (!Task.WhenAll(allTasks).IsCompleted)
        {
            int current = Process.GetCurrentProcess().Threads.Count;
            if (current > peak) peak = current;
            await Task.Delay(100);
        }
        return peak;
    });

    await Task.WhenAll(allTasks);
    timer.Stop();

    int peakThreads = await threadMonitor;
    Console.WriteLine($"Result: Finished in {timer.ElapsedMilliseconds}ms");
    Console.WriteLine($"Efficiency: Reached {peakThreads} peak threads.");
}

// Implementation Details

public class LegacySyncGate(int limit)
{
    private readonly object _lockKey = new();
    private int _activeUsers = 0;

    public void HandleRequest()
    {
        // Important: The 'lock' keyword is strictly synchronous. 
        // We can't 'await' inside it, so we are forced to "Spin" or "Sleep".
        lock (_lockKey)
        {
            while (_activeUsers >= limit)
            {
                // We're literally pausing the thread. This thread can't do 
                // anything else until this loop finishes.
                Thread.Sleep(10);
            }
            _activeUsers++;
        }

        try
        {
            // .Wait() blocks the thread while simulating I/O.
            // This is "Thread Starvation" in the making.
            Task.Delay(500).Wait();
        }
        finally
        {
            lock (_lockKey) { _activeUsers--; }
        }
    }
}



public class ModernAsyncGate(int limit)
{
    // A SemaphoreSlim is like a bouncer who knows how to use 'await'.
    private readonly SemaphoreSlim _bouncer = new(limit, limit);

    public async Task HandleRequestAsync()
    {
        // If the limit is reached, the thread doesn't wait!
        // It returns to the "Pool" to help other people. 
        // It only comes back here when the bouncer signals a spot is open.
        await _bouncer.WaitAsync();

        try
        {
            // We 'await' the delay. 
            // The thread is released back to the OS during this "work".
            await Task.Delay(500);
        }
        finally
        {
            _bouncer.Release();
        }
    }
}