using System;
using System.Net.Http;
using System.Diagnostics;
using System.Threading.Tasks;

public class AsyncMentor
{
    public static async Task RunExample()
    {
        string url = "https://api.github.com/_mobile_check";
        using var client = new HttpClient();
        // HttpClient requires a User-Agent header for many APIs
        client.DefaultRequestHeaders.Add("User-Agent", "CSharp-Mentor-App");

        Stopwatch sw = Stopwatch.StartNew();

        try
        {
            // Control is yielded here until the string is downloaded.
            string content = await client.GetStringAsync(url);

            sw.Stop();

            long L = content.Length;
            long T = sw.ElapsedMilliseconds;

            // Implementation of Formula: E = L / (T + 1)
            double efficiency = (double)L / (T + 1);

            Console.WriteLine($"Success! Bytes: {L}, Time: {T}ms");
            Console.WriteLine($"Efficiency Score: {efficiency:F2}");
        }
        catch (HttpRequestException ex)
        {
            // Exceptions in async methods are wrapped and re-thrown at the awaiter.
            Console.WriteLine($"Network Error: {ex.Message}");
        }
        finally
        {
            sw.Stop();
        }
    }
}