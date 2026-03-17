// test 1 should be leak of memory without line of code
SomeTest someTest = new SomeTest();
someTest.Method();
GC.Collect();
GC.WaitForPendingFinalizers();
Thread.Sleep(100);
GlobalPublisher.Instance.RaiseTick();

// test 2
var subscriber = new Subscriber("Beta");
subscriber.Dispose();
GC.Collect();
GlobalPublisher.Instance.RaiseTick();


class SomeTest
{
    public void Method()
    {
        // to ensure local variables go out of scope
        var miniSubscriber = new Subscriber("Alpha");
    }
}


public class GlobalPublisher
{
    private static readonly Lazy<GlobalPublisher> _instance = new(() => new GlobalPublisher());
    public static GlobalPublisher Instance => _instance.Value;

    public event Action OnTick;

    public void RaiseTick()
    {
        Console.WriteLine($"\n[Publisher] Raising Tick...");
        OnTick?.Invoke();
    }
}

public class Subscriber : IDisposable
{
    private readonly string _name;

    public Subscriber(string name)
    {
        _name = name;
        // This is where the leak is born
        GlobalPublisher.Instance.OnTick += HandleTick;
        Console.WriteLine($"[Subscriber {_name}] Created and Subscribed.");
    }

    private void HandleTick()
    {
        Console.WriteLine($"[Subscriber {_name}] I am alive! (Memory address: {GetHashCode()})");
    }

    public void Dispose()
    {
        // Removing the reference
        GlobalPublisher.Instance.OnTick -= HandleTick;
        Console.WriteLine($"[Subscriber {_name}] Disposed (Unsubscribed).");
    }

    ~Subscriber()
    {
        Console.WriteLine($"[Subscriber {_name}] Finalized (Actually removed from RAM.");
    }
}