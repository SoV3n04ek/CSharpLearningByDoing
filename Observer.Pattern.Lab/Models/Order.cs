namespace Observer.Pattern.Lab.Models;

public record Order(
    Guid Id,
    string CustomerName,
    decimal TotalAmount,
    string Status);

public record PaymentFailed(
    Guid OrderId,
    string Reason);