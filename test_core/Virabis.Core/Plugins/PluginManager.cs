using System.Collections.Concurrent;
using Virabis.Core.Components;
using Virabis.Core.Events;
using Virabis.Core.Pooling;
using Virabis.Core.Templates;

namespace Virabis.Core.Plugins;

/// <summary>
/// Manages plugin lifecycle and dependencies.
/// "Eklentiler gelsin, kod değişmesin" - Selim Entegrasyon (Lego Ustası)
/// </summary>
public class PluginManager : IPluginContext, IDisposable
{
    private readonly ConcurrentDictionary<string, IPlugin> _plugins = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<Type, IPlugin> _pluginsByType = new();
    private readonly ConcurrentDictionary<Guid, IPlugin> _pluginsById = new();
    private readonly List<IPlugin> _loadOrder = new();
    private bool _disposed;

    // Context services
    private readonly DI.IServiceContainer _services;
    private readonly Events.IEventBus _events;
    private readonly Components.ComponentRegistry _components;
    private readonly Templates.TemplateRegistry _templates;
    private readonly Pooling.IPoolManager _pools;
    private readonly Abstractions.ILoggingService _logger;

    // ========================================
    // PROPERTIES
    // ========================================

    public DI.IServiceContainer Services => _services;
    public Events.IEventBus Events => _events;
    public Components.ComponentRegistry Components => _components;
    public Templates.TemplateRegistry Templates => _templates;
    public Pooling.IPoolManager Pools => _pools;
    public Abstractions.ILoggingService Logger => _logger;

    /// <summary>
    /// All loaded plugins.
    /// </summary>
    public IReadOnlyCollection<IPlugin> Plugins => _plugins.Values;

    /// <summary>
    /// Plugin count.
    /// </summary>
    public int Count => _plugins.Count;

    /// <summary>
    /// Number of enabled plugins.
    /// </summary>
    public int EnabledCount => _plugins.Values.Count(p => p.State == PluginState.Enabled);

    // ========================================
    // CONSTRUCTOR
    // ========================================

    public PluginManager(
        DI.IServiceContainer? services = null,
        Events.IEventBus? events = null,
        Abstractions.ILoggingService? logger = null)
    {
        _services = services ?? new DI.ServiceContainer();
        _events = events ?? new Events.EventBus();
        _components = new Components.ComponentRegistry();
        _templates = new Templates.TemplateRegistry();
        _pools = new Pooling.PoolManager();
        _logger = logger ?? new Logging.ConsoleLogger("PluginManager");
    }

    // ========================================
    // REGISTRATION
    // ========================================

    /// <summary>
    /// Registers a plugin.
    /// </summary>
    public void Register(IPlugin plugin)
    {
        if (plugin == null)
            throw new ArgumentNullException(nameof(plugin));

        if (_plugins.ContainsKey(plugin.Name))
            throw new InvalidOperationException($"Plugin '{plugin.Name}' is already registered");

        // Check dependencies
        foreach (var dep in plugin.Dependencies)
        {
            if (!_plugins.ContainsKey(dep))
            {
                _logger.LogWarning($"Plugin '{plugin.Name}' depends on '{dep}' which is not yet registered");
            }
        }

        _plugins[plugin.Name] = plugin;
        _pluginsByType[plugin.GetType()] = plugin;
        _pluginsById[plugin.Id] = plugin;

        _logger.LogInformation($"Registered plugin: {plugin.Name} v{plugin.Version}");

        // Call OnRegister
        try
        {
            plugin.OnRegister(this);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error registering plugin '{plugin.Name}'");
            throw;
        }
    }

    /// <summary>
    /// Registers multiple plugins.
    /// </summary>
    public void Register(params IPlugin[] plugins)
    {
        foreach (var plugin in plugins)
        {
            Register(plugin);
        }
    }

    // ========================================
    // INITIALIZATION
    // ========================================

    /// <summary>
    /// Initializes all registered plugins in dependency order.
    /// </summary>
    public void InitializeAll()
    {
        var sorted = SortByDependencies();

        foreach (var plugin in sorted)
        {
            if (plugin.State != PluginState.Registered)
                continue;

            try
            {
                plugin.OnInitialize(this);
                _logger.LogInformation($"Initialized plugin: {plugin.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error initializing plugin '{plugin.Name}'");
                throw;
            }
        }
    }

    /// <summary>
    /// Enables all initialized plugins.
    /// </summary>
    public void EnableAll()
    {
        var sorted = SortByDependencies();

        foreach (var plugin in sorted)
        {
            if (plugin.State != PluginState.Initialized && plugin.State != PluginState.Disabled)
                continue;

            try
            {
                plugin.OnEnable(this);
                _logger.LogInformation($"Enabled plugin: {plugin.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error enabling plugin '{plugin.Name}'");
                throw;
            }
        }
    }

