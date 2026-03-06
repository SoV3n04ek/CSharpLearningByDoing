using System.Threading.Channels;

public record ImageJob(int Id, string Url);

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("Start of q");

        using var cts = new CancellationTokenSource();
        // cts.CancelAfter(2000); 
        
        var channel = Channel.CreateBounded<ImageJob>(new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.Wait
        });

        var producerTask = ProduceJobsAsync(channel.Writer, cts.Token);

        int workerCount = 4;
        var workers = Enumerable.Range(1, workerCount)
            .Select(id => ConsumeJobsAsync(id, channel.Reader, cts.Token))
            .ToList();

        try
        {
            await producerTask;

            await Task.WhenAll(workers);

            Console.WriteLine("\nAll tasks are done.");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("\n[SYSTEM] Operation canceled by user");
        }
        catch (Exception)
        {
            var allTasks = Task.WhenAll(workers);
            if (allTasks.Exception != null)
            {
                Console.WriteLine($"\n[ERROR] Finded {allTasks.Exception.InnerExceptions.Count} exceptions:");
                foreach (var ex in allTasks.Exception.InnerExceptions)
                {
                    Console.WriteLine($" - {ex.Message}");
                }
            }
        }
    }

    static async Task ProduceJobsAsync(ChannelWriter<ImageJob> writer, CancellationToken ct)
    {
        for (int i = 1; i <= 500; i++)
        {
            if (ct.IsCancellationRequested) break;

            var job = new ImageJob(i, $"https://server.com/image/{i}.jpg");

            await writer.WriteAsync(job, ct);

            if (i % 50 == 0) Console.WriteLine($"[Producer] Added 50 tasks. (at all: {i})");
        }

        writer.Complete();
    }

    static async Task ConsumeJobsAsync(int workerId, ChannelReader<ImageJob> reader, CancellationToken ct)
    {
        await foreach (var job in reader.ReadAllAsync(ct))
        {
            Console.WriteLine($"Worker #{workerId} drawing Image {job.Id}...");

            if (job.Id % 10 == 0)
            {
                throw new Exception($"Worker #{workerId} had exception on photo with id {job.Id}!");
            }

            await Task.Delay(Random.Shared.Next(100, 300), ct);
        }
    }
    async void SemaphoreSlimInPractice()
    {
        using var semaphore = new SemaphoreSlim(5);
        var tasks = new List<Task>();

        for (int taskId = 1; taskId <= 10; ++taskId)
        {
            tasks.Add(Task.Run(async () =>
            {
                await semaphore.WaitAsync();
                try
                {
                    Console.WriteLine($"[Thread {Thread.CurrentThread.ManagedThreadId}] Task {taskId} run into a tight spot...");
                    await Task.Delay(200); // Simulation of work
                }
                finally
                {
                    Console.WriteLine($"Task {taskId} went. Empty spot for thread!");
                    semaphore.Release();
                }
            }));
        }

        await Task.WhenAll(tasks);
    }
}