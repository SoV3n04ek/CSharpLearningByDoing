using System.Diagnostics;

public class Program
{
    // Note: Task return type to make the entry point asynchronous
    static async Task Main(string[] args)
    {
        string url = "https://google.com";

        using (HttpClient client = new HttpClient())
        {
            Console.WriteLine("Starting download...");

            Stopwatch sw = Stopwatch.StartNew();

            try
            {
                // The 'await' keyword yields control back to the system
                // until GetStringAsync finishes
                string content = await client.GetStringAsync(url);

                sw.Stop();

                // Manual calculations based on constraints
                long length = content.Length;
                long timeMs = sw.ElapsedMilliseconds;

                // Formula: Speed = Length / Time
                double speed = timeMs > 0 ? (double)length / timeMs : length;

                Console.WriteLine("--- Results ---");
                Console.WriteLine($"Characters received: {length}");
                Console.WriteLine($"Time taken: {timeMs} ms");
                Console.WriteLine($"Manual Speed Calc: {speed} chars/ms");
                Console.WriteLine($"Snippet of content: {content.Substring(0, 50)}...");
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
            }
        }
    }
}