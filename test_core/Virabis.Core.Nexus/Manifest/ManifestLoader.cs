using System.Text.Json;
using System.Text.RegularExpressions;

namespace Virabis.Core.Nexus.Manifest;

/// <summary>
/// Loads and validates feature manifests from JSON files.
/// </summary>
/// <remarks>
/// ManifestLoader provides static methods to load feature metadata from JSON manifest files.
/// It performs comprehensive validation to ensure manifests meet all requirements.
/// </remarks>
/// <example>
/// Loading a manifest:
/// <code>
/// var manifest = ManifestLoader.Load("features/combat/manifest.json");
/// Console.WriteLine($"Loaded feature: {manifest.Name} v{manifest.Version}");
/// </code>
/// </example>
public static class ManifestLoader
{
    /// <summary>
    /// Loads a feature manifest from the specified JSON file path.
    /// </summary>
    /// <param name="path">The file path to the JSON manifest file.</param>
    /// <returns>A validated <see cref="FeatureManifest"/> instance.</returns>
    /// <exception cref="InvalidManifestException">
    /// Thrown when the manifest file cannot be read, parsed, or fails validation.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="path"/> is null or empty.
    /// </exception>
    /// <example>
    /// <code>
    /// try
    /// {
    ///     var manifest = ManifestLoader.Load("combat_manifest.json");
    ///     Console.WriteLine($"Loaded: {manifest.Name}");
    /// }
    /// catch (InvalidManifestException ex)
    /// {
    ///     Console.WriteLine($"Invalid manifest: {ex.Message}");
    /// }
    /// </code>
    /// </example>
    public static FeatureManifest Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentNullException(nameof(path), "Manifest path cannot be null or empty");
        }

        try
        {
            // Read the JSON file
            var json = File.ReadAllText(path);
            
            // Deserialize with case-insensitive property matching
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            };
            
            var manifest = JsonSerializer.Deserialize<FeatureManifest>(json, options);
            
            if (manifest == null)
            {
                throw new InvalidManifestException($"Failed to deserialize manifest from '{path}': result was null");
            }
            
            // Validate the manifest
            Validate(manifest);
            
            return manifest;
        }
        catch (FileNotFoundException ex)
        {
            throw new InvalidManifestException($"Manifest file not found: '{path}'", ex);
        }
        catch (IOException ex)
        {
            throw new InvalidManifestException($"Error reading manifest file '{path}': {ex.Message}", ex);
        }
        catch (JsonException ex)
        {
            throw new InvalidManifestException($"Invalid JSON in manifest file '{path}': {ex.Message}", ex);
        }
        catch (InvalidManifestException)
        {
            // Re-throw validation exceptions as-is
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidManifestException($"Unexpected error loading manifest from '{path}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Validates a feature manifest according to Nexus v2.0 requirements.
    /// </summary>
    /// <param name="manifest">The manifest to validate.</param>
    /// <exception cref="InvalidManifestException">
    /// Thrown when the manifest fails any validation rule.
    /// </exception>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="manifest"/> is null.
    /// </exception>
    /// <remarks>
    /// Validation rules:
    /// - Name: Required, non-empty
    /// - Version: Required, must match semantic versioning format (e.g., "1.0.0")
    /// - Capabilities: Required, must have at least one capability
    /// - Resources.InitTimeMs: Must be >= 0 if specified
    /// - Resources.MemoryMb: Must be >= 0 if specified
    /// - Resources.CpuPercent: Must be 0-100 if specified
    /// - Lifecycle.Priority: Must be >= 0 if specified
    /// </remarks>
    public static void Validate(FeatureManifest manifest)
    {
        if (manifest == null)
        {
            throw new ArgumentNullException(nameof(manifest), "Manifest cannot be null");
        }

        // Validate Name (required)
        if (string.IsNullOrWhiteSpace(manifest.Name))
        {
            throw new InvalidManifestException("Name is required and cannot be empty");
        }

        // Validate Version (required, semantic versioning format)
        if (string.IsNullOrWhiteSpace(manifest.Version))
        {
            throw new InvalidManifestException("Version is required and cannot be empty");
        }

        if (!IsValidSemanticVersion(manifest.Version))
        {
            throw new InvalidManifestException(
                $"Version '{manifest.Version}' is invalid. Must follow semantic versioning format (e.g., '1.0.0')");
        }

        // Validate Capabilities (required, at least one)
        if (manifest.Capabilities == null || manifest.Capabilities.Length == 0)
        {
            throw new InvalidManifestException("Capabilities are required. At least one capability must be specified");
        }

        // Check for empty capability strings
        if (manifest.Capabilities.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidManifestException("Capabilities cannot contain empty or whitespace-only strings");
        }

        // Validate Dependencies (optional, but no empty strings if present)
        if (manifest.Dependencies != null && manifest.Dependencies.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidManifestException("Dependencies cannot contain empty or whitespace-only strings");
        }

        // Validate Resources (optional)
        if (manifest.Resources != null)
        {
            ValidateResourceBudget(manifest.Resources);
        }

        // Validate Lifecycle (optional)
        if (manifest.Lifecycle != null)
        {
            ValidateLifecycleConfig(manifest.Lifecycle);
        }

        // Validate Events (optional)
        if (manifest.Events != null)
        {
            ValidateEventConfig(manifest.Events);
        }
    }

    /// <summary>
    /// Validates resource budget constraints.
    /// </summary>
    private static void ValidateResourceBudget(ResourceBudget resources)
    {
        if (resources.InitTimeMs < 0)
        {
            throw new InvalidManifestException($"InitTimeMs must be >= 0, got {resources.InitTimeMs}");
        }

        if (resources.MemoryMb < 0)
        {
            throw new InvalidManifestException($"MemoryMb must be >= 0, got {resources.MemoryMb}");
        }

        if (resources.CpuPercent < 0 || resources.CpuPercent > 100)
        {
            throw new InvalidManifestException($"CpuPercent must be between 0 and 100, got {resources.CpuPercent}");
        }
    }

    /// <summary>
    /// Validates lifecycle configuration.
    /// </summary>
    private static void ValidateLifecycleConfig(LifecycleConfig lifecycle)
    {
        if (lifecycle.Priority < 0)
        {
            throw new InvalidManifestException($"Priority must be >= 0, got {lifecycle.Priority}");
        }
    }

    /// <summary>
    /// Validates event configuration.
    /// </summary>
    private static void ValidateEventConfig(EventConfig events)
    {
        if (events.Publishes != null && events.Publishes.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidManifestException("Publishes cannot contain empty or whitespace-only event names");
        }

        if (events.Subscribes != null && events.Subscribes.Any(string.IsNullOrWhiteSpace))
        {
            throw new InvalidManifestException("Subscribes cannot contain empty or whitespace-only event names");
        }
    }

    /// <summary>
    /// Checks if a version string follows semantic versioning format (MAJOR.MINOR.PATCH).
    /// </summary>
    /// <param name="version">The version string to validate.</param>
    /// <returns>True if the version is valid, false otherwise.</returns>
    private static bool IsValidSemanticVersion(string version)
    {
        // Semantic versioning pattern: MAJOR.MINOR.PATCH (e.g., "1.0.0", "2.3.1")
        // Optionally allows pre-release and build metadata (e.g., "1.0.0-alpha", "1.0.0+build.123")
        var pattern = @"^\d+\.\d+\.\d+(-[0-9A-Za-z\-\.]+)?(\+[0-9A-Za-z\-\.]+)?$";
        return Regex.IsMatch(version, pattern);
    }
}
