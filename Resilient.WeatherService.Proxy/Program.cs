using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

/*
 * TOPIC: Dependency Injection (DI), Decorator Pattern, and Resilience.
 * * LEARNING GOAL: 
 * 1. Understand how DI manages service lifetimes (Singleton vs Transient).
 * 2. Master the Decorator Pattern to add Caching/Logging without modifying existing code.
 * 3. Implement manual "Retry" logic to handle unstable external dependencies.
 * * TECHNICAL TASK:
 * 1. Implement a 'WeatherSettings' record bound via the Options Pattern.
 * 2. Create an 'ExternalWeatherService' that randomly fails (simulating a real-world API).
 * 3. Create a 'CachedWeatherService' (Decorator) that wraps the external service to prevent redundant calls.
 * 4. Manually implement a Retry-with-Fallback mechanism in the Main logic.
 */

// --- 1. Models & Configuration ---
public record WeatherSettings
{
    public string ApiUrl { get; set; } = "https://api.unstable-weather.com";
    public int MaxRetries { get; set; } = 3;
}

public record WeatherReport(string City, double Temperature, string Status);

// --- 2. Service Abstraction ---
public interface IWeatherService
{
    Task<WeatherReport> GetWeatherAsync(string city);
}

// --- 3. The "Flaky" Implementation ---
public class ExternalWeatherService : IWeatherService
{
    private readonly WeatherSettings _settings;

    public ExternalWeatherService(IOptions<WeatherSettings> settings)
    {
        _settings = settings.Value;
    }

    public async Task<WeatherReport> GetWeatherAsync(string city)
    {
        Console.WriteLine($"[API] Calling {_settings.ApiUrl} for {city}...");

        // Simulate a 50% failure rate
        if (Random.Shared.Next(0, 2) == 0)
        {
            throw new Exception("Temporary API Connection Failure!");
        }

        await Task.Delay(500); // Simulate network latency
        return new WeatherReport(city, 22.5, "Sunny");
    }
}

// --- 4. The Decorator Pattern (Caching) ---
// This class wraps the "real" service. The client doesn't know it's being cached.
public class CachedWeatherService : IWeatherService
{
    private readonly IWeatherService _innerService;
    private readonly Dictionary<string, WeatherReport> _cache = new();

    public CachedWeatherService(IWeatherService innerService)
    {
        _innerService = innerService;
    }

    public async Task<WeatherReport> GetWeatherAsync(string city)
    {
        if (_cache.TryGetValue(city, out var cachedReport))
        {
            Console.WriteLine($"[CACHE] Returning saved data for {city}.");
            return cachedReport;
        }

        var report = await _innerService.GetWeatherAsync(city);
        _cache[city] = report;
        return report;
    }
}

class Program
{
    static async Task Main()
    {
        Console.WriteLine("=== STARTING RESILIENT WEATHER PROXY ===\n");

        // --- STEP 1: Dependency Injection Setup ---
        var services = new ServiceCollection();

        // Bind settings (usually from appsettings.json)
        services.Configure<WeatherSettings>(s => {
            s.MaxRetries = 3;
            s.ApiUrl = "https://prod.weather.com";
        });

        // Register the Base Service
        services.AddSingleton<ExternalWeatherService>();

        // Register the Decorator manually
        // We inject the "Real" service into the "Cached" service.
        services.AddSingleton<IWeatherService>(provider =>
            new CachedWeatherService(provider.GetRequiredService<ExternalWeatherService>()));


        var serviceProvider = services.BuildServiceProvider();
        var weatherProxy = serviceProvider.GetRequiredService<IWeatherService>();

        // --- STEP 2: Manual Testing with Resilience (Retry Logic) ---
        string targetCity = "Kyiv";
        WeatherReport finalReport = null;

        int currentTry = 0;
        int maxAttempts = serviceProvider.GetRequiredService<IOptions<WeatherSettings>>().Value.MaxRetries;

        while (currentTry < maxAttempts)
        {
            try
            {
                currentTry++;
                // This call goes through: Cache -> External API
                finalReport = await weatherProxy.GetWeatherAsync(targetCity);
                break; // Success! Exit the loop.
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[RETRY] Attempt {currentTry} failed: {ex.Message}");
                if (currentTry >= maxAttempts)
                {
                    // --- STEP 3: Fallback Logic ---
                    Console.WriteLine("[FALLBACK] All retries failed. Returning offline data.");
                    finalReport = new WeatherReport(targetCity, 0.0, "Data Unavailable");
                }
                else
                {
                    await Task.Delay(1000); // Wait before retrying (Backoff)
                }
            }
        }

        Console.WriteLine($"\nFINAL RESULT: {finalReport.City} | {finalReport.Temperature}°C | {finalReport.Status}");

        // --- STEP 4: Verify Caching ---
        Console.WriteLine("\nTesting cache (Should be instant):");
        await weatherProxy.GetWeatherAsync(targetCity);
    }
}