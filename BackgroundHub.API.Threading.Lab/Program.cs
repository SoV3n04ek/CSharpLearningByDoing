using BackgroundHub.API.Threading.Lab;
using BackgroundHub.API.Threading.Lab.Models;

var builder = WebApplication.CreateBuilder(args);

// Register our queue as a Singleton
builder.Services.AddSingleton<BackgroundTaskQueue>();

// Register our Worker as a Hosted Service
builder.Services.AddHostedService<Worker>();

var app = builder.Build();

app.MapPost("/signup", async (UserSignupTask task, BackgroundTaskQueue queue) =>
{   
    // Immediate DB Save (Logic here)
    // Push to background channel
    await queue.SignupChannel.Writer.WriteAsync(task);

    return Results.Ok("User created. Email and PDF are processing in background.");
});

app.MapPost("/log", (string msg, BackgroundTaskQueue queue) =>
{
    // Non-blocking try-write (Fastest possible logging)
    queue.LogChannel.Writer.TryWrite(new LogEntry(msg, DateTime.UtcNow, "Info"));
    return Results.Accepted();
});