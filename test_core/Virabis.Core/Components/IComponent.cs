namespace Virabis.Core.Components;

/// <summary>
/// Marker interface for all components.
/// Components are data containers that can be attached to entities.
/// </summary>
public interface IComponent
{
    /// <summary>
    /// Entity this component is attached to.
    /// </summary>
    Entity? Entity { get; set; }

    /// <summary>
    /// Called when component is added to entity.
    /// </summary>
    void OnAdded() { }

    /// <summary>
    /// Called when component is removed from entity.
    /// </summary>
    void OnRemoved() { }

    /// <summary>
    /// Called every frame when enabled.
    /// </summary>
    void OnUpdate(float deltaTime) { }

    /// <summary>
    /// Whether this component should update.
    /// </summary>
    bool IsEnabled { get; set; }
}

/// <summary>
/// Base class for components with common functionality.
/// </summary>
public abstract class ComponentBase : IComponent
{
    public Entity? Entity { get; set; }
    public bool IsEnabled { get; set; } = true;

    public virtual void OnAdded() { }
    public virtual void OnRemoved() { }
    public virtual void OnUpdate(float deltaTime) { }

    /// <summary>
    /// Gets a sibling component from the same entity.
    /// </summary>
    protected T? GetSibling<T>() where T : class, IComponent
        => Entity?.GetComponent<T>();

    /// <summary>
    /// Requires a sibling component, throws if not found.
    /// </summary>
    protected T RequireSibling<T>() where T : class, IComponent
    {
        var component = GetSibling<T>();
        if (component == null)
            throw new InvalidOperationException($"{GetType().Name} requires {typeof(T).Name} component");

        return component;
    }
}

/// <summary>
/// Component that provides a specific capability.
/// </summary>
/// <typeparam name="TInterface">The interface this component provides</typeparam>
public interface ICapabilityProvider<TInterface> : IComponent where TInterface : class
{
    /// <summary>
    /// Gets the capability instance.
    /// </summary>
    TInterface Capability { get; }
}

/// <summary>
/// Attribute to mark component dependencies.
/// When a component with this attribute is added, required components are auto-added.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class RequireComponentAttribute : Attribute
{
    public Type ComponentType { get; }

    public RequireComponentAttribute(Type componentType)
    {
        if (!typeof(IComponent).IsAssignableFrom(componentType))
            throw new ArgumentException($"Type {componentType.Name} does not implement IComponent");

        ComponentType = componentType;
    }
}

/// <summary>
/// Attribute to mark components that should auto-add their dependencies.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class AutoAddDependenciesAttribute : Attribute
{
}
