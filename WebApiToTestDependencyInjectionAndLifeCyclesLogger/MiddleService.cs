namespace WebApiToTestDependencyInjectionAndLifeCyclesLogger
{
    
    public class MiddleService
    {
        public ITransientService Transient { get; }
        public IScopedService Scoped { get; }
        public ISingletonService Singleton { get; }

        public MiddleService(
            ITransientService transient,
            IScopedService scoped,
            ISingletonService singleton)
        {
            Transient = transient; // Different (New instance every time)
            Scoped = scoped;       // Same(Shared within the Request)
            Singleton = singleton; // Same (Shared globally)
        }
    }
}
