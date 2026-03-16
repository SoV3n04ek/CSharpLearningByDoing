using System.Threading.Channels;

// Bounded channel (max 100 items)
Channel<string> channel = Channel.CreateBounded<string>(100);

// The Producer (Api controller)
async Task Produce(string message)
{
    await channel.Writer.WriteAsync(message);
}

// The consumer (background worker)
async Task Consume()
{
    // This loops efficiently and 'pauses' when the channel is empty
    await foreach(var message in channel.Reader.ReadAllAsync())
    {
        Console.WriteLine($"Processing background task: {message}");
        await Task.Delay(1000);
    }
}