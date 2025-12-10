using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System;


/*
 * // --- BOXING ---
int i = 123;         // Value type: stored on the stack
object o = i;        // Boxing: A new object (the box) is allocated on the heap, 
                     // the value 123 is copied into it, and 'o' holds the reference.

// --- UNBOXING ---
int j = (int)o;      // Unboxing: The runtime checks if the object 'o' is an int 
                     // (it is), and the value 123 is copied back to the stack.

// --- HIDDEN BOXING (VERY COMMON) ---
// Using value types in a non-generic collection like ArrayList or a method 
// that accepts 'object'.
System.Collections.ArrayList list = new System.Collections.ArrayList();
list.Add(5);         // 5 (int, value type) is boxed to be stored as object.

// If we iterate through it:
int sum = 0;
foreach (object item in list)
{
    // The item is retrieved as 'object' (boxed) and then unboxed to perform the addition.
    sum += (int)item; // Unboxing
}

*/ 
public class PerformanceTest
{
    private const int Iterations = 10_000_000; // 10 million operations

    public static void Main(string[] args)
    {
        Console.WriteLine($"Starting test with {Iterations:N0} iterations...");

        // Measure ArrayList (Boxing)
        MeasureArrayList(Iterations);

        // Measure List<int> (No Boxing)
        MeasureGenericList(Iterations);
    }

    // SCENARIO 1: Uses non-generic ArrayList, causing boxing for every 'int' added.
    private static void MeasureArrayList(int iterations)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        ArrayList arrayList = new ArrayList(iterations);

        for (int i = 0; i < iterations; i++)
        {
            arrayList.Add(i); // !!! BOXING HAPPENS HERE !!!
        }

        stopwatch.Stop();
        Console.WriteLine($"\n--- ArrayList (Boxing) ---");
        Console.WriteLine($"Time taken: {stopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine($"GC Collections (Gen 0): {GC.CollectionCount(0)}");
    }

    // SCENARIO 2: Uses generic List<int>, avoiding boxing.
    private static void MeasureGenericList(int iterations)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();
        List<int> genericList = new List<int>(iterations);

        for (int i = 0; i < iterations; i++)
        {
            genericList.Add(i); // NO BOXING
        }

        stopwatch.Stop();
        Console.WriteLine($"\n--- List<int> (No Boxing) ---");
        Console.WriteLine($"Time taken: {stopwatch.ElapsedMilliseconds} ms");
        Console.WriteLine($"GC Collections (Gen 0): {GC.CollectionCount(0)}");
    }
}