TASK: I expect you to handle the thread-safety issues without me holding your hand on the `lock` syntax.

---

## Technical Specification: High-Throughput Flash Sale Order Processor

### **1. Background**

During our upcoming "Black Friday" event, we expect a burst of approximately 10,000 orders per minute. Our current sequential processing is too slow, causing a bottleneck in the database. We need a high-performance **In-Memory Pre-Processor** to handle deduplication and validation before the orders hit the persistence layer.

### **2. Objective**

Build a thread-safe processing engine that ingests a batch of raw orders, deduplicates them in real-time, validates them against a constrained external API, and prepares a summary report.

---

### **3. Functional Requirements**

#### **3.1 Real-Time Deduplication & In-Memory Inventory**

* **Input:**An `IEnumerable < RawOrder >` containing potential duplicates (same `OrderId` or same `UserTransactionId`).
* **Requirement:**Use a high-performance concurrent collection to ensure that each unique `OrderId` is processed exactly once.
* **Requirement:**Maintain an in-memory "Product Category Counter." If an order contains 3 items of "Electronics," the "Electronics" category total must be incremented across all concurrent tasks.
* **Constraint:**You must** avoid** using a global `lock` for the counters, as this will kill throughput on our 10 - core processors.Use atomic update patterns.

#### **3.2 Throttled Address Validation**

* **Requirement:**Each order must be validated via the `LegacyShippingProvider` API.
* **The Constraint:**The legacy provider will trigger an IP - ban if we exceed * *4 concurrent requests**.
* **Implementation Detail:**You must use the `Parallel` library to process the batch. You are required to explicitly configure the engine to never exceed a **Degree of Parallelism of 4** for this specific stage.
* **Simulation:**The validation call should be a simulated I/O delay of 200ms.

#### **3.3 Performance Telemetry**

* **Requirement:**The engine must capture and output:
1.Total time elapsed for the batch.
2. The number of rejected (duplicate) orders.
3. The final state of the Category Counter.
4. The **Peak Thread Count** observed during the execution (to verify the ThreadPool isn't being starved).



---

### **4. Non-Functional Requirements (Acceptance Criteria)**

1. **Thread Safety:**No `Race Conditions`. The final count of "Electronics" must be mathematically perfect every time the test runs.
2. **No Deadlocks:**The application must not hang. Ensure that any waiting on the semaphore/collection is handled without blocking the ThreadPool indefinitely.
3. **Memory Efficiency:**Avoid creating a new `Task` for every single order if `Parallel.ForEach` can partition the work more efficiently.
4. **Error Handling:**If the "External API" simulation fails (randomly), the engine should log the error but continue processing the rest of the batch.

---

### **5. Technical Constraints for the Lab**

* **Target Framework:** .NET 9.
* **Collections:**Use `System.Collections.Concurrent`.
***Parallelism:**Use `System.Threading.Tasks.Parallel` with `ParallelOptions`.
* **Synchronization:**Use `SemaphoreSlim` or `ParallelOptions.MaxDegreeOfParallelism` (compare which is better for this business case).
