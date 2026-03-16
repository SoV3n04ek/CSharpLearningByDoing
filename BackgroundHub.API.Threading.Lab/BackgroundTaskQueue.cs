using BackgroundHub.API.Threading.Lab.Models;
using System.Threading.Channels;

namespace BackgroundHub.API.Threading.Lab;

public class BackgroundTaskQueue
{
    // Fire & Forget (Unbounded we don't want to lose signups)
    public Channel<UserSignupTask> SignupChannel { get; } = Channel.CreateUnbounded<UserSignupTask>();

    // Telemetry (Bounded to 1000 to prevent memory leaks if consumer slow down
    public Channel<LogEntry> LogChannel { get; } = Channel.CreateBounded<LogEntry>(1000);

    // Rate limiting (bounded to 50 for strict control)
    public Channel<ExternalApiRequest> ApiChannel { get; } = Channel.CreateBounded<ExternalApiRequest>(50);
}
