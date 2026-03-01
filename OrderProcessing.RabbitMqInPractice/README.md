# 📌 TECHNICAL ASSIGNMENT

## Production-Grade Order Processing with RabbitMQ

### Stack: .NET 10 + ASP.NET Core Web API + RabbitMQ

### Timebox: 40 minutes

### Objective: Master Core RabbitMQ Fundamentals for Commercial Systems

---

# 1️⃣ Business Context (Commercial Scenario)

You are a backend developer in a mid-sized e-commerce company.

The current monolithic API processes orders synchronously:

* Save order to database
* Send confirmation email
* Update inventory
* Publish analytics event

This leads to:

* Slow response times
* High coupling
* Poor fault tolerance
* Risk of losing operations on crash

The CTO requests a refactor to a **message-driven architecture using RabbitMQ**.

---

# 2️⃣ Architectural Goal

Transform synchronous order processing into an asynchronous, reliable workflow:

```
Client
  ↓
Order API (Producer)
  ↓
RabbitMQ Exchange
  ↓
Queue
  ↓
Worker Service (Consumer)
  ↓
Email + Inventory + Analytics
```

System must guarantee:

* At-least-once delivery
* No message loss on restart
* Manual acknowledgment control
* Proper durability configuration
* Clean separation of concerns

---

# 3️⃣ Solution Constraints

You MUST use:

* .NET 10
* ASP.NET Core Web API
* Worker Service (BackgroundService)
* Official RabbitMQ.Client package
* Docker for RabbitMQ
* Dependency Injection
* Clean Architecture principles

You MUST NOT:

* Use MassTransit
* Use EasyNetQ
* Use external abstraction frameworks
* Put RabbitMQ code inside controller logic

---

# 4️⃣ Functional Requirements

---

## A. Order API (Producer)

### Endpoint

```
POST /api/orders
```

### Request DTO

```json
{
  "customerId": "string",
  "items": [
    {
      "productId": "string",
      "quantity": 2
    }
  ]
}
```

### Validation

* customerId required
* items must not be empty
* quantity > 0

### Processing Steps

1. Generate `OrderId` (GUID)
2. Save order to in-memory store (EF Core InMemory or static collection)
3. Publish `OrderCreated` message to RabbitMQ
4. Return `202 Accepted`
5. Response body:

```json
{
  "orderId": "guid",
  "status": "Processing"
}
```

⚠️ Do NOT:

* Send email
* Update inventory
* Perform long-running tasks in controller

---

## B. Message Contract (Shared Project)

Create separate project:

```
Order.Contracts
```

### OrderCreated Event

```csharp
public class OrderCreated
{
    public Guid OrderId { get; init; }
    public string CustomerId { get; init; }
    public DateTime CreatedAtUtc { get; init; }
}
```

Message must be serialized as JSON.

---

# 5️⃣ RabbitMQ Topology (MUST MATCH EXACTLY)

### Exchange

* Name: `order.exchange`
* Type: `direct`
* Durable: true
* AutoDelete: false

### Queue

* Name: `order.processing.queue`
* Durable: true
* Exclusive: false
* AutoDelete: false

### Routing Key

```
order.created
```

### Binding

Bind queue to exchange using routing key above.

---

# 6️⃣ Publisher Implementation Requirements

You MUST implement:

### Connection Management

* Single persistent IConnection (Singleton)
* Channels created per publish or scoped properly
* Proper disposal

### Message Publishing

* Set message as persistent:

```csharp
properties.Persistent = true;
```

* Use mandatory flag if possible
* Log publish confirmation

### Abstraction

Create:

```csharp
public interface IMessagePublisher
{
    Task PublishAsync<T>(T message, string routingKey);
}
```

RabbitMQ implementation must live in:

```
Order.Infrastructure
```

Controller must depend only on `IMessagePublisher`.

---

# 7️⃣ Consumer (Worker Service)

Create:

```
Order.Worker
```

Use:

```csharp
BackgroundService
```

### Configuration

* Connect to same RabbitMQ
* Declare exchange and queue (idempotent)
* Set QoS:

```csharp
channel.BasicQos(0, 1, false);
```

(prefetch count = 1)

---

## Message Consumption Requirements

Use:

```csharp
autoAck: false
```

When message received:

1. Deserialize JSON
2. Simulate:

   * Email sending (Console.WriteLine)
   * Inventory update (Console.WriteLine)
