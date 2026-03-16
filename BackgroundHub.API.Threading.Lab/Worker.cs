using BackgroundHub.API.Threading.Lab.Models;

namespace BackgroundHub.API.Threading.Lab;

public class Worker : BackgroundService
{
    private readonly BackgroundTaskQueue _queue;
    private readonly ILogger<Worker> _logger;

    public Worker(BackgroundTaskQueue queue, ILogger<Worker> logger)
    {
        _queue = queue;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // We run all three consumers in parallel
        await Task.WhenAll(
            ProcessSignups(stoppingToken),
            ProcessTelemetryBatches(stoppingToken),
            ProcessRateLimitedApi(stoppingToken));
    }

    // Fire & Forget
    private async Task ProcessSignups(CancellationToken ct)
    {
        await foreach(var task in _queue.SignupChannel.Reader.ReadAllAsync(ct))
        {
            _logger.LogInformation("sending email to {email}...", task.Email);
            await Task.Delay(5000, ct); // Simulate email latency
            _logger.LogInformation("Generating PDF for User {ID}...", task.UserId);
            await Task.Delay(10000, ct); // Simulate PDF generation
        }
    }

    // Telemetry Batching 
    private async Task ProcessTelemetryBatches(CancellationToken ct)
    {
        var batch = new List<LogEntry>();

        while (!ct.IsCancellationRequested)
        {
            // Try to read until we have 100 items or 5 seconds pass
            var readTask = _queue.LogChannel.Reader.WaitToReadAsync(ct).AsTask();
            var timeoutTask = Task.Delay(5000, ct);

            await Task.WhenAny(readTask, timeoutTask);

            while (batch.Count < 100 && _queue.LogChannel.Reader.TryRead(out var log))
            {
                batch.Add(log);
            }

            if (batch.Any())
            {
                _logger.LogWarning("Barch writing {Count} logs to database...", batch.Count);
                batch.Clear(); // In real life, write to DB here
            }
        }
    }

    private async Task ProcessRateLimitedApi(CancellationToken ct)
    {
        await foreach (var request in _queue.ApiChannel.Reader.ReadAllAsync(ct))
        {
            // STRICT RATE LIMIT: Only 5 per second
            await Task.Delay(200, ct); // 1000ms / 5 = 200ms per request

            _logger.LogInformation("Calling 3rd Party API with: {Payload}", request.Payload);
            request.ResponseTcs.SetResult("Success from Remote API");
        }
    }
}
