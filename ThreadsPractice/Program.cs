public class TruckingOperation
{
    // the shared resource limit: 10 parking places.
    private const int ParkingPlacesCount = 10;
    private static readonly SemaphoreSlim _parkingSpots = new SemaphoreSlim(ParkingPlacesCount);
    private const int TotalTrucks = 100;
    private const int UnloadTimeMs = 3500; // 3 seconds per truck

    public static void RunExample()
    {
        Console.WriteLine($"*** Starting Trucking Operation with {TotalTrucks} trucks and {ParkingPlacesCount} parking spots for unloading ***");

        // 1. Create 100 tasks (one for each truck)
        Task[] truckTasks = new Task[TotalTrucks];

        for (int i = 0; i < TotalTrucks; i++)
        {
            int truckId = i + 1; // Unique id for each truck
            truckTasks[i] = Task.Run(() => UnloadTruck(truckId));
        }

        // 2. Wait for all tasks to complete 

        Console.WriteLine("\n*** All trucks have been unloaded. Operation Complete.");
        Console.ReadLine();
    }

    private static void UnloadTruck(int truckId)
    {
        Console.WriteLine($"Truck {truckId} is approaching the parking lot.");

        // wait to acquire a spot (blocks if 3 are in use)
        _parkingSpots.Wait(); // blocks current block until it can enter a semaphore

        try
        {
            Console.WriteLine($"\t[PARKED] Truck {truckId} is unloading. AvaibleSpots: {_parkingSpots.CurrentCount}");

            // Simulate 3 seconds unloading time
            Thread.Sleep(UnloadTimeMs);

            Console.WriteLine($"\t[EXITED] Truck {truckId} finished unloading");
        }
        finally
        {
            _parkingSpots.Release();
        }
    }
}

class Program
{
    public static void Main()
    {
        TruckingOperation.RunExample();
    }
}