using RabbitMQ.Client;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/publish", async () =>
{
    RabbitMQ.Client.ConnectionFactory factory = new()
    {
        HostName = "localhost",
        Port = 5672,
        UserName = "guest",
        Password = "guest"
    };

    // using 'await using' for asynchronous disposal
    await using var connection = await factory.CreateConnectionAsync();
    await using var channel = await connection.CreateChannelAsync();

    var queueName = "hello-test-queue";

    // Declare the queue 
    await channel.QueueDeclareAsync(
        queue: queueName,
        durable: false, // Survive RabbitMQ restart
        exclusive: false,
        autoDelete: false,
        arguments: null);

    var message = $"Hello RabbitMQ at {DateTime.Now}";
    byte[] messageBodyBytes = Encoding.UTF8.GetBytes(message);

    CancellationToken cancellationToken = new();
    var props = new BasicProperties();

    // Publish message
    await channel.BasicPublishAsync(
        exchange: string.Empty, // using the default exchange 
        routingKey: queueName,
        mandatory: true,
        basicProperties: props,
        body: messageBodyBytes,
        cancellationToken: cancellationToken);

    return Results.Ok(new { status= "Message published successfully", message });
});

app.Run();