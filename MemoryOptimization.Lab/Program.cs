using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

/*
 * TOPIC: CLR Garbage Collector (GC), Memory Optimization, and Resource Management.
 * * LEARNING GOAL: 
 * 1. Differentiate between Heap (Class) and Stack (Struct) allocation costs.
 * 2. Understand GC Pressure and how Object Pooling (ArrayPool) mitigates it.
 * 3. Master the IDisposable pattern to prevent unmanaged resource leaks.
 * 4. Observe the impact of Boxing and the Large Object Heap (LOH).
 * * TECHNICAL TASK: "The High-Frequency Logger Lab"
 * - Step 1: Run a "Naive" simulation using Classes (Heap) and observe memory spikes.
 * - Step 2: Optimize using Readonly Structs (Stack) to reduce GC overhead.
 * - Step 3: Implement Object Pooling for large buffers to avoid LOH fragmentation.
 * - Step 4: Implement a robust IDisposable pattern for a File Wrapper.
 */

namespace MemoryOptimization.Lab
{
    // --- STEP 1: NAIVE IMPLEMENTATION (The GC Pressure Maker) ---
    // Classes are Reference Types. 1 million instances = 1 million objects on the Heap.
    // Each object has a header (8-16 bytes) + fields + alignment.
    public class DataPointClass
    {
        public int Id { get; set; }
        public double Value { get; set; }
        public DateTime Timestamp { get; set; }
    }

    // --- STEP 2: OPTIMIZATION (Stack Allocation) ---
    // Structs are Value Types. In a List<T> or Array, they are stored contiguously.
    // This is cache-friendly and doesn't require individual object headers.
    public readonly struct DataPointStruct
    {
        public int Id { get; init; }
        public double Value { get; init; }
        public DateTime Timestamp { get; init; }

        public DataPointStruct(int id, double value)
        {
            Id = id;
            Value = value;
            Timestamp = DateTime.UtcNow;
        }
    }

    // --- STEP 4: IDISPOSABLE PATTERN (The Resource Protector) ---
    public class UnmanagedFileWriter : IDisposable
    {
        private StreamWriter _writer;
        private bool _disposed = false;

        public UnmanagedFileWriter(string path)
        {
            _writer = new StreamWriter(path);
        }

        public void WriteLog(string message) => _writer.WriteLine(message);

        // Standard IDisposable implementation
        public void Dispose()
        {
            Dispose(true);
            // Optimization: Tells the GC this object is already clean. 
            // It skips the 'Finalization Queue', saving a Gen 2 promotion.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Clean managed resources
                    _writer?.Dispose();
                }
                // Clean unmanaged resources here (if any)
                _disposed = true;
            }
        }

        // Finalizer (Safety Net)
        ~UnmanagedFileWriter() => Dispose(false);
    }

    class Program
    {
        private const int Iterations = 1_000_000;

        static void Main(string[] args)
        {
            Console.WriteLine("=== Memory & GC Performance Lab ===\n");

            RunNaiveClassTest();
            RunOptimizedStructTest();
            RunArrayPoolTest();
            RunDisposableTest();

            Console.WriteLine("\n=== Lab Completed ===");
        }

        private static void RunNaiveClassTest()
        {
            GC.Collect(); // Start clean
            long before = GC.GetTotalMemory(true);
            Stopwatch sw = Stopwatch.StartNew();

            var list = new List<DataPointClass>(Iterations);
            for (int i = 0; i < Iterations; i++)
            {
                // Every 'new' here is a Heap allocation
                list.Add(new DataPointClass { Id = i, Value = i * 1.5 });
            }

            sw.Stop();
            long after = GC.GetTotalMemory(false);

            Console.WriteLine($"[NAIVE CLASS] Memory: {(after - before) / 1024 / 1024} MB | Time: {sw.ElapsedMilliseconds}ms");
            // Explanation: High memory usage because of object headers and metadata for 1M references.
        }

        private static void RunOptimizedStructTest()
        {
            GC.Collect();
            long before = GC.GetTotalMemory(true);
            Stopwatch sw = Stopwatch.StartNew();

            var list = new List<DataPointStruct>(Iterations);
            for (int i = 0; i < Iterations; i++)
            {
                // No 'new' on the heap. Data is copied directly into the List's internal array.
                list.Add(new DataPointStruct(i, i * 1.5));
            }

            sw.Stop();
            long after = GC.GetTotalMemory(false);

            Console.WriteLine($"[STRUCT TEST] Memory: {(after - before) / 1024 / 1024} MB | Time: {sw.ElapsedMilliseconds}ms");
            // Explanation: significantly lower memory. The data is packed tightly in one big block.
        }

        private static void RunArrayPoolTest()
        {
            Console.WriteLine("\n[POOLING] Processing large buffer (>85KB)...");

            // Large objects (>85,000 bytes) go to Large Object Heap (LOH).
            // LOH is not compacted, leading to fragmentation.
            // ArrayPool lets us RENT memory instead of allocating/deleting.

            var pool = ArrayPool<byte>.Shared;
            byte[] buffer = pool.Rent(100_000); // Rent 100KB

            try
            {
                // Simulate work with buffer
                Random.Shared.NextBytes(buffer);
                Console.WriteLine("Buffer rented and used safely.");
            }
            finally
            {
                // Return to pool so the next request can reuse it. No GC required!
                pool.Return(buffer);
                Console.WriteLine("Buffer returned to ArrayPool.");
            }
        }

        private static void RunDisposableTest()
        {
            Console.WriteLine("\n[IDISPOSABLE] Testing resource cleanup...");
            string path = "test_log.txt";

            // The 'using' declaration ensures Dispose() is called even if an error occurs.
            using (var writer = new UnmanagedFileWriter(path))
            {
                writer.WriteLog("Testing IDisposable...");
            }

            // File is closed here.
            File.Delete(path);
            Console.WriteLine("File created, written to, and disposed correctly.");
        }
    }
}