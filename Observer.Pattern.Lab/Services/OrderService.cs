using Observer.Pattern.Lab.Abstractions;
using Observer.Pattern.Lab.Models;

namespace Observer.Pattern.Lab.Services;

public class OrderService
{
    private readonly IMessageBroker _broker;

    public OrderService(IMessageBroker broker)
    {
        _broker = broker;
    }

    public void ProcessOrder(string customer, decimal amount)
    {
        var order = new Order(Guid.NewGuid(), customer, amount, "Processed");

        Console.WriteLine($"[OrderService] Order {order.Id} created.");

        _broker.Publish(order);
    }
}
