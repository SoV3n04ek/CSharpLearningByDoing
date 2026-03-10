namespace Observer.Pattern.Lab.Abstractions;

public interface IMessageBroker
{
    //void Subscribe(string topic, Action<object> handler);

    //void Publish(string topic, object data);

    void Subscribe<T>(Action<T> handler);
    void Publish<T>(T message);
}