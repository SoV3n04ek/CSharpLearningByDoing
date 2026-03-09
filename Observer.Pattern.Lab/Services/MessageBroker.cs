using Observer.Pattern.Lab.Abstractions;

namespace Observer.Pattern.Lab.Services;

public class MessageBroker : IMessageBroker
{
    //Dictionary<string, List<Action<object>>>
    private readonly Dictionary<string, List<Action<object>>> _subscribers = new();

    public void Subscribe<T>(Action<T> handler)
    {
        string topic = typeof(T).FullName!;

        if (!_subscribers.ContainsKey(topic))
        {
            _subscribers[topic] = new List<Action<object>>();
        }

        // Wrap the handler to match the Action<object> signature
        Action<object> wrapper = (data) => handler((T)data);

        _subscribers[topic].Add(wrapper);
    }

    public void Publish<T>(T message)
    {
        string topic = typeof(T).FullName!;

        if (!_subscribers.ContainsKey(topic))
        {
            Console.WriteLine($"[Broker] No subscribers for topic: {topic}");
        }

        foreach (var handler in _subscribers[topic])
        {
            // Execute the handler with the message
            handler(message!);
        }
    }
}
