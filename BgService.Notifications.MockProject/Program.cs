using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Entry point: build the host, register services, and run
var builder = Host.CreateApplicationBuilder(args);

// Wire up dependencies
builder.Services.AddScoped<IUserRepository, MockUserRepository>();
builder.Services.AddScoped<IEmailService, MockEmailService>();
builder.Services.AddSingleton<IMessageGenerator, LearningPathMessageGenerator>();

// Add the background worker
builder.Services.AddHostedService<SubscriptionReminderBackgroundService>();

using IHost host = builder.Build();
await host.RunAsync();

// Simple user record – represents a student with subscription info
public record User(
    int Id,
    string Email,
    string Name,
    DateTime SubscriptionEndDate,
    string PrimaryCourseCategory,
    DateTime? LastNotificationSentDate = null);

// Repository contract: who to notify and how to update them
public interface IUserRepository
{
    Task<List<User>> GetUsersForNotificationAsync(DateTime targetDate, int batchSize);
    Task UpdateNotificationDateAsync(int userId, DateTime date);
}

// Email sending contract
public interface IEmailService
{
    Task SendSubscriptionReminderAsync(User user, string message);
}

// Message generator contract – creates a personalised reminder
public interface IMessageGenerator
{
    string GenerateMessage(User user);
}

// Mock repository that holds data in memory (static list)
public class MockUserRepository : IUserRepository
{
    private static readonly List<User> _database = new();

    public MockUserRepository()
    {
        // Seed some test users if the list is empty
        if (!_database.Any()) Seed();
    }

    private void Seed()
    {
        for (int i = 1; i <= 20; i++)
        {
            // First 5 users expire in 3 days, the rest in 30 days
            var expiry = (i <= 5) ? DateTime.Today.AddDays(3) : DateTime.Today.AddDays(30);
            var category = i % 2 == 0 ? "C#" : "DevOps";
            _database.Add(new User(i, $"user{i}@example.com", $"Student {i}", expiry, category));
        }
    }

    public Task<List<User>> GetUsersForNotificationAsync(DateTime targetDate, int batchSize)
    {
        // Only pick users who expire on targetDate and haven't been notified today
        var result = _database
            .Where(u => u.SubscriptionEndDate.Date == targetDate.Date
                        && u.LastNotificationSentDate?.Date != DateTime.Today)
            .Take(batchSize)
            .ToList();

        return Task.FromResult(result);
    }

    public Task UpdateNotificationDateAsync(int userId, DateTime date)
    {
        var userIndex = _database.FindIndex(u => u.Id == userId);
        if (userIndex != -1)
        {
            var user = _database[userIndex];
            _database[userIndex] = user with { LastNotificationSentDate = date };
        }
        return Task.CompletedTask;
    }
}

// Generates messages based on the user's primary course category
public class LearningPathMessageGenerator : IMessageGenerator
{
    public string GenerateMessage(User user) => user.PrimaryCourseCategory switch
    {
        "C#" => $"Hey {user.Name}, your C# journey is about to pause! Renew now.",
        "DevOps" => $"Your cloud labs are expiring in 3 days, {user.Name}. Don't lose your progress!",
        _ => $"Hi {user.Name}, your subscription ends in 3 days."
    };
}

// Mock email service that just logs messages (with a small delay)
public class MockEmailService : IEmailService
{
    private readonly ILogger<MockEmailService> _logger;
    public MockEmailService(ILogger<MockEmailService> logger) => _logger = logger;

    public async Task SendSubscriptionReminderAsync(User user, string message)
    {
        await Task.Delay(200); // simulate network
        _logger.LogInformation("\x1b[32m[EMAIL SENT]\x1b[0m To: {Email} | Message: {Msg}", user.Email, message);
    }
}

// Background service that runs periodically and sends reminders
public class SubscriptionReminderBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SubscriptionReminderBackgroundService> _logger;
    private int _jobId = 1;

    public SubscriptionReminderBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<SubscriptionReminderBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Sentinel Service is starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("[Job #{Id}] Checking for expiring subscriptions...", _jobId);

            using (var scope = _scopeFactory.CreateScope())
            {
                var repo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                var emailer = scope.ServiceProvider.GetRequiredService<IEmailService>();
                var generator = scope.ServiceProvider.GetRequiredService<IMessageGenerator>();

                bool hasMore = true;
                while (hasMore)
                {
                    // Fetch users in batches of 2 (target date = 3 days from now)
                    var users = await repo.GetUsersForNotificationAsync(DateTime.Today.AddDays(3), 2);

                    if (!users.Any())
                    {
                        hasMore = false;
                        continue;
                    }

                    _logger.LogInformation("[Job #{Id}] Processing batch of {Count} users...", _jobId, users.Count);

                    foreach (var user in users)
                    {
                        if (stoppingToken.IsCancellationRequested) break;

                        var message = generator.GenerateMessage(user);
                        await emailer.SendSubscriptionReminderAsync(user, message);
                        await repo.UpdateNotificationDateAsync(user.Id, DateTime.Now);
                    }
                }
            }

            _logger.LogInformation("[Job #{Id}] Completed. Sleeping for 10s...", _jobId++);
            await Task.Delay(10000, stoppingToken);
        }
    }
}