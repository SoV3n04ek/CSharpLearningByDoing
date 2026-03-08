namespace Order.Contracts;

public static class RabbitMqConstants
{
    public const string ExchangeName = "orders-exchange";
    public const string QueueName = "hello-test-queue";
    public const string DlxExchange = "dlx-exchange";
    public const string DlxQueue = "dead-message-queue";
    public const string RoutingKey = "order.created.v1";
    public const string DeadLetterRoutingKey = "dead-letter";
}