namespace Virabis.Core.Plugins;

/// <summary>
/// Interface for all plugins.
/// Plugins can extend the core functionality without modifying core code.
/// </summary>
public interface IPlugin
{
    /// <summary>
    /// Unique identifier for this plugin.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Human-readable plugin name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Plugin version.
    /// </summary>
    Version Version { get; }

    /// <summary>
    /// Plugin description.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Dependencies (plugin names that must be loaded first).
    /// </summary>
    IReadOnlyList<string> Dependencies { get; }

    /// <summary>
    /// Called when plugin is registered (before other plugins initialize).
    /// Register services, events, and types here.
    /// </summary>
    void OnRegister(IPluginContext context);

    /// <summary>
    /// Called when all plugins are registered.
    /// Initialize plugin state, resolve services here.
    /// </summary>
    void OnInitialize(IPluginContext context);

    /// <summary>
    /// Called when the plugin should be enabled.
    /// Start plugin functionality here.
    /// </summary>
    void OnEnable(IPluginContext context);

    /// <summary>
    /// Called when the plugin should be disabled.
    /// Stop plugin functionality here.
    /// </summary>
    void OnDisable(IPluginContext context);

    /// <summary>
    /// Called when plugin is being unloaded.
    /// Cleanup all resources here.
    /// </summary>
    void OnUnload(IPluginContext context);

    /// <summary>
    /// Current state of the plugin.
    /// </summary>
    PluginState State { get; }
}

/// <summary>
/// Plugin lifecycle state.
/// </summary>
public enum PluginState
{
    None,           // Not registered
    Registered,     // Registered but not initialized
    Initialized,    // Initialized but not enabled
    Enabled,        // Running
    Disabled,       // Stopped
    Error,          // In error state
    Unloaded        // Unloaded
}

/// <summary>
/// Context provided to plugins during lifecycle events.
/// </summary>
public interface IPluginContext
{
    /// <summary>
    /// Service container for dependency injection.
    /// </summary>
    DI.IServiceContainer Services { get; }

    /// <summary>
    /// Event bus for subscribing to events.
    /// </summary>
    Events.IEventBus Events { get; }

    /// <summary>
    /// Component registry for registering new component types.
    /// </summary>
    Components.ComponentRegistry Components { get; }

    /// <summary>
    /// Template registry for registering entity templates.
    /// </summary>
    Templates.TemplateRegistry Templates { get; }

    /// <summary>
    /// Pool manager for creating object pools.
    /// </summary>
    Pooling.IPoolManager Pools { get; }

    /// <summary>
    /// Logger for plugin logging.
    /// </summary>
    Abstractions.ILoggingService Logger { get; }

    /// <summary>
    /// Gets another plugin by name.
    /// </summary>
    IPlugin? GetPlugin(string name);

    /// <summary>
    /// Gets another plugin by type.
    /// </summary>
    T? GetPlugin<T>() where T : class, IPlugin;

    /// <summary>
    /// Checks if a plugin is loaded and enabled.
    /// </summary>
    bool IsPluginEnabled(string name);
}
