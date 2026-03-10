namespace Order.Contracts;

// The top-level request sent by the client
public record OrderRequest(
    string CustomerId,
    List<OrderItemRequest> Items);

// Individual items in the order
public record OrderItemRequest(
    string ProductId,
    int Quantity);