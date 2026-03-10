TOPIC: Decoupled Systems via Observer and Message Broker Patterns
LEARNING GOAL: To implement a loosely coupled system where the core business logic (Order Processing) is unaware of side-effect handlers (Shipping, Notifications) by using both local events and a centralized broker simulation.

1. Domain Model
Create a record to represent the system state:

Order: Guid Id, string CustomerName, decimal TotalAmount, string Status.

2. Part 1: Standard Observer (C# Events)
Goal: Implement a direct notification system using native .NET events.

Task: Create an OrderService class.

Member: Define public event Action<Order>? OrderProcessed.

Method: Create ProcessOrder(string customer, decimal amount). Inside this method, create the Order object, set the status to "Processed", and invoke the event.

Observers: Create two classes: EmailNotifier and InventoryManager. Each should have a method that matches the Action<Order> signature and prints a unique message to the console when triggered.

Requirement: In Program.cs, instantiate the service and observers, subscribe the observers to the event, and call ProcessOrder.

3. Part 2: The "Broker" Pattern (In-Memory Simulation)
Goal: Remove the direct dependency between the Service and the Observers by introducing a middleman.

Task: Create a MessageBroker class.

Members: Use a Dictionary<string, List<Action<object>>> to store subscribers organized by "Topic".

Method - Subscribe(string topic, Action<object> handler): Adds a callback to the list for a specific topic.

Method - Publish(string topic, object data): Finds all callbacks for the topic and executes them.

Integration:

Modify OrderService to accept IMessageBroker in its constructor (Dependency Injection).

In ProcessOrder, instead of firing a C# event, call _broker.Publish("order.processed", order).

In Program.cs, ensure EmailNotifier and InventoryManager subscribe to the broker using the string "order.processed" rather than the service event.

4. Part 3: Advanced Broker Features (Filtering and Type Safety)
Goal: Enhance the broker to handle different types of events without "Magic Strings".

Task: Create a generic Publish<T>(T message) and Subscribe<T>(Action<T> handler) method.

Logic: Use typeof(T).FullName as the key in your dictionary to automatically route messages based on their Class type.

Experiment: Create a second event type PaymentFailed. Register a SupportTeam observer that only listens for PaymentFailed messages. Ensure they do not receive Order messages.

5. Success Requirements and Verification
Loose Coupling: The OrderService must not have any reference to EmailNotifier or InventoryManager.

Scalability: Demonstrate that you can add a third observer (e.g., SmsNotifier) by only changing the setup code in Program.cs without touching OrderService.

Memory Management: Implement an Unsubscribe method in the Broker and demonstrate that after calling it, the observer no longer reacts to new messages.

Console Output:

[OrderService] Order 123 created.

[Broker] Routing message 'Order' to 3 subscribers...

[EmailNotifier] Sending email to customer...

[InventoryManager] Reducing stock count...