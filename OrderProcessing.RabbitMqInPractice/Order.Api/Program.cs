using RabbitMQ.Client;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/publish", async () =>
{
    var factory = new ConnectionFactory()
    {
        HostName = "localhost",
        Port = 5672,
        UserName = "guest",
        Password = "guest"
    };

    using var connection = factory.CreateConnection();
    using var channel = connection.CreateModel();

    var queueName = "hello-test-queue";

    channel.QueueDeclare(
        queue: queueName,
        durable: false,
        exclusive: false,
        autoDelete: false,
        arguments: null);

    var message = "Hello RabbitMQ";
    var body = Encoding.UTF8.GetBytes(message);

    channel.BasicPublish(
        exchange: "",
        routingKey: queueName,
        basicProperties: null,
        body: body);

    return "Message published successfully";
});

app.Run();