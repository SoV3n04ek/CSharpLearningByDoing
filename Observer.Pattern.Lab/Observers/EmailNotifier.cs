using Observer.Pattern.Lab.Models;

namespace Observer.Pattern.Lab.Observers;

public class EmailNotifier
{
    // simulates an external communication service
    public void Handle(Order order)
    {
        // In real life, this would send an email using smtpClient. Here we just print to console
        Console.WriteLine($"\x1b[32m[EmailNotifier]\x1b[0m Sending confirmation to {order.CustomerName} for Order {order.Id}...");
        Console.WriteLine($"[EmailNotifier] Body: 'Thank you for your purchase of {order.TotalAmount:C}!'");
    }
}
