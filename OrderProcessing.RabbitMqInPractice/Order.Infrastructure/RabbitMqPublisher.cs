using Order.Contracts;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace Order.Infrastructure;

public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, string routingKey) where T : class;
}

public class RabbitMqPublisher(IConnection connection) : IMessagePublisher
{
    public async Task PublishAsync<T>(T message, string routingKey) where T : class
    {
        // create a channel for this specific operation
        await using var channel = await connection.CreateChannelAsync();

        // serialize the POCO to json
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        // set persistence
        // this ensures the message is written to disk
            var props = new BasicProperties { Persistent = true };

        // publish to topic exchange
        await channel.BasicPublishAsync(
            exchange: RabbitMqConstants.ExchangeName,
            routingKey: routingKey,
            basicProperties: props,
            body: body,
            mandatory: false);
    }

}
