namespace Virabis.Core.Plugins;

/// <summary>
/// Base class for plugins with common functionality.
/// </summary>
public abstract class PluginBase : IPlugin
{
    public Guid Id { get; }
    public string Name { get; }
    public Version Version { get; }
    public string Description { get; }
    public virtual IReadOnlyList<string> Dependencies => Array.Empty<string>();
    public PluginState State { get; protected set; } = PluginState.None;

    protected IPluginContext? Context { get; private set; }

    protected PluginBase(string name, string version, string description = "")
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Plugin name cannot be empty", nameof(name));

        Name = name;
        Version = Version.Parse(version);
        Description = description;
        Id = GenerateId(name);
    }

    private static Guid GenerateId(string name)
    {
        // Deterministic ID based on name
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(name));
        return new Guid(hash);
    }

    // ========================================
    // LIFECYCLE
    // ========================================

    public virtual void OnRegister(IPluginContext context)
    {
        Context = context;
        State = PluginState.Registered;
        Context.Logger.LogDebug($"Plugin '{Name}' registered");
    }

    public virtual void OnInitialize(IPluginContext context)
    {
        State = PluginState.Initialized;
        Context?.Logger.LogDebug($"Plugin '{Name}' initialized");
    }

    public virtual void OnEnable(IPluginContext context)
    {
        State = PluginState.Enabled;
        Context?.Logger.LogInformation($"Plugin '{Name}' enabled");
    }

    public virtual void OnDisable(IPluginContext context)
    {
        State = PluginState.Disabled;
        Context?.Logger.LogInformation($"Plugin '{Name}' disabled");
    }

    public virtual void OnUnload(IPluginContext context)
    {
        State = PluginState.Unloaded;
        Context?.Logger.LogInformation($"Plugin '{Name}' unloaded");
    }

    // ========================================
    // HELPERS
    // ========================================

    /// <summary>
    /// Gets another plugin (dependency).
    /// </summary>
    protected T? GetDependency<T>() where T : class, IPlugin
        => Context?.GetPlugin<T>();

    /// <summary>
    /// Checks if a dependency is enabled.
    /// </summary>
    protected bool IsDependencyEnabled(string pluginName)
        => Context?.IsPluginEnabled(pluginName) ?? false;

    /// <summary>
    /// Logs a debug message.
    /// </summary>
    protected void LogDebug(string message) => Context?.Logger.LogDebug($"[{Name}] {message}");

    /// <summary>
    /// Logs an info message.
    /// </summary>
    protected void LogInfo(string message) => Context?.Logger.LogInformation($"[{Name}] {message}");

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    protected void LogWarning(string message) => Context?.Logger.LogWarning($"[{Name}] {message}");

    /// <summary>
    /// Logs an error message.
    /// </summary>
    protected void LogError(string message, Exception? ex = null)
    {
        if (ex != null)
            Context?.Logger.LogError(ex, $"[{Name}] {message}");
        else
            Context?.Logger.LogError($"[{Name}] {message}");
    }
}

/// <summary>
/// Attribute for plugin metadata.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class PluginAttribute : Attribute
{
    public string Name { get; }
    public string Version { get; }
    public string Description { get; }

    public PluginAttribute(string name, string version, string description = "")
    {
        Name = name;
        Version = version;
        Description = description;
    }
}

/// <summary>
/// Attribute for plugin dependencies.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class PluginDependencyAttribute : Attribute
{
    public string PluginName { get; }

    public PluginDependencyAttribute(string pluginName)
    {
        PluginName = pluginName;
    }
}
