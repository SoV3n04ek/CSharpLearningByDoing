/*
 * TOPIC: Advanced Async/Await Patterns
 * LEARNING GOALS:
 * 1. Task.WhenAll for concurrency.
 * 2. CancellationTokens for resource management.
 * 3. IProgress<T> for UI/Console feedback.
 * 4. Async exception handling.
 */

class Program
{
    static async Task Main(string[] args)
    {
        var cts = new CancellationTokenSource();
        var progress = new Progress<int>(percent => Console.WriteLine($"[PROGRESS] {percent}% completed"));

        try
        {
            // Challenge: Try to press a key to cancel mid-execution
            Console.WriteLine("Starting downloads... Press any key to cancel.");

            // Start a task that listens for a key press to cancel
            _ = Task.Run(() => { Console.ReadKey(); cts.Cancel(); });

            await RunLabAsync(cts.Token, progress);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\n[!] Operation was cancelled by the user.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n[ERROR] One or more tasks failed: {ex.Message}");
        }
    }

    private static async Task RunLabAsync(CancellationToken token, IProgress<int> progress)
    {
        var fileNames = new List<string> { "data.bin", "image.png", "config.json", "corrupted.file" };

        // Implement the logic to download and process files concurrently
        // 1. We create a list of "Contracts" (Tasks)
        // We are STARTING the downloads here but not waiting for them yet
        List<Task<string>> downloadTasks = new List<Task<string>>();

        foreach (var name in fileNames)
        {
            // We call the method. It starts immediately and returns a "Promise" (Task)
            downloadTasks.Add(DownloadFileAsync(name, token));
        }

        // 2. Now we tell the Manager to wait for EVERYONE
        // Task.WhenAll returns a single Task that completes when all others do
        Console.WriteLine("[SYSTEM] ALL downloads started. Waiting for comletion...");

        // This is where the "Magic" happens. We yield the thread until they are all done.
        string[] results = await Task.WhenAll(downloadTasks);

        // 3. Reporting progress
        progress.Report(100);

        foreach (var content in results)
        {
            Console.WriteLine($"[SYSTEM] Processed result: {content.Length} characters.");
        }
    }

    static async Task<string> DownloadFileAsync(string name, CancellationToken token)
    {
        // Simulate network delay
        // We pass the 'token' so if the user cancels, Task.Delay stops immediately.
        await Task.Delay(2000, token);

        return $"Content of {name}";
    }
}