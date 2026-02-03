/*
 * 
 * Project Overview
Build a mini-reporting engine that processes a collection of "Financial Transactions." You will use Records to ensure data integrity and LINQ to perform complex filtering and transformations.

Technical Requirements
1. The Data Model (Records)
Define a public record Transaction with the following properties: Id (Guid), Category (string), Amount (decimal), and Timestamp (DateTime).

Use the Positional Syntax for the record (the one-liner definition).

Challenge: Implement a TaxedTransaction record that inherits from Transaction and adds a TaxAmount property.

2. Immutability & Mutation
In your Main method, create an instance of a transaction.

Demonstrate Non-destructive Mutation using the with expression to change only the Amount of a transaction while keeping the rest of the data intact.

Show Value Equality: Create two different record instances with identical data and prove they are equal using ==, then explain why this would fail with a standard class.

3. The LINQ Engine (Fluent & Query Syntax)
Create a list of 10+ transactions and implement the following logic:

Filtering: Find all transactions in the "Groceries" category with an amount > 50.

Transformation (Projection): Create a new anonymous type or a record that only contains the Id and a formatted String of the amount (e.g., "$50.00").

Aggregation: Calculate the total sum of all transactions using .Sum().

Grouping: Group transactions by Category and display the count of transactions in each group.

Deferred Execution Proof: Add a new transaction to your list after defining a LINQ query but before iterating over it. Observe if the new item appears in the results.

*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqRecords.DataProcessor.Lab
{
    // TASK 1: Records & Inheritance
    // Positional record: provides Constructor, Deconstructor, and Value Equality automatically.
    public record Transaction(Guid Id, string Category, decimal Amount, DateTime Timestamp);

    // Records can inherit from other records.
    public record TaxedTransaction(Guid Id, string Category, decimal Amount, DateTime Timestamp, decimal TaxAmount)
        : Transaction(Id, Category, Amount, Timestamp);

    class Program
    {
        static void Main()
        {
            Console.WriteLine("=== TASK 2: IMMUTABILITY & EQUALITY ===");

            var id = Guid.NewGuid();
            var t1 = new Transaction(id, "Groceries", 50.00m, DateTime.Now);
            var t2 = new Transaction(id, "Groceries", 50.00m, DateTime.Now);

            // Value Equality: Records compare properties, not memory addresses.
            Console.WriteLine($"t1 == t2: {t1 == t2}"); // True
            Console.WriteLine($"ReferenceEquals: {ReferenceEquals(t1, t2)}"); // False

            // Non-destructive Mutation (with expression)
            // t1 remains unchanged. t3 is a new object.
            var t3 = t1 with { Amount = 100.00m };
            Console.WriteLine($"Original: {t1.Amount}, Mutated: {t3.Amount}");


            Console.WriteLine("\n=== TASK 3: LINQ OPERATIONS ===");

            var history = new List<Transaction>
            {
                new (Guid.NewGuid(), "Tech", 1200.00m, DateTime.Now.AddDays(-1)),
                new (Guid.NewGuid(), "Groceries", 15.50m, DateTime.Now.AddDays(-2)),
                new (Guid.NewGuid(), "Groceries", 65.00m, DateTime.Now.AddDays(-1)),
                new (Guid.NewGuid(), "Entertainment", 100.00m, DateTime.Now.AddDays(-3)),
                new (Guid.NewGuid(), "Tech", 500.00m, DateTime.Now.AddDays(-10))
            };

            // Filtering & Projection
            var expensiveGroceries = history
                .Where(t => t.Category == "Groceries" && t.Amount > 50)
                .Select(t => new { t.Id, DisplayAmount = t.Amount.ToString("C") });

            Console.WriteLine("Expensive Groceries found:");
            foreach (var item in expensiveGroceries) Console.WriteLine($"- {item.DisplayAmount}");

            // Grouping & Aggregation
            var report = history
                .GroupBy(t => t.Category)
                .Select(group => new {
                    Category = group.Key,
                    Total = group.Sum(t => t.Amount),
                    Count = group.Count()
                });

            foreach (var r in report)
                Console.WriteLine($"Category: {r.Category}, Total: {r.Total}, Count: {r.Count}");


            Console.WriteLine("\n=== TASK 4: DEFERRED EXECUTION ===");

            // Query is DEFINED here, but not EXECUTED.
            var recentTransactions = history.Where(t => t.Timestamp > DateTime.Now.AddDays(-5));

            // We modify the underlying collection AFTER the query definition.
            var newItem = new Transaction(Guid.NewGuid(), "NewItem", 999m, DateTime.Now);
            history.Add(newItem);

            // Execution happens HERE during the foreach loop.
            Console.WriteLine("Iteration results (NewItem should appear):");
            foreach (var t in recentTransactions)
            {
                Console.WriteLine($"- {t.Category}: {t.Amount}");
            }

            /* FINDINGS:
               NewItem APPEARS in the results because LINQ (IEnumerable) uses deferred execution. 
               The query acts as a 'recipe' that is followed only when you iterate (foreach, ToList, etc.).
               If we had called .ToList() on the query definition, NewItem would NOT be there.
            */
        }
    }
}

/* Discussion Points (Interview Prep)
Records vs Classes: Why use a record for a DTO (Data Transfer Object) instead of a class?
(Answer: Value equality makes unit testing easier and prevents accidental side effects).

IEnumerable vs IQueryable: When does LINQ run on the CPU (RAM) vs when is it translated to SQL?

Reference Equality: How do you force a Record to check for reference equality if needed? (ReferenceEquals(a, b)).

Performance: Is foreach faster than .ForEach() or a LINQ .Select()? 
(Answer: Standard foreach is generally slightly faster and easier to debug, though LINQ is more expressive).
*/