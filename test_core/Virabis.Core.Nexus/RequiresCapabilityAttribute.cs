using System;

namespace Virabis.Core.Nexus;

/// <summary>
/// Marks a method as requiring specific capabilities to execute.
/// Multiple attributes can be applied to require multiple capabilities.
/// </summary>
/// <example>
/// <code>
/// [RequiresCapability("combat.damage")]
/// [RequiresCapability("combat.targeting")]
/// public void DealDamage(Entity target, float amount)
/// {
///     // Method implementation
/// }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class RequiresCapabilityAttribute : Attribute
{
    /// <summary>
    /// Gets the required capability identifier.
    /// </summary>
    /// <example>
    /// "combat.damage", "inventory.modify", "network.send"
    /// </example>
    public string Capability { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RequiresCapabilityAttribute"/> class.
    /// </summary>
    /// <param name="capability">The required capability identifier (e.g., "combat.damage").</param>
    /// <exception cref="ArgumentNullException">If capability is null.</exception>
    /// <exception cref="ArgumentException">If capability is empty or whitespace.</exception>
    public RequiresCapabilityAttribute(string capability)
    {
        if (capability == null)
            throw new ArgumentNullException(nameof(capability));
        
        if (string.IsNullOrWhiteSpace(capability))
            throw new ArgumentException("Capability cannot be empty or whitespace.", nameof(capability));

        Capability = capability;
    }
}
