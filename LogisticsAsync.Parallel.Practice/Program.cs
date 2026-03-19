
using Serilog;
using System.Collections.Concurrent;
using System.Diagnostics;

public record Truck(int Id);
public record RouteResult(int TruckId, long CalculationTimeMs, string Status);

public class DispatchOptimizer
{
    private readonly SemaphoreSlim _loadingDocks = new(3, 3);
    private readonly ConcurrentBag<RouteResult> _results = new();
    private readonly ILogger _logger;

    public DispatchOptimizer(ILogger logger) => _logger = logger;

    public async Task RunOptimizationAsync(IEnumerable<Truck> fleet, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        var options = new ParallelOptions
        {
            CancellationToken = ct,
            MaxDegreeOfParallelism = Environment.ProcessorCount
        };

        await Parallel.ForEachAsync(fleet, options, async (truck, token) =>
        {
            try
            {
                var calcSw = Stopwatch.StartNew();
                OptimizeRoute(truck);
                calcSw.Stop();

                await LoadCargoAsync(truck, token).ConfigureAwait(false);

                _results.Add(new RouteResult(truck.Id, calcSw.ElapsedMilliseconds, "success"));
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Truck {Id} optimization cancelled.", truck.Id);
            }
        });

        stopwatch.Stop();
        _logger.LogInformation("Optimization finished");
        _logger.LogInformation("Total Trucks Processed: {Count}", _results.Count);
        _logger.LogInformation("Total Time: {Ms}ms", stopwatch.ElapsedMilliseconds);
    }

    private void OptimizeRoute(Truck truck)
    {
        // Simulate heavy CPU work (Math/Algorithms)
        // DO NOT use Task.Delay here because this is CPU-bound
        var end = DateTime.Now.AddMilliseconds(100);
        while (DateTime.Now < end) { /* Spinning CPU */ }
    }

    private async Task LoadCargoAsync(Truck truck, CancellationToken ct)
    {
        // Request access to the limited resource (3 docks)
        await _loadingDocks.WaitAsync(ct).ConfigureAwait(false);

        try
        {
            _logger.LogInformation("[DOCK] Truck {Id} started loading.", truck.Id);

            // Simulate I/O (Waiting for physical loading)
            // Thread is released to the pool here!
            await Task.Delay(Random.Shared.Next(500, 1000), ct).ConfigureAwait(false);

            _logger.LogInformation("[DOCK] Truck {Id} finished loading and left.", truck.Id);
        }
        finally
        {
            // Release the semaphore in a finally block
            _loadingDocks.Release();
        }
    }
}