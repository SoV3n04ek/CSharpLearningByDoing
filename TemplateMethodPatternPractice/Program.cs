/*
 * 🚀 Technical Task: The Resilient Pipeline Engine
📌 Project Overview
You are tasked with building a "Data Processing Pipeline". The engine should be able to take a raw data input (a string), pass it through a series of customizable transformation steps, validate the result, and finally execute an action (like logging or saving).

The core requirement is that the Engine must not know what the transformations are. It only defines the Workflow (The Template). The caller provides the logic using Delegates, Funcs, and Actions.

🎯 Learning Goals
Func<T, TResult>: Use this to define transformation "steps" that take an input and return a modified output.

Action<T>: Use this for "side effects" (logging, alerting) that don't return data.

Delegates: Define a custom multicast delegate to handle pipeline event notifications.

Anonymous Functions (Lambdas): Use them to implement logic on the fly without creating named methods.

Anonymous Classes: Use them to group temporary metadata results within the pipeline.

Functional Template Pattern: Replace traditional class inheritance with delegate injection to define algorithm steps.

🛠 Detailed Technical Task
1. The Core Engine (PipelineProcessor)
Create a class that manages the workflow. It should hold:

A Pre-processor step (Func).

A Validation step (Predicate<T> or Func<T, bool>).

A Success Callback (Action).

A Custom Delegate for OnStepCompleted.

2. The Logic: "The Template Method" (Functional Style)
Instead of a base class with abstract methods, your Process method should follow this fixed sequence:

Log that processing started (via Delegate).

Transform the data (via the injected Func).

Wrap the intermediate result in an Anonymous Class that contains the Value and a Timestamp.

Validate the anonymous class data.

If valid: Execute the success Action.

If invalid: Log an error (via Delegate).

3. Requirements for the Implementation (Console App)
In your Main method, you must:

Instantiate the PipelineProcessor.

Transformation: Pass a lambda that converts a string to "Upper Case" and trims whitespace.

Validation: Pass a lambda that checks if the string length is greater than 5.

Anonymous Class Usage: Within the processor, create a temporary object: new { Data = result, ProcessedAt = DateTime.Now }.

Multicast Delegate: Subscribe two different methods to the OnStepCompleted event (one for Console logging, one for "Database" simulation).

4. The Senior Challenge: "The Pipeline Extension"
Make the PipelineProcessor support a Collection of Func<string, string> instead of just one. The engine should chain them together (Composition).

Input: " hello "

Step 1 (Trim): "hello"

Step 2 (Upper): "HELLO"

Output: "HELLO"
*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace FunctionalPipeline.Lab
{
    // 1. CUSTOM DELEGATE
    // Ми визначаємо власний делегат для подій. 
    // Це "контракт", який каже: "Я приймаю рядок і нічого не повертаю".
    public delegate void PipelineEventHandler(string message);

    public class PipelineProcessor
    {
        // 2. PROPERTIES FOR LOGIC INJECTION
        // Ми використовуємо вбудовані типи Func та Action, щоб не плодити зайві делегати.

        // Список кроків трансформації
        public List<Func<string, string>> Transformations { get; set; } = new();

        // Предикат для валідації (повертає bool)
        public Func<string, bool> Validator { get; set; }

        // Дія, яка виконується при успіху (Side effect)
        public Action<string> OnSuccess { get; set; }

        // Подія на основі нашого кастомного делегату
        public PipelineEventHandler OnStepCompleted;

        // 3. THE FUNCTIONAL TEMPLATE METHOD
        public void Process(string input)
        {
            // Крок 1: Початок (Multicast Delegate)
            OnStepCompleted?.Invoke($"[START] Processing input: '{input}'");

            // Крок 2: Трансформація (Chaining/Composition)
            // Ми беремо вхідний рядок і "проганяємо" його через усі функції по черзі.
            string currentResult = input;
            foreach (var transform in Transformations)
            {
                currentResult = transform(currentResult);
            }

            // Крок 3: Anonymous Class
            // Створюємо тимчасовий об'єкт для групування даних. 
            // Це зручно, бо нам не треба створювати окремий клас-DTO для внутрішньої логіки.
            var context = new
            {
                Data = currentResult,
                ProcessedAt = DateTime.Now
            };

            // Крок 4: Валідація
            if (Validator != null && Validator(context.Data))
            {
                // Крок 5: Успіх (Action)
                OnStepCompleted?.Invoke($"[SUCCESS] Validation passed at {context.ProcessedAt}");
                OnSuccess?.Invoke(context.Data);
            }
            else
            {
                // Крок 6: Помилка
                OnStepCompleted?.Invoke($"[ERROR] Validation failed for: '{context.Data}'");
            }
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("=== Resilient Pipeline Engine Initializing ===\n");

            // Ініціалізація процесора
            var processor = new PipelineProcessor();

            // ПІДПИСКА НА ДЕЛЕГАТИ (Multicast)
            // Додаємо два обробники подій
            processor.OnStepCompleted += msg => Console.WriteLine($"LOG: {msg}");
            processor.OnStepCompleted += msg => FakeDatabaseLog(msg);

            // НАЛАШТУВАННЯ ТРАНСФОРМАЦІЙ (Lambdas)
            // Senior Challenge: додаємо кілька кроків
            processor.Transformations.Add(s => s.Trim());
            processor.Transformations.Add(s => s.ToUpper());

            // НАЛАШТУВАННЯ ВАЛІДАЦІЇ
            // Перевіряємо, чи довжина більше 5
            processor.Validator = s => s.Length > 5;

            // НАЛАШТУВАННЯ ФІНАЛЬНОЇ ДІЇ (Action)
            processor.OnSuccess = finalResult =>
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[FINAL SAVING] Writing '{finalResult}' to persistent storage...");
                Console.ResetColor();
            };

            // ЗАПУСК 1: Валідні дані
            Console.WriteLine("--- Running Test 1 (Valid) ---");
            processor.Process("   hello world   ");

            // ЗАПУСК 2: Невалідні дані
            Console.WriteLine("\n--- Running Test 2 (Invalid) ---");
            processor.Process("  hey  ");
        }

        static void FakeDatabaseLog(string message)
        {
            // Імітація запису в БД
            // В реальному проекті тут міг би бути SQL insert
        }
    }
}


/*
❓  Interview "Deep Dive" Questions
Prepare to answer these after your implementation:

Variance: Why can you pass a Func<string, object> to a variable expected to be Func<string, string> (or vice versa)? Talk about Covariance and Contravariance in delegates.

Closures: If your lambda function captures a local variable from the Main method, how does the CLR handle that memory under the hood?

Closure Pitfall: What happens if you capture a loop variable (e.g., for (int i...)) in a delegate and execute it later?

Anonymous Types: Can you return an Anonymous Type from a method? Why or why not? (Hint: object vs dynamic).
*/