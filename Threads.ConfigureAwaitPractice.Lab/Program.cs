using System.Diagnostics;

Console.WriteLine($"[Main] Started on Thread: {Environment.CurrentManagedThreadId}");

await BusinessProcess.StartAsync();

public static class BusinessProcess
{
    public static async Task StartAsync()
    {
        Console.WriteLine($"[Process] Before Library Call - Thread: {Environment.CurrentManagedThreadId}");

        // call a "library" method
        await LowLevelLibrary.DoWorkAsync().ConfigureAwait(false);

        // continuation
        Console.WriteLine($"[Process] After Library Call - Thread: {Environment.CurrentManagedThreadId}");

        // Call stack 
        StackTrace st = new StackTrace();
        Console.WriteLine($"[Process] Current Stack Depth: {st.FrameCount}");
    }
}

public static class LowLevelLibrary
{
    public static async Task DoWorkAsync()
    {
        await Task.Delay(1000); // Simulate I/O
        Console.WriteLine($"[Library] Work Finished - Thread: {Environment.CurrentManagedThreadId}");
    }
}