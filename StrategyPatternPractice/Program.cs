/*
 * 🏢 Real-World Problem
You are building an e-commerce platform. 
The business wants to support multiple payment methods: 
    Stripe (Credit Card), PayPal, and Bitcoin.

Each method has totally different logic:

Stripe needs a card number and CVV.

PayPal needs an email and a redirect URL.

Bitcoin needs a wallet address and a transaction hash.

If you put all this logic into one PaymentService class, it will become a "God Object" 
that breaks every time you add a new payment provider (like Apple Pay).

🛠 Technical Task
Define the Strategy: Create an interface IPaymentStrategy with a method ProcessPayment(decimal amount).

Implement Concrete Strategies: * StripePaymentStrategy

PayPalPaymentStrategy

CryptoPaymentStrategy

The Context: Create a PaymentProcessor class that accepts an IPaymentStrategy (via constructor or method injection).

The Senior Twist (Dependency Injection): Don't just instantiate the strategies. Use the Factory Pattern combined with DI to resolve the correct strategy based on a string or enum passed from the UI.
*/

using Microsoft.Extensions.DependencyInjection;
using System;
using System.Reflection.Emit;

/*
*TOPIC: Strategy Pattern with Dependency Injection.
 * * LEARNING GOAL: 
 *1.Decouple high - level business logic from low-level implementation.
 * 2. Use the Open/Closed Principle (add new providers without changing existing code).
 * 3. Use a Dictionary-based Factory for clean strategy resolution.
 */

// StrategyInterface
public interface IPaymentStrategy
{
    // A unique identifier to help the factory find the right strategy
    PaymentMethod Method { get; }
    void Pay(decimal amount);
}

public enum PaymentMethod { Stripe, PayPal, Crypto }

// Concrete Strategies
public class StripePayment : IPaymentStrategy
{
    public PaymentMethod Method => PaymentMethod.Stripe;
    public void Pay(decimal amount) => Console.WriteLine($"[Stripe] Charging {amount:C} via credit card.");
}

public class PayPalPayment : IPaymentStrategy
{
    public PaymentMethod Method => PaymentMethod.PayPal;
    public void Pay(decimal amount) => Console.WriteLine($"[PayPal] Charging {amount:C} via User Redirect.");
}

public class CryptoPayment : IPaymentStrategy
{
    public PaymentMethod Method => PaymentMethod.Crypto;
    public void Pay(decimal amount) => Console.WriteLine($"[Crypto] Transferring {amount:C} to Blockchain Wallet.");
}

// The Context / Processor
public class CheckoutService
{
    private readonly IEnumerable<IPaymentStrategy> _strategies;

    public CheckoutService(IEnumerable<IPaymentStrategy> strategies)
    {
        _strategies = strategies;
    }

    public void Checkout(PaymentMethod method, decimal amount)
    {
        // Use LINQ to find the strategy at runtime. 
        // This avoids large 'switch' statements.
        var strategy = _strategies.FirstOrDefault(s => s.Method == method)
                       ?? throw new NotSupportedException("Payment method not supported.");

        strategy.Pay(amount);
    }
}

class Program
{
    static void Main()
    {
        // Setup DI
        var services = new ServiceCollection();

        // Register all strategies
        services.AddTransient<IPaymentStrategy, StripePayment>();
        services.AddTransient<IPaymentStrategy, PayPalPayment>();
        services.AddTransient<IPaymentStrategy, CryptoPayment>();

        // Register the main service
        services.AddTransient<CheckoutService>();

        var provider = services.BuildServiceProvider();

        // Execution 
        var checkout = provider.GetRequiredService<CheckoutService>();

        Console.WriteLine("--- Strategy Pattern Demo ---");

        // Simulating user choosing PayPal at runtime
        checkout.Checkout(PaymentMethod.PayPal, 99.50m);

        // Simulating user choosing Crypto
        checkout.Checkout(PaymentMethod.Crypto, 1500.00m);
    }
}