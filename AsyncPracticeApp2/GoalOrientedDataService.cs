namespace AsyncPracticeApp2;

// Task: 

/*
 * The following snippet represents a developer "addicted to control."
 * They are using manual Task.Run, blocking with .Result, 
 * and trying to "force" synchronization where it isn't needed.
 * 
 * // TODO: Refactor this "Mental Noise" implementation

public class OverthinkingDataService
{
    public async Task<string> ProcessDataAsync(int id)
    {
        // Issue 1: Overthinking control - using Task.Run for IO work
        return Task.Run(() => 
        {
            var data = FetchFromDb(id); // Synchronous DB call inside Task.Run
            
            // Issue 2: Defensive over-checking (Mental Noise)
            if (data == null) throw new Exception("Total failure");
            
            // Issue 3: Blocking the thread (The Control Addiction)
            var result = TransformDataAsync(data).Result; 
            
            return result;
        }).Result; 
    }
} 

Task 1: Refactor the service to be "Pure Async" (Async all the way up).

Task 2: Implement a SemaphoreSlim(1, 1) to handle resource access—this represents "Clear Goal Focus" by ensuring only one mental/process thread handles the critical section at a time.

Task 3: Write a 3-sentence "Code Review" comment explaining how the new code reduces "Mental Noise" compared to the old version.

*/

public class GoalOrientedDataService
{
    private readonly HttpClient _httpClient = new();
    // Using a Semaphore to focus mental energy on one task at a time
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<string> ProcessDataAsync(int id, CancellationToken ct)
    {
        // Step 1: Entrance. No over-checking, just action
        await _gate.WaitAsync(ct);

        try
        {
            // Step 2: trust the async state machine
            // Removed Task.Run because it was "Mental Noise"
            var data = await FetchFromDbAsync(id, ct);

            // Step 3: Direct Transformation
            // No .Result (Deadlock risk). Just pure flow
            return await TransformDataAsync(data, ct);
        }
        finally
        {
            // Step 4: Release control
            _gate.Release();
        }
    }

    private async Task<string> FetchFromDbAsync(int id, CancellationToken ct)
        => await Task.FromResult($"Data_{id}");

    private async Task<string> TransformDataAsync(string data, CancellationToken ct)
        => await Task.FromResult($"Transformed_{data}");

}