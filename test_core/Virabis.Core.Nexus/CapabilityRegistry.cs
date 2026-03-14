using System;
using System.Collections.Generic;
using System.Linq;

namespace Virabis.Core.Nexus;

/// <summary>
/// Registry for tracking feature capabilities at runtime.
/// Thread-safe singleton implementation.
/// </summary>
public class CapabilityRegistry
{
    private static readonly Lazy<CapabilityRegistry> _instance = new(() => new CapabilityRegistry());
    private readonly Dictionary<string, HashSet<string>> _featureCapabilities = new();
    private readonly object _lock = new();

    /// <summary>
    /// Gets the singleton instance of the CapabilityRegistry.
    /// </summary>
    public static CapabilityRegistry Instance => _instance.Value;

    private CapabilityRegistry() { }

    /// <summary>
    /// Registers capabilities for a feature.
    /// </summary>
    /// <param name="featureName">The name of the feature.</param>
    /// <param name="capabilities">The capabilities to register.</param>
    /// <exception cref="ArgumentNullException">If featureName or capabilities is null.</exception>
    /// <exception cref="ArgumentException">If featureName is empty or whitespace.</exception>
    public void Register(string featureName, IEnumerable<string> capabilities)
    {
        if (featureName == null)
            throw new ArgumentNullException(nameof(featureName));
        
        if (string.IsNullOrWhiteSpace(featureName))
            throw new ArgumentException("Feature name cannot be empty or whitespace.", nameof(featureName));
        
        if (capabilities == null)
            throw new ArgumentNullException(nameof(capabilities));

        lock (_lock)
        {
            if (!_featureCapabilities.ContainsKey(featureName))
            {
                _featureCapabilities[featureName] = new HashSet<string>();
            }

            foreach (var capability in capabilities)
            {
                if (!string.IsNullOrWhiteSpace(capability))
                {
                    _featureCapabilities[featureName].Add(capability);
                }
            }
        }
    }

    /// <summary>
    /// Checks if a feature has a specific capability.
    /// </summary>
    /// <param name="featureName">The name of the feature.</param>
    /// <param name="capability">The capability to check.</param>
    /// <returns>True if the feature has the capability, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">If featureName or capability is null.</exception>
    public bool HasCapability(string featureName, string capability)
    {
        if (featureName == null)
            throw new ArgumentNullException(nameof(featureName));
        
        if (capability == null)
            throw new ArgumentNullException(nameof(capability));

        lock (_lock)
        {
            return _featureCapabilities.TryGetValue(featureName, out var capabilities) &&
                   capabilities.Contains(capability);
        }
    }

    /// <summary>
    /// Enforces that a feature has a specific capability.
    /// </summary>
    /// <param name="featureName">The name of the feature.</param>
    /// <param name="capability">The required capability.</param>
    /// <exception cref="ArgumentNullException">If featureName or capability is null.</exception>
    /// <exception cref="UnauthorizedAccessException">If the feature does not have the required capability.</exception>
    public void EnforceCapability(string featureName, string capability)
    {
        if (featureName == null)
            throw new ArgumentNullException(nameof(featureName));
        
        if (capability == null)
            throw new ArgumentNullException(nameof(capability));

        if (!HasCapability(featureName, capability))
        {
            throw new UnauthorizedAccessException(
                $"Feature '{featureName}' does not have required capability '{capability}'.");
        }
    }

    /// <summary>
    /// Gets all capabilities for a feature.
    /// </summary>
    /// <param name="featureName">The name of the feature.</param>
    /// <returns>A read-only list of capabilities, or an empty list if the feature is not registered.</returns>
    /// <exception cref="ArgumentNullException">If featureName is null.</exception>
    public IReadOnlyList<string> GetCapabilities(string featureName)
    {
        if (featureName == null)
            throw new ArgumentNullException(nameof(featureName));

        lock (_lock)
        {
            if (_featureCapabilities.TryGetValue(featureName, out var capabilities))
            {
                return capabilities.ToList();
            }

            return new List<string>();
        }
    }

    /// <summary>
    /// Unregisters all capabilities for a feature.
    /// </summary>
    /// <param name="featureName">The name of the feature.</param>
    /// <exception cref="ArgumentNullException">If featureName is null.</exception>
    public void Unregister(string featureName)
    {
        if (featureName == null)
            throw new ArgumentNullException(nameof(featureName));

        lock (_lock)
        {
            _featureCapabilities.Remove(featureName);
        }
    }

    /// <summary>
    /// Clears all registered capabilities (for testing purposes).
    /// </summary>
    public void ClearAll()
    {
        lock (_lock)
        {
            _featureCapabilities.Clear();
        }
    }
}
