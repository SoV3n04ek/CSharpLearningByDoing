/*
 * Topics: Stack, Heap, ThreadPool, Events
 * Goal: See the life of objects and thread switching in real time.
 */

public struct OrderShortInfo { public int Id; } // Lives on Stack

// reference type lives on Heap
public class Order
{
    public int Id { get; set; }
    public string DishName { get; set; }
    public bool IsReady { get; set; }
}

public class WaiterService
{
    public event Action<Order> OnOrderStarted;
    public event Action<Order> OnOrderReady;

    public async Task ServeOrderAsync(int id, string dish)
    {
        // Creating object
        var order = new Order { Id = id, DishName = dish };

        // Starting event
        OnOrderStarted?.Invoke(order);
        LogThread($"Started cooking {dish}");

        // Async waiting (making free thread)
        await Task.Delay(2000);

        // Event ended (already started, maybe on another thread)
        order.IsReady = true;
        LogThread($"Finished cooking {dish}");
        OnOrderReady?.Invoke(order);
    }

    private void LogThread(string action)
    {
        Console.WriteLine($"[THREAD {Thread.CurrentThread.ManagedThreadId}] {action}");
    }
}

class Program
{
    static async Task Main()
    {
        Console.WriteLine($"Memory on start: {GC.GetTotalMemory(true)} bytes");

        var waiter = new WaiterService();


        // waiter.OnOrderStarted += ord => MyNotification(">>> Accepted: ", ord);
        //waiter.OnOrderReady += ord => MyNotification($"<<< READY: ", ord);
        waiter.OnOrderStarted += MyNotification;
        waiter.OnOrderReady += MyNotification;

        // Start (ThreadPool)
        var task1 = waiter.ServeOrderAsync(1, "borsch");
        var task2 = waiter.ServeOrderAsync(2, "pizza");

        await Task.WhenAll(task1, task2);

        await Console.Out.WriteLineAsync($"Memory at the end: {GC.GetTotalMemory(false)} bytes");
    }

    private static void Waiter_OnOrderStarted(Order obj)
    {
        MyNotification(">>> Accepted: ", obj);
    }

    static void MyNotification(string text, Order o)
    {
        Console.WriteLine($"{text} {o.DishName}");
    }

    static void MyNotification(Order o)
    {
        Console.WriteLine(o.DishName);
    }
}