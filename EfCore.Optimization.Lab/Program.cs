using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;


// Milestone 5 DTO

public class AppDbContext : DbContext
{
    public DbSet<Author> Authors { get; set; }
    public DbSet<Book> Books { get; set; }
    public DbSet<Publisher> Publishers { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseSqlite("Data Source=lab.db")
            // This line is the "Microscope" - it prints SQL to your console
            .LogTo(Console.WriteLine, LogLevel.Information)
            .EnableSensitiveDataLogging();
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        using var context = new AppDbContext();

        // Reset Database
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();

        await SeedData(context);

        Console.Clear();
        Console.WriteLine("--- MILESTONE 1A: Efficient IQueryable ---");
        var efficient = context.Authors.Where(a => a.Name == "Author 5").ToList();

        Console.WriteLine("\n--- MILESTONE 1B: The IEnumerable Trap (RAM Leak) ---");
        // .ToList() forces EVERYTHING into RAM first, then C# filters it.
        var leak = context.Authors.ToList().Where(a => a.Name == "Author 5");

        Console.WriteLine("\n--- MILESTONE 2: The N+1 Disaster ---");
        // We load authors, but NOT books.
        var authors = context.Authors.ToList();
        foreach (var author in authors)
        {
            // Accessing .Books here triggers a NEW query for EVERY author!
            Console.WriteLine($"Author: {author.Name}, Books: {author.Books.Count}");
        }

        Console.WriteLine("\n--- MILESTONE 3: Eager Loading (The Fix) ---");
        // One query with a JOIN
        var fixedAuthors = context.Authors.Include(a => a.Books).ToList();

        Console.WriteLine("\n--- MILESTONE 5: Projection (The Pro Way) ---");
        // Notice the SQL: No JOIN, just an optimized sub-select or aggregate
        var dtos = context.Authors
            .Select(a => new AuthorDto(a.Name, a.Books.Count))
            .ToList();

        Console.WriteLine("\n--- MILESTONE 6: Explicit Loading ---");
        var singleAuthor = context.Authors.First();
        // Manually loading related data only when needed
        context.Entry(singleAuthor).Collection(a => a.Books).Load();
    }

    static async Task SeedData(AppDbContext context)
    {
        var publisher = new Publisher { Name = "Global Press" };
        for (int i = 1; i <= 10; i++)
        {
            var author = new Author { Name = $"Author {i}" };
            for (int j = 1; j <= 5; j++)
            {
                author.Books.Add(new Book { Title = $"Book {i}-{j}", Publisher = publisher });
            }
            context.Authors.Add(author);
        }
        await context.SaveChangesAsync();
    }
}

public class Author
{
    public int Id { get; set; }
    public string Name { get; set; }
    public List<Book> Books { get; set; } = new();
}

public class Book
{
    public int Id { get; set; }
    public string Title { get; set; }
    public int AuthorId { get; set; }
    public Author Author { get; set; }
    public Publisher Publisher { get; set; }
}

public class Publisher
{
    public int Id { get; set; }
    public string Name { get; set; }
}


public record AuthorDto(string Name, int BookCount);