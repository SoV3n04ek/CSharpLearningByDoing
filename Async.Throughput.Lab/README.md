Scenario
You are building a system that simulates downloading and processing 500 images. If you download all 500 at once, the "Server" (simulated) will ban your IP. You must limit the system to exactly 4 concurrent workers.

Learning Objectives
System.Threading.Channels: Learn to decouple data generation from data processing.

CancellationTokenSource: Learn how to stop the entire pipeline instantly if the user clicks "Cancel."

Task.WhenAll with Error Aggregation: Learn how to catch exceptions from multiple workers without losing any.

SemaphoreSlim (Bonus): Compare it against Channels for simple throttling.

🛠 The Technical Task
Step 1: The "Job" Model
Create a public record ImageJob(int Id, string Url);.

Step 2: The Producer
Create a method ProduceJobsAsync(ChannelWriter<ImageJob> writer).

It should loop 500 times.

It should drop an ImageJob into the channel.

Once done, it must call writer.Complete().

Step 3: The Consumer (The Worker)
Create a method ConsumeJobsAsync(int workerId, ChannelReader<ImageJob> reader).

It should use await foreach (var job in reader.ReadAllAsync()).

Inside, simulate a download: await Task.Delay(Random.Shared.Next(100, 500)).

The Twist: If job.Id % 10 == 0, throw a new Exception("Download Failed!").

Step 4: The Orchestrator (Main)
In your Main method:

Create a Channel<ImageJob>.CreateBounded(100). (The "100" limits how many jobs can sit in the queue).

Start one Producer task.

Start four Consumer tasks.

Use Task.WhenAll(consumers) to wait for the workers to finish.

Requirement: Use a try-catch block that handles AggregateException to print every single error that occurred during the 500 downloads.

Why this is "Advanced"
Backpressure: If your producer is faster than your consumers, the Bounded channel will automatically pause the producer so you don't run out of RAM.

Graceful Shutdown: You will learn how to close a channel so that workers finish what's left in the queue before exiting.

Thread Safety: Channels are thread-safe by design; you don't need lock keywords.

Manual Test Verification
Observation: Watch the console. You should see 4 workers working in parallel.

Stress Test: Change the worker count from 4 to 100. Watch how much faster it goes (and how much more "chaos" the console shows).

Cancellation: Add a CancellationTokenSource that cancels after 2 seconds. Does your program stop cleanly or does it crash with an unhandled exception?