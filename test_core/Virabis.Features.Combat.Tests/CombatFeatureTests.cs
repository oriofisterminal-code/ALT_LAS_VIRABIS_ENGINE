using Virabis.Core;
using Virabis.Core.Nexus;
using Virabis.Features.Combat;
using Xunit;

namespace Virabis.Features.Combat.Tests;

public class CombatFeatureTests : FeatureTestBase<CombatFeature>
{
    [Fact]
    [Trait("Category", "Smoke")]
    public void CombatFeature_ProcessHit_WorksCorrectly()
    {
        // Arrange
        Feature.Initialize();

        var attacker = new Entity(TeamId.Player, maxHealth: 100);
        attacker.Stats.CritChance = 0.0f;

        var target = new Entity(TeamId.Enemy, maxHealth: 100);

        var context = new HitContext(attacker, target, baseDamage: 10);

        // Act
        float damage = Feature.ProcessHit(context);

        // Assert
        Assert.Equal(10f, damage, precision: 2);
        Assert.Equal(90f, target.Health.CurrentHealth, precision: 2);
    }

    [Fact]
    [Trait("Category", "Smoke")]
    public void CombatFeature_IsHealthy_ReturnsTrueWithGoodFPS()
    {
        // Arrange
        Feature.Initialize();

        // Act - simulate 60 FPS (deltaTime = 1/60)
        Feature.Update(1.0f / 60.0f);

        // Assert
        Assert.True(Feature.IsHealthy());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void CombatFeature_IsHealthy_ReturnsFalseWithBadFPS()
    {
        // Arrange
        Feature.Initialize();

        // Act - simulate 30 FPS (deltaTime = 1/30)
        Feature.Update(1.0f / 30.0f);

        // Assert
        Assert.False(Feature.IsHealthy());
    }

    [Fact]
    [Trait("Category", "Unit")]
    public void CombatFeature_GetHealthStatus_ReturnsCorrectFormat()
    {
        // Arrange
        Feature.Initialize();
        Feature.Update(1.0f / 60.0f); // 60 FPS

        // Act
        string status = Feature.GetHealthStatus();

        // Assert
        Assert.Contains("Combat:", status);
        Assert.Contains("FPS=", status);
        Assert.Contains("Healthy=", status);
    }
}
