using Virabis.Core.State;

namespace Virabis.Core.Templates;

/// <summary>
/// Central registry for entity templates.
/// "Bahçıvanlık hobim var, tohum eker gibi bir kere ek, sonra büyüsün" - Mehmet Temel (İnşaat Mühendisi)
///
/// v2.0: Standardized naming conventions (TryGet/Get pattern).
/// </summary>
public static class TemplateRegistry
{
    private static readonly Dictionary<string, EntityTemplate> _templates = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// All registered template names.
    /// </summary>
    public static IReadOnlyCollection<string> TemplateNames => _templates.Keys;

    /// <summary>
    /// Number of registered templates.
    /// </summary>
    public static int Count => _templates.Count;

    // ========================================
    // REGISTRATION
    // ========================================

    /// <summary>
    /// Registers a template with a name.
    /// </summary>
    /// <param name="name">Template name</param>
    /// <param name="template">Template to register</param>
    /// <exception cref="ArgumentNullException">When name or template is null</exception>
    public static void Register(string name, EntityTemplate template)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentNullException(nameof(name));

        if (template == null)
            throw new ArgumentNullException(nameof(template));

        _templates[name] = template;
    }

    /// <summary>
    /// Registers a template using its internal name.
    /// </summary>
    /// <param name="template">Template to register</param>
    /// <exception cref="ArgumentNullException">When template is null</exception>
    public static void Register(EntityTemplate template)
    {
        if (template == null)
            throw new ArgumentNullException(nameof(template));

        _templates[template.Name] = template;
    }

    /// <summary>
    /// Registers multiple templates.
    /// </summary>
    /// <param name="templates">Templates to register</param>
    public static void RegisterRange(params EntityTemplate[] templates)
    {
        if (templates == null) return;

        foreach (var template in templates)
        {
            if (template != null)
                _templates[template.Name] = template;
        }
    }

    // ========================================
    // RETRIEVAL (Consistent Naming)
    // ========================================

    /// <summary>
    /// Tries to get a template by name.
    /// </summary>
    /// <param name="name">Template name</param>
    /// <returns>Template if found, null otherwise</returns>
    public static EntityTemplate? TryGet(string name)
    {
        return _templates.TryGetValue(name, out var template) ? template : null;
    }

    /// <summary>
    /// Gets a template by name. Throws if not found.
    /// </summary>
    /// <param name="name">Template name</param>
    /// <returns>Template instance</returns>
    /// <exception cref="KeyNotFoundException">When template not found</exception>
    public static EntityTemplate Get(string name)
    {
        return _templates.TryGetValue(name, out var template)
            ? template
            : throw new KeyNotFoundException($"Template '{name}' not found. Available: {string.Join(", ", _templates.Keys)}");
    }

    /// <summary>
    /// Checks if template exists.
    /// </summary>
    public static bool Exists(string name) => !string.IsNullOrEmpty(name) && _templates.ContainsKey(name);

    /// <summary>
    /// Checks if template exists (alias for Exists).
    /// </summary>
    public static bool Contains(string name) => Exists(name);

    // ========================================
    // INSTANTIATION
    // ========================================

    /// <summary>
    /// Tries to create an entity from a template.
    /// </summary>
    /// <param name="templateName">Template name</param>
    /// <returns>Entity if template found, null otherwise</returns>
    public static Entity? TryInstantiate(string templateName)
    {
        var template = TryGet(templateName);
        return template?.Instantiate();
    }

    /// <summary>
    /// Creates an entity from a template. Throws if template not found.
    /// </summary>
    /// <param name="templateName">Template name</param>
    /// <returns>New entity instance</returns>
    /// <exception cref="KeyNotFoundException">When template not found</exception>
    public static Entity Instantiate(string templateName)
    {
        return Get(templateName).Instantiate();
    }

    /// <summary>
    /// Creates multiple entities from a template.
    /// </summary>
    /// <param name="templateName">Template name</param>
    /// <param name="count">Number of entities to create</param>
    /// <returns>Collection of entities (empty if template not found)</returns>
    public static IReadOnlyList<Entity> InstantiateMany(string templateName, int count)
    {
        var template = TryGet(templateName);
        return template?.Instantiate(count).ToList() ?? new List<Entity>();
    }

    // ========================================
    // REMOVAL
    // ========================================

    /// <summary>
    /// Removes a template.
    /// </summary>
    /// <returns>True if template was removed</returns>
    public static bool Remove(string name) => !string.IsNullOrEmpty(name) && _templates.Remove(name);

    /// <summary>
    /// Clears all templates (for testing).
    /// </summary>
    public static void Clear() => _templates.Clear();

    // ========================================
    // DEFAULTS
    // ========================================

    /// <summary>
    /// Registers default templates (Player, Enemy, Boss, Minion, Elite).
    /// </summary>
    public static void RegisterDefaults()
    {
        Register("Player", EntityTemplate.Player());
        Register("Enemy", EntityTemplate.Enemy());
        Register("Boss", EntityTemplate.Boss());
        Register("Minion", EntityTemplate.Minion());
        Register("Elite", EntityTemplate.Elite());
    }

    /// <summary>
    /// Resets to default templates only.
    /// </summary>
    public static void ResetToDefaults()
    {
        Clear();
        RegisterDefaults();
    }
}

/// <summary>
/// Extension methods for templates.
/// </summary>
public static class TemplateExtensions
{
    /// <summary>
    /// Registers this template to the global registry.
    /// </summary>
    public static EntityTemplate RegisterGlobal(this EntityTemplate template)
    {
        TemplateRegistry.Register(template);
        return template;
    }

    /// <summary>
    /// Creates and configures an entity, then returns it.
    /// </summary>
    public static Entity InstantiateAndConfigure(
        this EntityTemplate template,
        Action<Entity>? configure = null)
    {
        var entity = template.Instantiate();
        configure?.Invoke(entity);
        return entity;
    }
}