    /// <summary>
    /// Disables all enabled plugins in reverse order.
    /// </summary>
    public void DisableAll()
    {
        var sorted = SortByDependencies();
        sorted.Reverse();

        foreach (var plugin in sorted)
        {
            if (plugin.State != PluginState.Enabled)
                continue;

            try
            {
                plugin.OnDisable(this);
                _logger.LogInformation($"Disabled plugin: {plugin.Name}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error disabling plugin '{plugin.Name}'");
            }
        }
    }

    // ========================================
    // SINGLE PLUGIN OPERATIONS
    // ========================================

    /// <summary>
    /// Enables a specific plugin.
    /// </summary>
    public bool Enable(string name)
    {
        if (!_plugins.TryGetValue(name, out var plugin))
            return false;

        if (plugin.State == PluginState.Enabled)
            return true;

        // Check dependencies
        foreach (var dep in plugin.Dependencies)
        {
            var depPlugin = GetPlugin(dep);
            if (depPlugin?.State != PluginState.Enabled)
            {
                _logger.LogWarning($"Cannot enable '{name}': dependency '{dep}' is not enabled");
                return false;
            }
        }

        plugin.OnEnable(this);
        _logger.LogInformation($"Enabled plugin: {name}");
        return true;
    }

    /// <summary>
    /// Disables a specific plugin.
    /// </summary>
    public bool Disable(string name)
    {
        if (!_plugins.TryGetValue(name, out var plugin))
            return false;

        if (plugin.State != PluginState.Enabled)
            return true;

        // Check if other plugins depend on this
        var dependents = _plugins.Values
            .Where(p => p.Dependencies.Contains(name, StringComparer.OrdinalIgnoreCase) && p.State == PluginState.Enabled)
            .ToList();

        if (dependents.Count > 0)
        {
            _logger.LogWarning($"Cannot disable '{name}': plugins [{string.Join(", ", dependents.Select(p => p.Name))}] depend on it");
            return false;
        }

        plugin.OnDisable(this);
        _logger.LogInformation($"Disabled plugin: {name}");
        return true;
    }

    /// <summary>
    /// Unloads a plugin completely.
    /// </summary>
    public bool Unload(string name)
    {
        if (!Disable(name))
            return false;

        if (!_plugins.TryRemove(name, out var plugin))
            return false;

        _pluginsByType.TryRemove(plugin.GetType(), out _);
        _pluginsById.TryRemove(plugin.Id, out _);

        plugin.OnUnload(this);
        _logger.LogInformation($"Unloaded plugin: {name}");
        return true;
    }

    // ========================================
    // QUERY
    // ========================================

    public IPlugin? GetPlugin(string name)
        => _plugins.TryGetValue(name, out var plugin) ? plugin : null;

    public T? GetPlugin<T>() where T : class, IPlugin
        => _pluginsByType.TryGetValue(typeof(T), out var plugin) ? plugin as T : null;

    public bool IsPluginEnabled(string name)
        => _plugins.TryGetValue(name, out var plugin) && plugin.State == PluginState.Enabled;

    public bool IsPluginLoaded(string name)
        => _plugins.ContainsKey(name);

    // ========================================
    // DEPENDENCY SORTING
    // ========================================

    private List<IPlugin> SortByDependencies()
    {
        var sorted = new List<IPlugin>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var plugin in _plugins.Values)
        {
            Visit(plugin, sorted, visited, visiting);
        }

        return sorted;
    }

    private void Visit(IPlugin plugin, List<IPlugin> sorted, HashSet<string> visited, HashSet<string> visiting)
    {
        if (visited.Contains(plugin.Name))
            return;

        if (visiting.Contains(plugin.Name))
            throw new InvalidOperationException($"Circular dependency detected: {plugin.Name}");

        visiting.Add(plugin.Name);

        foreach (var dep in plugin.Dependencies)
        {
            if (_plugins.TryGetValue(dep, out var depPlugin))
            {
                Visit(depPlugin, sorted, visited, visiting);
            }
        }

        visiting.Remove(plugin.Name);
        visited.Add(plugin.Name);
        sorted.Add(plugin);
    }

    // ========================================
    // DISPOSE
    // ========================================

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        DisableAll();

        foreach (var plugin in _plugins.Values.ToList())
        {
            try
            {
                plugin.OnUnload(this);
            }
            catch { }
        }

        _plugins.Clear();
        _pluginsByType.Clear();
        _pluginsById.Clear();
    }
}
