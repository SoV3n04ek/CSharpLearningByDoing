namespace WebApiToTestDependencyInjectionAndLifeCyclesLogger
{
    public interface ITransientService { string GetId(); }
    public interface IScopedService { string GetId(); }
    public interface ISingletonService { string GetId(); }

    public class LifeTimeService : ITransientService, IScopedService, ISingletonService
    {
        private readonly string _id;

        public LifeTimeService()
        {
            // Generating a GUID in the constructor is the key. 
            // It tells us exactly WHEN a new instance was created.
            _id = Guid.NewGuid().ToString().Substring(0, 8);
        }

        public string GetId() => _id;
    }
}
