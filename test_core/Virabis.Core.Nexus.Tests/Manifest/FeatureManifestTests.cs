using Xunit;
using Virabis.Core.Nexus.Manifest;

namespace Virabis.Core.Nexus.Tests.Manifest;

/// <summary>
/// Unit tests for FeatureManifest loading and validation.
/// Tests manifest parsing, validation rules, and error handling.
/// </summary>
public class FeatureManifestTests
{
    /// <summary>
    /// Helper method to get the correct path to manifest files from the test bin directory.
    /// </summary>
    private static string GetManifestPath(string relativePath)
    {
        // Get the directory where the test assembly is located
        var testAssemblyPath = Path.GetDirectoryName(typeof(FeatureManifestTests).Assembly.Location)!;
        
        // Navigate up to the core directory and then to the target feature directory
        // From: core/Virabis.Core.Nexus.Tests/bin/Debug/net8.0
        // To: core/Virabis.Features.*/
        var corePath = Path.GetFullPath(Path.Combine(testAssemblyPath, "..", "..", "..", ".."));
        return Path.Combine(corePath, relativePath);
    }

    #region Valid Manifest Loading Tests (Task 1.5.1)

    [Fact]
    [Trait("Category", "Smoke")]
    public void Load_CombatManifest_Success()
    {
        // Arrange
        var path = GetManifestPath("Virabis.Features.Combat/combat_manifest.json");

        // Act
        var manifest = ManifestLoader.Load(path);

        // Assert
        Assert.NotNull(manifest);
        Assert.Equal("Combat", manifest.Name);
        Assert.Equal("1.0.0", manifest.Version);
        Assert.Equal("Virabis Team", manifest.Author);
        Assert.Contains("damage", manifest.Capabilities);
        Assert.Contains("entity", manifest.Capabilities);
        Assert.Contains("combat", manifest.Capabilities);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Load_TestManifest_Success()
    {
        // Arrange
        var path = GetManifestPath("Virabis.Features.Test/test_manifest.json");

        // Act
        var manifest = ManifestLoader.Load(path);

        // Assert
        Assert.NotNull(manifest);
        Assert.Equal("Test", manifest.Name);
        Assert.Equal("1.0.0", manifest.Version);
        Assert.Contains("testing", manifest.Capabilities);
        Assert.Contains("validation", manifest.Capabilities);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Load_MCPManifest_Success()
    {
        // Arrange
        var path = GetManifestPath("Virabis.Features.MCP/mcp_manifest.json");

        // Act
        var manifest = ManifestLoader.Load(path);

        // Assert
        Assert.NotNull(manifest);
        Assert.Equal("MCP", manifest.Name);
        Assert.Equal("1.0.0", manifest.Version);
        Assert.Contains("mcp", manifest.Capabilities);
        Assert.Contains("memory", manifest.Capabilities);
        Assert.Contains("search", manifest.Capabilities);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void Load_ValidManifest_ParsesResourceBudget()
    {
        // Arrange
        var path = GetManifestPath("Virabis.Features.Combat/combat_manifest.json");

        // Act
        var manifest = ManifestLoader.Load(path);

        // Assert
        Assert.NotNull(manifest.Resources);
        Assert.Equal(50, manifest.Resources.InitTimeMs);
        Assert.Equal(5, manifest.Resources.MemoryMb);
        Assert.Equal(3, manifest.Resources.CpuPercent);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Load_ValidManifest_ParsesLifecycleConfig()
    {
        // Arrange
        var path = GetManifestPath("Virabis.Features.Combat/combat_manifest.json");

        // Act
        var manifest = ManifestLoader.Load(path);

        // Assert
        Assert.NotNull(manifest.Lifecycle);
        Assert.False(manifest.Lifecycle.AsyncInit);
        Assert.False(manifest.Lifecycle.LazyLoad);
        Assert.False(manifest.Lifecycle.AutoShutdown);
        Assert.Equal(100, manifest.Lifecycle.Priority);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Load_ValidManifest_ParsesEventConfig()
    {
        // Arrange
        var path = GetManifestPath("Virabis.Features.Combat/combat_manifest.json");

        // Act
        var manifest = ManifestLoader.Load(path);

        // Assert
        Assert.NotNull(manifest.Events);
        Assert.Contains("DamageDealt", manifest.Events.Publishes);
        Assert.Contains("EntityDied", manifest.Events.Publishes);
        Assert.Contains("EntitySpawned", manifest.Events.Subscribes);
    }

    #endregion

    #region Invalid Manifest Rejection Tests (Task 1.5.2)

    [Fact]
    [Trait("Category", "Unit")]
    public void Load_InvalidManifest_ThrowsException()
    {
        // Arrange
        var path = "TestData/invalid_manifest.json";

        // Act & Assert
        var exception = Assert.Throws<InvalidManifestException>(() => ManifestLoader.Load(path));
        Assert.NotNull(exception.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Load_NonExistentFile_ThrowsException()
    {
        // Arrange
        var path = "nonexistent_manifest.json";

        // Act & Assert
        var exception = Assert.Throws<InvalidManifestException>(() => ManifestLoader.Load(path));
        Assert.Contains("not found", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Load_NullPath_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ManifestLoader.Load(null!));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Load_EmptyPath_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ManifestLoader.Load(""));
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Load_WhitespacePath_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => ManifestLoader.Load("   "));
    }

    #endregion

    #region Validation Rules Tests (Task 1.5.3)

    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_EmptyName_ThrowsException()
    {
        // Arrange
        var manifest = new FeatureManifest
        {
            Name = "",
            Version = "1.0.0",
            Capabilities = new[] { "test" }
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidManifestException>(() => ManifestLoader.Validate(manifest));
        Assert.Contains("Name", exception.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_NullName_ThrowsException()
    {
        // Arrange
        var manifest = new FeatureManifest
        {
            Name = null!,
            Version = "1.0.0",
            Capabilities = new[] { "test" }
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidManifestException>(() => ManifestLoader.Validate(manifest));
        Assert.Contains("Name", exception.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_WhitespaceName_ThrowsException()
    {
        // Arrange
        var manifest = new FeatureManifest
        {
            Name = "   ",
            Version = "1.0.0",
            Capabilities = new[] { "test" }
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidManifestException>(() => ManifestLoader.Validate(manifest));
        Assert.Contains("Name", exception.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_EmptyVersion_ThrowsException()
    {
        // Arrange
        var manifest = new FeatureManifest
        {
            Name = "TestFeature",
            Version = "",
            Capabilities = new[] { "test" }
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidManifestException>(() => ManifestLoader.Validate(manifest));
        Assert.Contains("Version", exception.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_InvalidVersionFormat_ThrowsException()
    {
        // Arrange
        var manifest = new FeatureManifest
        {
            Name = "TestFeature",
            Version = "invalid-version",
            Capabilities = new[] { "test" }
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidManifestException>(() => ManifestLoader.Validate(manifest));
        Assert.Contains("Version", exception.Message);
        Assert.Contains("semantic versioning", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_NoCapabilities_ThrowsException()
    {
        // Arrange
        var manifest = new FeatureManifest
        {
            Name = "TestFeature",
            Version = "1.0.0",
            Capabilities = Array.Empty<string>()
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidManifestException>(() => ManifestLoader.Validate(manifest));
        Assert.Contains("Capabilities", exception.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_NullCapabilities_ThrowsException()
    {
        // Arrange
        var manifest = new FeatureManifest
        {
            Name = "TestFeature",
            Version = "1.0.0",
            Capabilities = null!
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidManifestException>(() => ManifestLoader.Validate(manifest));
        Assert.Contains("Capabilities", exception.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_EmptyCapabilityString_ThrowsException()
    {
        // Arrange
        var manifest = new FeatureManifest
        {
            Name = "TestFeature",
            Version = "1.0.0",
            Capabilities = new[] { "valid", "", "another" }
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidManifestException>(() => ManifestLoader.Validate(manifest));
        Assert.Contains("Capabilities", exception.Message);
        Assert.Contains("empty", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_NegativeInitTimeMs_ThrowsException()
    {
        // Arrange
        var manifest = new FeatureManifest
        {
            Name = "TestFeature",
            Version = "1.0.0",
            Capabilities = new[] { "test" },
            Resources = new ResourceBudget { InitTimeMs = -100 }
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidManifestException>(() => ManifestLoader.Validate(manifest));
        Assert.Contains("InitTimeMs", exception.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_NegativeMemoryMb_ThrowsException()
    {
        // Arrange
        var manifest = new FeatureManifest
        {
            Name = "TestFeature",
            Version = "1.0.0",
            Capabilities = new[] { "test" },
            Resources = new ResourceBudget { MemoryMb = -50 }
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidManifestException>(() => ManifestLoader.Validate(manifest));
        Assert.Contains("MemoryMb", exception.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_InvalidCpuPercent_ThrowsException()
    {
        // Arrange
        var manifest = new FeatureManifest
        {
            Name = "TestFeature",
            Version = "1.0.0",
            Capabilities = new[] { "test" },
            Resources = new ResourceBudget { CpuPercent = 150 }
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidManifestException>(() => ManifestLoader.Validate(manifest));
        Assert.Contains("CpuPercent", exception.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_NegativePriority_ThrowsException()
    {
        // Arrange
        var manifest = new FeatureManifest
        {
            Name = "TestFeature",
            Version = "1.0.0",
            Capabilities = new[] { "test" },
            Lifecycle = new LifecycleConfig { Priority = -10 }
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidManifestException>(() => ManifestLoader.Validate(manifest));
        Assert.Contains("Priority", exception.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void Validate_ValidManifest_NoException()
    {
        // Arrange
        var manifest = new FeatureManifest
        {
            Name = "TestFeature",
            Version = "1.0.0",
            Capabilities = new[] { "test", "validation" },
            Resources = new ResourceBudget
            {
                InitTimeMs = 100,
                MemoryMb = 10,
                CpuPercent = 5
            },
            Lifecycle = new LifecycleConfig
            {
                Priority = 50
            }
        };

        // Act & Assert
        Assert.Null(Record.Exception(() => ManifestLoader.Validate(manifest)));
    }

    #endregion

    #region Error Message Tests (Task 1.5.4)

    [Fact]
    [Trait("Category", "Unit")]
    public void ErrorMessage_EmptyName_IsDescriptive()
    {
        // Arrange
        var manifest = new FeatureManifest
        {
            Name = "",
            Version = "1.0.0",
            Capabilities = new[] { "test" }
        };

        // Act
        var exception = Assert.Throws<InvalidManifestException>(() => ManifestLoader.Validate(manifest));

        // Assert
        Assert.Contains("Name", exception.Message);
        Assert.Contains("required", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ErrorMessage_InvalidVersion_IsDescriptive()
    {
        // Arrange
        var manifest = new FeatureManifest
        {
            Name = "TestFeature",
            Version = "not-a-version",
            Capabilities = new[] { "test" }
        };

        // Act
        var exception = Assert.Throws<InvalidManifestException>(() => ManifestLoader.Validate(manifest));

        // Assert
        Assert.Contains("Version", exception.Message);
        Assert.Contains("not-a-version", exception.Message);
        Assert.Contains("semantic versioning", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ErrorMessage_NoCapabilities_IsDescriptive()
    {
        // Arrange
        var manifest = new FeatureManifest
        {
            Name = "TestFeature",
            Version = "1.0.0",
            Capabilities = Array.Empty<string>()
        };

        // Act
        var exception = Assert.Throws<InvalidManifestException>(() => ManifestLoader.Validate(manifest));

        // Assert
        Assert.Contains("Capabilities", exception.Message);
        Assert.Contains("required", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ErrorMessage_NegativeResource_IncludesValue()
    {
        // Arrange
        var manifest = new FeatureManifest
        {
            Name = "TestFeature",
            Version = "1.0.0",
            Capabilities = new[] { "test" },
            Resources = new ResourceBudget { InitTimeMs = -100 }
        };

        // Act
        var exception = Assert.Throws<InvalidManifestException>(() => ManifestLoader.Validate(manifest));

        // Assert
        Assert.Contains("InitTimeMs", exception.Message);
        Assert.Contains("-100", exception.Message);
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void ErrorMessage_FileNotFound_IncludesPath()
    {
        // Arrange
        var path = "nonexistent_file.json";

        // Act
        var exception = Assert.Throws<InvalidManifestException>(() => ManifestLoader.Load(path));

        // Assert
        Assert.Contains("not found", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(path, exception.Message);
    }

    #endregion
}
