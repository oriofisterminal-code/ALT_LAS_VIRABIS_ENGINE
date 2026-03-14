using System.Collections.Concurrent;

namespace Virabis.Core.DI;

/// <summary>
/// Simple service container implementation.
/// "Tembelim, uzatmayalım çok" - Mehmet Temel (İnşaat Mühendisi)
/// </summary>
public class ServiceContainer : IServiceContainer
{
    private readonly ConcurrentDictionary<Type, ServiceDescriptor> _services = new();
    private readonly ConcurrentDictionary<Type, object> _singletons = new();
    private readonly ServiceContainer? _parent;
    private bool _disposed;

    public ServiceContainer() { }

    private ServiceContainer(ServiceContainer parent)
    {
        _parent = parent;
    }

    /// <summary>
    /// Registers a service with implementation type.
    /// </summary>
    public IServiceContainer Register<TService, TImplementation>(
        ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TImplementation : TService
    {
        var descriptor = new ServiceDescriptor(
            typeof(TService),
            typeof(TImplementation),
            lifetime);

        _services[typeof(TService)] = descriptor;
        return this;
    }

    /// <summary>
    /// Registers a service with factory.
    /// </summary>
    public IServiceContainer Register<TService>(
        Func<IServiceContainer, TService> factory,
        ServiceLifetime lifetime = ServiceLifetime.Transient)
    {
        var descriptor = new ServiceDescriptor(
            typeof(TService),
            c => factory(c)!,
            lifetime);

        _services[typeof(TService)] = descriptor;
        return this;
    }

    /// <summary>
    /// Registers a singleton instance directly.
    /// </summary>
    public IServiceContainer RegisterInstance<TService>(TService instance)
    {
        _singletons[typeof(TService)] = instance ?? throw new ArgumentNullException(nameof(instance));
        return this;
    }

    /// <summary>
    /// Gets a service by type. Throws if not found.
    /// </summary>
    public TService Get<TService>()
    {
        var service = TryGet<TService>();
        if (service == null)
            throw new InvalidOperationException($"Service {typeof(TService).Name} is not registered");

        return service;
    }

    /// <summary>
    /// Tries to get a service. Returns default if not found.
    /// </summary>
    public TService? TryGet<TService>()
    {
        var type = typeof(TService);

        // Check local singletons first
        if (_singletons.TryGetValue(type, out var singleton))
            return (TService)singleton;

        // Check parent singletons
        if (_parent?._singletons.TryGetValue(type, out var parentSingleton) == true)
            return (TService)parentSingleton;

        // Get descriptor
        var descriptor = GetDescriptor(type);
        if (descriptor == null)
            return default;

        return (TService)CreateInstance(descriptor);
    }

    /// <summary>
    /// Checks if service is registered.
    /// </summary>
    public bool IsRegistered<TService>()
    {
        return _services.ContainsKey(typeof(TService)) ||
               _singletons.ContainsKey(typeof(TService)) ||
               (_parent?.IsRegistered<TService>() ?? false);
    }

    /// <summary>
    /// Creates a new scope for scoped services.
    /// </summary>
    public IServiceScope CreateScope()
    {
        return new ServiceScope(new ServiceContainer(this));
    }

    private ServiceDescriptor? GetDescriptor(Type type)
    {
        if (_services.TryGetValue(type, out var descriptor))
            return descriptor;

        return _parent?.GetDescriptor(type);
    }

    private object CreateInstance(ServiceDescriptor descriptor)
    {
        // Singleton - check cache
        if (descriptor.Lifetime == ServiceLifetime.Singleton)
        {
            if (_singletons.TryGetValue(descriptor.ServiceType, out var cached))
                return cached;

            if (_parent?._singletons.TryGetValue(descriptor.ServiceType, out var parentCached) == true)
                return parentCached;
        }

        // Create instance
        object instance;

        if (descriptor.Factory != null)
        {
            instance = descriptor.Factory(this);
        }
        else if (descriptor.ImplementationType != null)
        {
            instance = Activator.CreateInstance(descriptor.ImplementationType)!;
        }
        else
        {
            throw new InvalidOperationException($"Cannot create instance for {descriptor.ServiceType.Name}");
        }

        // Cache singleton
        if (descriptor.Lifetime == ServiceLifetime.Singleton)
        {
            _singletons[descriptor.ServiceType] = instance;
        }

        return instance;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // Dispose disposable singletons
        foreach (var singleton in _singletons.Values)
        {
            if (singleton is IDisposable disposable)
                disposable.Dispose();
        }

        _singletons.Clear();
        _services.Clear();
    }

    /// <summary>
    /// Clears all registrations (for testing).
    /// </summary>
    public void Clear()
    {
        _singletons.Clear();
        _services.Clear();
    }
}

/// <summary>
/// Service scope implementation.
/// </summary>
internal class ServiceScope : IServiceScope
{
    private readonly ServiceContainer _container;
    private bool _disposed;

    public IServiceContainer Container => _container;

    public ServiceScope(ServiceContainer container)
    {
        _container = container;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _container.Dispose();
    }
}

/// <summary>
/// Global service container for quick access.
/// </summary>
public static class Services
{
    private static ServiceContainer? _instance;

    /// <summary>
    /// Global container instance.
    /// </summary>
    public static ServiceContainer Container => _instance ??= new ServiceContainer();

    /// <summary>
    /// Quick access to Get service.
    /// </summary>
    public static T Get<T>() => Container.Get<T>();

    /// <summary>
    /// Quick access to TryGet service.
    /// </summary>
    public static T? TryGet<T>() => Container.TryGet<T>();

    /// <summary>
    /// Quick registration.
    /// </summary>
    public static ServiceContainer Register<TService, TImplementation>(
        ServiceLifetime lifetime = ServiceLifetime.Transient)
        where TImplementation : TService
        => (ServiceContainer)Container.Register<TService, TImplementation>(lifetime);

    /// <summary>
    /// Resets the container (for testing).
    /// </summary>
    public static void Reset()
    {
        _instance?.Dispose();
        _instance = null;
    }
}
