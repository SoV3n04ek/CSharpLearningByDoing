using RabbitMQ.Client;
using System.Text;
using Order.Contracts;
using Order.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Singleton Connection (The right way)
builder.Services.AddSingleton<IConnection>(sp =>
{
    var factory = new ConnectionFactory { HostName = "localhost" };
    return factory.CreateConnectionAsync().GetAwaiter().GetResult();
});

builder.Services.AddSingleton<IMessagePublisher, RabbitMqPublisher>();

var app = builder.Build();

// Infrastructure setup (Run once at startup)
await SetupRabbitMqInfrastructure(app.Services);

app.MapGet("/publish", async (IConnection connection) =>
{
    // Create a channel just for this request (Channels are lightweight)
    using var channel = await connection.CreateChannelAsync();

    var message = $"Order Created at {DateTime.Now}";
    var body = Encoding.UTF8.GetBytes(message);

    // Publish (To the Topic Exchange, not the queue)
    await channel.BasicPublishAsync(
        exchange: "orders-exchange",
        routingKey: "order.created.v1",
        body: body);

    return Results.Ok(new { status = "Published to Topic", message });
});

// The Clean Endpoint
app.MapPost("/api/orders", async (OrderRequest request, IMessagePublisher publisher) =>
{
    // 1. Validation (Requirement 4A)
    if (string.IsNullOrEmpty(request.CustomerId) || !request.Items.Any())
        return Results.BadRequest("Invalid order data");

    // 2. Create the Event
    var orderId = Guid.NewGuid();
    var orderEvent = new OrderCreated(orderId, request.CustomerId, DateTime.UtcNow);

    // 3. Publish using the Abstraction
    await publisher.PublishAsync(orderEvent, "order.created.v1");

    // 4. Return 202 Accepted (Requirement 4A)
    return Results.Accepted($"/api/orders/{orderId}", new { orderId, status = "Processing" });
});

app.Run();

async Task SetupRabbitMqInfrastructure(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var connection = scope.ServiceProvider.GetRequiredService<IConnection>();
    using var channel = await connection.CreateChannelAsync();

    // 1. Define Names
    const string ExchangeName = "orders-exchange";
    const string QueueName = "hello-test-queue";
    const string DlxExchange = "dlx-exchange";
    const string DlxQueue = "dead-messages-queue";

    // 2. Setup the "Hospital" (DLX)
    // Direct exchange is fine here—we want to send bad messages to one specific place
    await channel.ExchangeDeclareAsync(DlxExchange, ExchangeType.Direct, durable: true);
    await channel.QueueDeclareAsync(DlxQueue, durable: true, exclusive: false, autoDelete: false);
    await channel.QueueBindAsync(DlxQueue, DlxExchange, routingKey: "dead-letter");

    // 3. Setup the Main Exchange
    await channel.ExchangeDeclareAsync(ExchangeName, ExchangeType.Topic, durable: true);

    // 4. Setup the Main Queue with DLX arguments
    // NOTE: This will fail with 406 if "hello-test-queue" already exists as non-durable!
    var queueArgs = new Dictionary<string, object?>
    {
        { "x-dead-letter-exchange", DlxExchange },
        { "x-dead-letter-routing-key", "dead-letter" } // This matches the bind above
    };

    await channel.QueueDeclareAsync(
        queue: QueueName,
        durable: true,
        exclusive: false,
        autoDelete: false,
        arguments: queueArgs);

    // 5. Bind the Main Queue to the Exchange
    // This tells RabbitMQ: "Send any message with a key starting with 'order.' here"
    await channel.QueueBindAsync(QueueName, ExchangeName, routingKey: "order.#");

    Console.WriteLine("Infrastructure Ready: Queues and Exchanges created.");
}