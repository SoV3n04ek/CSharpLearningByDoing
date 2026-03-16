namespace BackgroundHub.API.Threading.Lab.Models;

public record UserSignupTask(int UserId, string Email);
public record LogEntry(string Message, DateTime Timestamp, string Level);
public record ExternalApiRequest(string Payload, TaskCompletionSource<string> ResponseTcs);