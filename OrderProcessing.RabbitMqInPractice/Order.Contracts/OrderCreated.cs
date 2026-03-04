namespace Order.Contracts;

public record OrderCreated(
    Guid OrderId,
    string CustomerId,
    DateTime CreatedAtUTC);