3. Log success
4. Call:

```csharp
channel.BasicAck(...)
```

---

## Failure Handling

If exception occurs:

* Log error
* Do NOT acknowledge
* Either:

  * Requeue message
    OR
  * Configure basic dead-letter exchange

Choose ONE approach and implement correctly.

---

# 8️⃣ Non-Functional Requirements

---

## Durability

Demonstrate understanding of:

* Durable exchange
* Durable queue
* Persistent messages

Explain in README:

What happens if:

* API crashes after publish
* Worker crashes before ack
* RabbitMQ restarts

---

## Delivery Semantics

Explain:

* Why this setup guarantees **at-least-once delivery**
* Why it does NOT guarantee exactly-once
* How idempotency could be implemented (concept explanation only)

---

## Graceful Shutdown

Ensure:

* Worker stops cleanly
* Channel and connection disposed properly
* No unacked message loss on shutdown

---

# 9️⃣ Dependency Injection Design

Register:

```
IConnection → Singleton
IMessagePublisher → Scoped or Singleton
```

Ensure:

* No new connection per request
* No connection per publish

---

# 🔟 Docker Setup

Create:

```yaml
version: '3.8'
services:
  rabbitmq:
    image: rabbitmq:3-management
    ports:
      - "5672:5672"
      - "15672:15672"
```

Access management UI:

