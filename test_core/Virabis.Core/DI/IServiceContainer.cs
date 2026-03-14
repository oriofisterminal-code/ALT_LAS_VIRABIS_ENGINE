namespace Virabis.Core.DI;

/// <summary>
/// Service lifetime options.
/// </summary>
public enum ServiceLifetime
{
    /// <summary>
    /// New instance every time.
    /// </summary>
    Transient,

    /// <summary>
    /// Single instance for all requests.
    /// </summary>
    Singleton,

    /// <summary>
    /// Instance per scope (e.g., per game session).
    /// </summary>
    Scoped
}

/// <summary>
/// Service descriptor for registration.
/// </summary>
public readonly struct ServiceDescriptor
{
    public Type ServiceType { get; }
    public Type? ImplementationType { get; }
    public Func<IServiceContainer, object>? Factory { get; }
    public ServiceLifetime Lifetime { get; }
    public object? SingletonInstance { get; internal set; }

    public ServiceDescriptor(
        Type serviceType,
        Type implementationType,
        ServiceLifetime lifetime)
    {
        ServiceType = serviceType;
        ImplementationType = implementationType;
        Factory = null;
        Lifetime = lifetime;
        SingletonInstance = null;
    }

    public ServiceDescriptor(
        Type serviceType,
        Func<IServiceContainer, object> factory,
        ServiceLifetime lifetime)
    {
        ServiceType = serviceType;
        ImplementationType = null;
        Factory = factory;
        Lifetime = lifetime;
        SingletonInstance = null;
    }
}

/// <summary>
/// Service container interface for dependency injection.
/// "Tembelim, her seferinde yazmak istemiyorum" - Mehmet Temel (İnşaat Mühendisi)
/// </summary>
public interface IServiceContainer : IDisposable
{
    /// <summary>
    /// Registers a service with implementation type.
    /// </summary>
    IServiceContainer Register<TService, TImplementation>(
        ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TImplementation : TService;

    /// <summary>
    /// Registers a service with factory.
    /// </summary>
    IServiceContainer Register<TService>(
        Func<IServiceContainer, TService> factory,
        ServiceLifetime lifetime = ServiceLifetime.Transient);

    /// <summary>
    /// Registers a singleton instance.
    /// </summary>
    IServiceContainer RegisterInstance<TService>(TService instance);

    /// <summary>
    /// Gets a service by type.
    /// </summary>
    TService Get<TService>();

    /// <summary>
    /// Tries to get a service, returns null if not found.
    /// </summary>
    TService? TryGet<TService>();

    /// <summary>
    /// Checks if service is registered.
    /// </summary>
    bool IsRegistered<TService>();

    /// <summary>
    /// Creates a new scope for scoped services.
    /// </summary>
    IServiceScope CreateScope();
}

/// <summary>
/// Service scope for scoped lifetime.
/// </summary>
public interface IServiceScope : IDisposable
{
    /// <summary>
    /// Service container for this scope.
    /// </summary>
    IServiceContainer Container { get; }
}
