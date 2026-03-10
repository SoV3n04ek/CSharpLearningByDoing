public record Shipment(string Id, double Weight, string Destination, bool IsUrgent);

public class LogisticsEngine
{
    // Part A: Using Predicate<T> for filtering
    public static List<Shipment> Filter(List<Shipment> shipments, Predicate<Shipment> criteria)
    {
        var result = new List<Shipment>();
        foreach (var s in shipments)
        {
            if (criteria(s)) result.Add(s); // The lambda is executed here
        }
        return result;
    }

    // Part B: Using Func<T, TResult> for transformation/calculation
    public static void ProcessFees(List<Shipment> shipments, Func<Shipment, double> feeCalculator)
    {
        foreach (var s in shipments)
        {
            double fee = feeCalculator(s);
            Console.WriteLine($"Shipment {s.Id}: Fee = ${fee:F2}");
        }
    }

    // Part C: Using Action<T> for execution/side-effects
    public static void ExecuteAction(List<Shipment> shipments, Action<Shipment> process)
    {
        foreach (var s in shipments)
        {
            process(s);
        }
    }
}

class Program
{
    static void Main()
    {
        var shipments = new List<Shipment>
        {
            new Shipment("USA-001", 120.5, "New York", false),
            new Shipment("UK-002", 45.0, "London", true),
            new Shipment("GER-003", 200.0, "Berlin", true),
            new Shipment("CAN-004", 10.0, "Toronto", false)
        };

        // PART A: Predicate<T> (Expression Lambda)
        var heavyPackages = LogisticsEngine.Filter(shipments, s => s.Weight > 100);
        Console.WriteLine($"Found {heavyPackages.Count} heavy packages.");

        // PART B: Func<T, TResult> (Statement Lambda) ---
        LogisticsEngine.ProcessFees(shipments, s =>
        {
            double baseRate = s.Weight * 0.5;
            return s.IsUrgent ? baseRate + 50.0 : baseRate;
        });

        // PART C: Action<T> (Expression Lambda)
        Console.WriteLine("\nPrinting Manifest:");
        LogisticsEngine.ExecuteAction(shipments, s => Console.WriteLine($"-> [{s.Destination}] ID: {s.Id}"));
    }
}