using Observer.Pattern.Lab.Models;

namespace Observer.Pattern.Lab.Observers;

public class InventoryManager
{
    public void Handle(Order order)
    {
        // In a real app: _db.Stock.Update(...)
        Console.WriteLine($"\x1b[34m[InventoryManager]\x1b[0m Checking stock for items in Order {order.Id}...");
        Console.WriteLine($"[InventoryManager] Result: Items reserved and moved to 'Packing' area.");
    }
}