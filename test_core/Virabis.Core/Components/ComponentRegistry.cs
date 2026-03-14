namespace Virabis.Core.Components;

/// <summary>
/// Registry for component types and their metadata.
/// "Her parçanın yeri belli olsun" - Zeynep Modüler (Origami Sanatçısı)
/// </summary>
public static class ComponentRegistry
{
    private static readonly Dictionary<Type, ComponentMetadata> _metadata = new();
    private static readonly Dictionary<string, Type> _nameToType = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<Type, string> _typeToName = new();

    /// <summary>
    /// Gets all registered component types.
    /// </summary>
    public static IReadOnlyCollection<Type> RegisteredTypes => _metadata.Keys;

    /// <summary>
    /// Gets the number of registered components.
    /// </summary>
    public static int Count => _metadata.Count;

    // ========================================
    // REGISTRATION
    // ========================================

    /// <summary>
    /// Registers a component type.
    /// </summary>
    public static void Register<TComponent>() where TComponent : IComponent, new()
    {
        Register(typeof(TComponent));
    }

    /// <summary>
    /// Registers a component type.
    /// </summary>
    public static void Register(Type componentType)
    {
        if (!typeof(IComponent).IsAssignableFrom(componentType))
            throw new ArgumentException($"{componentType.Name} does not implement IComponent");

        var name = componentType.Name;
        if (name.EndsWith("Component"))
            name = name[..^"Component".Length];

        var metadata = new ComponentMetadata(componentType)
        {
            Name = name,
            FullName = componentType.FullName ?? componentType.Name,
            Requires = GetRequiredComponents(componentType),
            AutoAddDependencies = componentType.GetCustomAttributes(typeof(AutoAddDependenciesAttribute), false).Any()
        };

        _metadata[componentType] = metadata;
        _nameToType[name] = componentType;
        _typeToName[componentType] = name;
    }

    /// <summary>
    /// Registers all components from an assembly.
    /// </summary>
    public static void RegisterFromAssembly(System.Reflection.Assembly assembly)
    {
        var componentTypes = assembly.GetTypes()
            .Where(t => typeof(IComponent).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

        foreach (var type in componentTypes)
        {
            Register(type);
        }
    }

    /// <summary>
    /// Registers all common components.
    /// </summary>
    public static void RegisterDefaults()
    {
        Register<HealthComponent>();
        Register<StatComponent>();
        Register<TagComponent>();
        Register<StateComponent>();
        Register<VelocityComponent>();
        Register<InventoryComponent>();
    }

    // ========================================
    // QUERY
    // ========================================

    /// <summary>
    /// Gets metadata for a component type.
    /// </summary>
    public static ComponentMetadata? GetMetadata<TComponent>() where TComponent : IComponent
        => GetMetadata(typeof(TComponent));

    /// <summary>
    /// Gets metadata for a component type.
    /// </summary>
    public static ComponentMetadata? GetMetadata(Type componentType)
        => _metadata.TryGetValue(componentType, out var meta) ? meta : null;

    /// <summary>
    /// Gets component type by name.
    /// </summary>
    public static Type? GetTypeByName(string name)
        => _nameToType.TryGetValue(name, out var type) ? type : null;

    /// <summary>
    /// Gets component name by type.
    /// </summary>
    public static string GetNameByType(Type type)
        => _typeToName.TryGetValue(type, out var name) ? name : type.Name;

    /// <summary>
    /// Gets required components for a type.
    /// </summary>
    public static IReadOnlyList<Type> GetRequiredComponents(Type componentType)
    {
        var attrs = componentType.GetCustomAttributes(typeof(RequireComponentAttribute), false);
        return attrs.Cast<RequireComponentAttribute>().Select(a => a.ComponentType).ToList();
    }

    /// <summary>
    /// Checks if a type is registered.
    /// </summary>
    public static bool IsRegistered<TComponent>() where TComponent : IComponent
        => _metadata.ContainsKey(typeof(TComponent));

    /// <summary>
    /// Creates a new instance of a component.
    /// </summary>
    public static IComponent? CreateInstance(Type componentType)
    {
        if (!_metadata.ContainsKey(componentType))
            Register(componentType);

        return Activator.CreateInstance(componentType) as IComponent;
    }

    // ========================================
    // CLEAR
    // ========================================

    /// <summary>
    /// Clears all registrations.
    /// </summary>
    public static void Clear()
    {
        _metadata.Clear();
        _nameToType.Clear();
        _typeToName.Clear();
    }
}

/// <summary>
/// Metadata for a component type.
/// </summary>
public class ComponentMetadata
{
    public Type Type { get; }
    public string Name { get; set; } = "";
    public string FullName { get; set; } = "";
    public IReadOnlyList<Type> Requires { get; set; } = Array.Empty<Type>();
    public bool AutoAddDependencies { get; set; }

    public ComponentMetadata(Type type)
    {
        Type = type;
    }

    public override string ToString() => $"Component: {Name}";
}
