using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Virabis.Core.Nexus;

/// <summary>
/// Validates that features have the required capabilities before method execution.
/// </summary>
public static class CapabilityValidator
{
    /// <summary>
    /// Validates that a feature type has all required capabilities declared in its manifest.
    /// </summary>
    /// <param name="featureType">The feature type to validate.</param>
    /// <param name="declaredCapabilities">The capabilities declared in the feature's manifest.</param>
    /// <returns>A validation result containing any errors found.</returns>
    /// <exception cref="ArgumentNullException">If featureType or declaredCapabilities is null.</exception>
    public static ValidationResult ValidateFeature(Type featureType, IEnumerable<string> declaredCapabilities)
    {
        if (featureType == null)
            throw new ArgumentNullException(nameof(featureType));
        
        if (declaredCapabilities == null)
            throw new ArgumentNullException(nameof(declaredCapabilities));

        var errors = new List<string>();
        var capabilitySet = new HashSet<string>(declaredCapabilities);

        // Get all methods with RequiresCapability attributes
        var methods = featureType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
        
        foreach (var method in methods)
        {
            var requiredCapabilities = method.GetCustomAttributes<RequiresCapabilityAttribute>()
                .Select(attr => attr.Capability)
                .ToList();

            foreach (var required in requiredCapabilities)
            {
                if (!capabilitySet.Contains(required))
                {
                    errors.Add($"Method '{method.Name}' requires capability '{required}' " +
                              $"but feature '{featureType.Name}' does not declare it in manifest.");
                }
            }
        }

        return new ValidationResult(errors.Count == 0, errors);
    }

    /// <summary>
    /// Gets all required capabilities for a specific method.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <returns>A list of required capability identifiers.</returns>
    /// <exception cref="ArgumentNullException">If method is null.</exception>
    public static IReadOnlyList<string> GetRequiredCapabilities(MethodInfo method)
    {
        if (method == null)
            throw new ArgumentNullException(nameof(method));

        return method.GetCustomAttributes<RequiresCapabilityAttribute>()
            .Select(attr => attr.Capability)
            .ToList();
    }
}

/// <summary>
/// Represents the result of a capability validation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets a value indicating whether the validation passed.
    /// </summary>
    public bool IsValid { get; }

    /// <summary>
    /// Gets the list of validation errors (empty if IsValid is true).
    /// </summary>
    public IReadOnlyList<string> Errors { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationResult"/> class.
    /// </summary>
    /// <param name="isValid">Whether the validation passed.</param>
    /// <param name="errors">The list of validation errors.</param>
    public ValidationResult(bool isValid, IEnumerable<string> errors)
    {
        IsValid = isValid;
        Errors = errors?.ToList() ?? new List<string>();
    }

    /// <summary>
    /// Gets a formatted error message containing all validation errors.
    /// </summary>
    public string GetErrorMessage()
    {
        if (IsValid)
            return string.Empty;

        return string.Join(Environment.NewLine, Errors);
    }
}
