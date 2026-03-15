namespace CovarianceAndContravariance.Lab;

// Contravariance Task
internal class PracticeTask3
{
    class Vehicle { public string Vin { get; set; } }
    class Car : Vehicle { public bool HasTurbo { get; set; } }

    // 'in' keyword makes this interface Contravarient
    // It can only consume T, never return it
    interface IValidator<in T>
    {
        void Validate(T item);
    }

    // A general validator that works for any vehicle
    class GeneralVehicleValidator : IValidator<Vehicle>
    {
        public void Validate(Vehicle item)
        {
            Console.WriteLine($"Validating vehicle VIN: {item.Vin}");
        }
    }

    public void Run()
    {
        // 1. Create the general validator
        IValidator<Vehicle> vehicleValidator = new GeneralVehicleValidator();

        // 2. Contravariance in action:
        // We assign a General validator to a Specific variable.
        // This works because IValidator is defined with 'in T'.
        IValidator<Car> carValidator = vehicleValidator;

        // 3. Usage
        carValidator.Validate(new Car { Vin = "123-ABC", HasTurbo = true });
    }
}
