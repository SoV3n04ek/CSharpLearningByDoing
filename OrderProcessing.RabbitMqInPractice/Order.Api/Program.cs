using Order.Contracts;
using Order.Infrastructure;
using RabbitMQ.Client;
using System.Text;

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
        exchange: RabbitMqConstants.ExchangeName,
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

    /* Development only */
    try
    {
        await channel.ExchangeDeleteAsync(RabbitMqConstants.DlxExchange);
        await channel.QueueDeleteAsync(RabbitMqConstants.DlxQueue);
        await channel.ExchangeDeleteAsync(RabbitMqConstants.ExchangeName);
        await channel.QueueDeleteAsync(RabbitMqConstants.QueueName);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\t[Exception] | {ex.Message}");
    }

    // DLX Setup
    await channel.ExchangeDeclareAsync(
        RabbitMqConstants.DlxExchange,
        ExchangeType.Direct,
        durable: true);
    
    await channel.QueueDeclareAsync(
        RabbitMqConstants.DlxQueue,
        durable: true,
        exclusive: false,
        autoDelete: false);

    await channel.QueueBindAsync(
        RabbitMqConstants.DlxQueue,
        RabbitMqConstants.DlxExchange,
        RabbitMqConstants.DeadLetterRoutingKey);

    // Main exchange setup
    await channel.ExchangeDeclareAsync(
        RabbitMqConstants.ExchangeName,
        ExchangeType.Topic,
        durable: true);

    // Main queue setup
    var queueArgs = new Dictionary<string, object?>
    {
        { "x-dead-letter-exchange", RabbitMqConstants.DlxExchange },
        { "x-dead-letter-routing-key", RabbitMqConstants.DeadLetterRoutingKey }
    };

    // Durable true consistently
    await channel.QueueDeclareAsync(
        queue: RabbitMqConstants.QueueName,
        durable: true,
        exclusive: false,
        autoDelete: false,
        arguments: queueArgs);

    await channel.QueueBindAsync(
        RabbitMqConstants.QueueName,
        RabbitMqConstants.ExchangeName,
        "order.#");

    Console.WriteLine("\t\\\\\\Infrastructure ready!///");
}