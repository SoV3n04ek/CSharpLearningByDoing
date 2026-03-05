Project Name: **`EfCore.Optimization.Lab`**

---

### **TOPIC: EF Core Performance & The "N+1" Investigation**

**LEARNING GOAL:** To visualize the "Hidden Costs" of Database I/O. You will prove, via console logs, how different LINQ methods change the underlying SQL and impact application scalability.

---

### **1. The Setup (Prerequisites)**

* **Target Framework:** .NET 8.0 or .NET 9.0 (Console App).
* **NuGet Packages:** * `Microsoft.EntityFrameworkCore.Sqlite` (Easiest for local labs).
* `Microsoft.Extensions.Logging.Console` (Critical for seeing the SQL).


* **Domain Model:**
* `Author`: `int Id`, `string Name`, `List<Book> Books`.
* `Book`: `int Id`, `string Title`, `int AuthorId`, `Author Author`.



---

### **2. The Core Infrastructure (DbContext)**

Configure your `DbContext` to log every SQL command to the console. This is your "Microscope."

```csharp
optionsBuilder
    .UseSqlite("Data Source=lab.db")
    .LogTo(Console.WriteLine, LogLevel.Information); // This is mandatory for this task

```

---

### **3. Task Milestones**

#### **Milestone 1: The "IQueryable vs IEnumerable" Trap**

* **Task:** Create a query that looks for Authors named "King".
* **Sub-task A (Efficient):** Apply the `.Where()` filter directly on the `DbSet` before calling `.ToList()`. Observe the SQL `WHERE` clause.
* **Sub-task B (The Leak):** Call `.ToList()` on the entire Authors table *first*, then apply `.Where()` on the resulting list.
* **Goal:** Observe that Sub-task B downloads the *entire* database into RAM before filtering.

#### **Milestone 2: Triggering the "N+1" Disaster**

* **Task:** Load all Authors from the database.
* **Logic:** Loop through the authors, and inside that loop, loop through their `Books` collection to print titles.
* **Goal:** Count the SQL queries. You should see 1 query for authors + 10 queries for books (Total: 11). This is the "N+1" performance killer.

#### **Milestone 3: Fixing with Eager Loading**

* **Task:** Rewrite the Milestone 2 query using `.Include(a => a.Books)`.
* **Goal:** Observe the SQL output. You should see a single `LEFT JOIN` query. Prove that the N+1 issue is gone.

#### **Milestone 4: Chaining with .ThenInclude()**

* **Task:** Add a `Publisher` class related to `Book`. Use `.Include(a => a.Books).ThenInclude(b => b.Publisher)`.
* **Goal:** Understand how to navigate deep object graphs in a single database round-trip.

#### **Milestone 5: The "Ultimate" Optimization (Projection)**

* **Task:** Create a small DTO: `public record AuthorDto(string Name, int BookCount)`.
* **Logic:** Use `.Select(a => new AuthorDto(a.Name, a.Books.Count))` on your query.
* **Goal:** Examine the SQL. Notice that EF Core generates a `COUNT()` aggregate and **does not** return all book columns. Explain why this is faster than Eager Loading for read-only views.

#### **Milestone 6: Explicit Loading**

* **Task:** Load a single Author *without* their books. Later in the code, use `context.Entry(author).Collection(a => a.Books).Load()` to fetch them.
* **Goal:** Understand when to use "On-Demand" loading without enabling dangerous Global Lazy Loading.

---

### **4. Manual Test Verification**

To "pass" this lab, your console output must clearly show:

1. **The "Trap":** A SQL statement selecting `*` from Authors without a filter.
2. **The "N+1":** A wall of 11 separate `SELECT` statements.
3. **The "Fix":** One large `SELECT` with a `JOIN`.
4. **The "Pro":** A `SELECT` with only two columns and a subquery/aggregate.

---

Next: explore **Global Query Filters** (for Soft Deletes) or **AsNoTracking** (for read-only performance)