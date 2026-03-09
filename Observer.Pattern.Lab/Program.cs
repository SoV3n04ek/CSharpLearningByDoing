using Observer.Pattern.Lab.Models;
using Observer.Pattern.Lab.Observers;
using Observer.Pattern.Lab.Services;

var broker = new MessageBroker();
var orderService = new OrderService(broker);

var email = new EmailNotifier();
var inventory = new InventoryManager();
var support = new SupportTeam();

// Registering observers for different types of messages
broker.Subscribe<Order>(email.Handle);
broker.Subscribe<Order>(inventory.Handle);
broker.Subscribe<PaymentFailed>(support.Handle);

// Test 1: Successful Order
orderService.ProcessOrder("Ivan", 150.00m);

// Test 2: Simulating a Failure (you can add this method to your OrderService)
broker.Publish(new PaymentFailed(Guid.NewGuid(), "Insufficient funds"));