namespace CovarianceAndContravariance.Lab;

public class PracticalTask2
{
    class Vehicle { }

    class Car : Vehicle { }

    interface IRepository<out T>
    {
        T GetById(int id);
    }

    class CarRepository : IRepository<Car>
    {
        public Car GetById(int id)
        {
            return new Car();
        }
    }

    public void Main()
    {
        CarRepository carRepository = new CarRepository();
        // It works thanks to covariance
        IRepository<Vehicle> vehicleRepository = carRepository;
    }
}
