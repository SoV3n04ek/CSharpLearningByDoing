using Observer.Pattern.Lab.Models;

namespace Observer.Pattern.Lab.Observers;

public class SupportTeam
{
    public void Handle(PaymentFailed failure)
    {
        Console.WriteLine($"\x1b[31m[SupportTeam ALERT]\x1b[0m Manual intervention required for Order {failure.OrderId}!");
        Console.WriteLine($"[SupportTeam ALERT] Reason: {failure.Reason}");
    }
}