[http://localhost:15672](http://localhost:15672)
guest / guest

---

# 1️⃣1️⃣ Testing Scenario

After implementation:

1. Start Docker
2. Start API
3. Start Worker
4. Send POST request
5. Verify:

   * Message visible in RabbitMQ
   * Worker processes message
   * Ack removes message

Kill worker mid-processing:

* Restart
* Verify message is reprocessed

---

# 1️⃣2️⃣ Project Structure (Strict)

```
/src
  /Order.Api
  /Order.Worker
  /Order.Contracts
  /Order.Infrastructure
docker-compose.yml
README.md
```

---

# 1️⃣3️⃣ README Must Include

Explain:

* RabbitMQ topology
* Exchange vs Queue role
* Direct exchange routing
* Manual acknowledgment
* Prefetch count meaning
* Why persistent messages matter
* Delivery guarantees
* Failure scenarios

---

# 1️⃣4️⃣ Evaluation Criteria

You succeed if:

✔ Messages survive service restart
✔ Worker does not auto-ack
✔ No RabbitMQ logic inside controller
✔ Proper DI usage
✔ Clean separation of layers
✔ Correct topology declaration
✔ At-least-once delivery demonstrated

---

# 1️⃣5️⃣ Stretch Goals (If Time Allows)

* Implement Dead Letter Exchange
* Add retry counter header
* Structured logging
* Health check endpoint
* Multiple consumers (competing consumer pattern)

---

# 🎯 Final Learning Outcomes

After completing this task, you will understand:

* AMQP model fundamentals
* Producer/Consumer separation
* Durable messaging
* Manual acknowledgment
* Prefetch (QoS)
* Delivery guarantees
* Failure recovery
* Clean RabbitMQ integration in ASP.NET

---

**Start implementation immediately. Focus on correctness, reliability, and proper messaging fundamentals. Avoid overengineering.**

**TECHNICAL ASSIGNMENT PROMPT**

---

# 🧩 Commercial-Style Technical Assignment

## Topic: Reliable Order Processing with RabbitMQ in ASP.NET (.NET 10)

### Estimated Time: 40 minutes

### Goal: Learn and apply fundamental, production-relevant RabbitMQ patterns in a real-world ASP.NET application.

---

## 🎯 Business Context (Real-World Scenario)

You are developing a backend system for a small e-commerce company.

Currently, the `OrderService` API processes orders synchronously:

* Saves order to database
* Sends confirmation email
* Updates inventory
* Logs analytics event

This causes:

* Slow API responses
* Tight coupling
* Risk of losing background operations if the API crashes

The CTO requires an architectural improvement:

> Introduce **RabbitMQ-based asynchronous processing** to decouple order creation from post-processing tasks while ensuring reliability, scalability, and failure safety.

---

# 🏗️ Your Mission

Design and implement a **production-style message-driven architecture** using:

* **.NET 10**
* **ASP.NET Core Web API**
* **RabbitMQ**
* Official RabbitMQ.Client library
* Docker (for RabbitMQ container)

You must implement a **reliable, resilient message workflow** using RabbitMQ best practices.

---

# 📦 Functional Requirements

## 1️⃣ Order API (Producer)

Create an ASP.NET Web API project:

### Endpoint:

```
POST /api/orders
```

### Input:

```json
{
  "customerId": "string",
  "items": [
    {
      "productId": "string",
      "quantity": 2
    }
  ]
}
```

### Behavior:

1. Validate input
2. Generate `OrderId`
3. Persist order in memory (or minimal EF Core InMemory)
4. Publish an `OrderCreated` event to RabbitMQ
5. Return `202 Accepted`

⚠️ Do NOT perform email or inventory updates in controller.

---

## 2️⃣ Messaging Design (RabbitMQ)

You must implement:

### Exchange

* Type: `direct`
* Name: `order.exchange`
* Durable: true

### Queue

* Name: `order.processing.queue`
* Durable: true
* Bind to exchange with routing key: `order.created`

### Message

Structure:

```json
{
  "orderId": "guid",
  "customerId": "string",
  "createdAtUtc": "datetime"
}
```

### Required RabbitMQ Best Practices

You MUST implement:

* Durable exchange
* Durable queue
* Persistent messages (`IBasicProperties.Persistent = true`)
* Manual acknowledgments (no auto-ack)
* Prefetch count (QoS) set to 1
* Proper exception handling
* Requeue strategy or Dead Letter configuration (basic version acceptable)
* Graceful shutdown support
* Connection + Channel reuse via DI (Singleton connection)

---

## 3️⃣ Background Worker (Consumer)

Create a separate Worker Service project:

* HostedService / BackgroundService
* Connects to same RabbitMQ instance
* Subscribes to `order.processing.queue`

### Behavior:

When message received:

1. Simulate:

   * Email sending (Console.WriteLine)
   * Inventory update (Console.WriteLine)
2. Log success
3. Acknowledge message

If failure occurs:

* Do NOT ack
* Requeue or dead-letter (choose one and implement correctly)

---

# 🔐 Non-Functional Requirements

Your solution must demonstrate understanding of:

### ✔ Reliability

* No message loss if API restarts
* No message loss if consumer crashes before ack

### ✔ Idempotency (Conceptual)

Explain in comments how duplicate processing could be handled.

### ✔ Clean Architecture

* Separate Messaging infrastructure layer
* Strongly typed message contracts
* No RabbitMQ code in Controllers

### ✔ Dependency Injection

Register:

* IConnection (Singleton)
* IModel (Scoped or transient properly)
* IMessagePublisher abstraction

---

# 🐳 Infrastructure

Provide `docker-compose.yml`:

```yaml
rabbitmq:
  image: rabbitmq:3-management
  ports:
    - "5672:5672"
    - "15672:15672"
```

Management UI:
[http://localhost:15672](http://localhost:15672)
guest / guest

---

# 📂 Expected Project Structure

```
/src
  /Order.Api
  /Order.Worker
  /Order.Contracts
  /Order.Infrastructure
docker-compose.yml
```

---

# 📋 Deliverables

Your final solution must include:

1. ASP.NET API project
2. Worker project
3. Docker compose
4. Clear README explaining:

   * How to run
   * How RabbitMQ topology works
   * Why durability settings matter
   * What happens if:

     * API crashes after publish
     * Worker crashes before ack

---

# 🧠 Knowledge You Must Apply

This assignment forces you to understand:

* AMQP model (Exchange → Binding → Queue → Consumer)
* Direct exchange routing
* Durable vs transient queues
* Persistent messages
* Acknowledgment modes
* Consumer prefetch
* Message-driven architecture
* At-least-once delivery
* Competing consumers pattern
* Clean separation of concerns

---

# 🏁 Stretch Goal (Optional if time remains)

Implement:

* Dead Letter Exchange
* Retry count header
* Basic retry mechanism
* Structured logging

---

# ⏱ Execution Constraint

You must complete:

* Basic working system
* With durability + manual ack
* In under 40 minutes

Prioritize correctness over polish.

---

# 🎓 Outcome

By completing this assignment you will have:

* Built a production-style asynchronous architecture
* Practiced real-world RabbitMQ patterns
* Implemented correct reliability settings
* Understood how message brokers solve scalability and fault tolerance problems in commercial systems

---

**Start implementing. Do not overengineer. Focus on core RabbitMQ fundamentals and clean architecture.**
