using Xunit;
using Virabis.Core.Templates;
using Virabis.Core.Tags;

namespace Virabis.Core.Tests;

/// <summary>
/// Tests for Entity Template system.
/// "Yamaç paraşütü gibi - bir kere tanımla, defalarca kullan" - Zeynep Kod (Aşçı)
/// </summary>
public class EntityTemplateTests
{
    // ========================================
    // BASIC CREATION TESTS
    // ========================================

    [Fact, Trait("Category", "Smoke")]
    public void Template_Instantiate_CreatesEntity()
    {
        var template = new EntityTemplate("TestEnemy")
            .AsEnemy()
            .WithHealth(50);

        var entity = template.Instantiate();

        Assert.NotNull(entity);
        Assert.Equal(TeamId.Enemy, entity.TeamId);
        Assert.Equal(50f, entity.Health.MaxHealth);
    }

    [Fact, Trait("Category", "Unit")]
    public void Template_MultipleInstantiate_CreatesDistinctEntities()
    {
        var template = new EntityTemplate("Test")
            .WithHealth(100);

        var entity1 = template.Instantiate();
        var entity2 = template.Instantiate();

        Assert.NotEqual(entity1.Id, entity2.Id);
    }

    // ========================================
    // CONFIGURATION TESTS
    // ========================================

    [Fact, Trait("Category", "Unit")]
    public void Template_WithHealth_SetsMaxHealth()
    {
        var template = new EntityTemplate("Test")
            .WithHealth(250);

        var entity = template.Instantiate();

        Assert.Equal(250f, entity.Health.MaxHealth);
        Assert.Equal(250f, entity.Health.CurrentHealth);
    }

    [Fact, Trait("Category", "Unit")]
    public void Template_AsPlayer_SetsPlayerTeam()
    {
        var template = new EntityTemplate("Player").AsPlayer();
        var entity = template.Instantiate();

        Assert.Equal(TeamId.Player, entity.TeamId);
    }

    [Fact, Trait("Category", "Unit")]
    public void Template_AsEnemy_SetsEnemyTeam()
    {
        var template = new EntityTemplate("Enemy").AsEnemy();
        var entity = template.Instantiate();

        Assert.Equal(TeamId.Enemy, entity.TeamId);
    }

    [Fact, Trait("Category", "Unit")]
    public void Template_WithCrit_SetsCritStats()
    {
        var template = new EntityTemplate("Test")
            .WithCrit(0.25f, 3.0f);

        var entity = template.Instantiate();

        Assert.Equal(0.25f, entity.Stats.CritChance);
        Assert.Equal(3.0f, entity.Stats.CritMultiplier);
    }

    [Fact, Trait("Category", "Unit")]
    public void Template_WithTag_AddsLegacyTag()
    {
        var template = new EntityTemplate("Test")
            .WithTags("boss", "flying");

        var entity = template.Instantiate();

        Assert.True(entity.Tags.HasTag("boss"));
        Assert.True(entity.Tags.HasTag("flying"));
    }

    [Fact, Trait("Category", "Unit")]
    public void Template_WithGameplayTag_AddsGameplayTag()
    {
        var template = new EntityTemplate("Test")
            .WithGameplayTag("Entity.Character.Boss");

        var entity = template.Instantiate();

        Assert.True(entity.Tags.HasGameplayTagMatch("Entity.Character.Boss"));
    }

    // ========================================
    // STATE CONFIGURATION TESTS
    // ========================================

    [Fact, Trait("Category", "Unit")]
    public void Template_WithCombatantStates_InitializesStateMachine()
    {
        var template = new EntityTemplate("Test")
            .WithCombatantStates();

        var entity = template.Instantiate();

        Assert.NotNull(entity.State);
        Assert.Equal("Idle", entity.State.CurrentStateName);
    }

    [Fact, Trait("Category", "Unit")]
    public void Template_WithAIStates_InitializesStateMachine()
    {
        var template = new EntityTemplate("Test")
            .WithAIStates();

        var entity = template.Instantiate();

        Assert.NotNull(entity.State);
        Assert.Equal("Idle", entity.State.CurrentStateName);
    }

    // ========================================
    // CUSTOM CONFIGURATION TESTS
    // ========================================

    [Fact, Trait("Category", "Unit")]
    public void Template_Configure_AppliesCustomConfig()
    {
        var template = new EntityTemplate("Test")
            .WithHealth(100)
            .Configure(e =>
            {
                e.Tags.AddTag("custom");
                e.Stats.MoveSpeed = 10f;
            });

        var entity = template.Instantiate();

        Assert.True(entity.Tags.HasTag("custom"));
        Assert.Equal(10f, entity.Stats.MoveSpeed);
    }

    // ========================================
    // MULTIPLE INSTANTIATION TESTS
    // ========================================

    [Fact, Trait("Category", "Unit")]
    public void Template_InstantiateCount_CreatesCorrectCount()
    {
        var template = new EntityTemplate("Test")
            .WithHealth(50);

        var entities = template.Instantiate(5).ToList();

        Assert.Equal(5, entities.Count);
        Assert.Equal(5, entities.Select(e => e.Id).Distinct().Count());
    }

    // ========================================
    // PREDEFINED TEMPLATES TESTS
    // ========================================

    [Fact, Trait("Category", "Unit")]
    public void Predefined_Player_HasCorrectDefaults()
    {
        var entity = EntityTemplate.Player().Instantiate();

        Assert.Equal(TeamId.Player, entity.TeamId);
        Assert.Equal(100f, entity.Health.MaxHealth);
        Assert.NotNull(entity.State);
    }

    [Fact, Trait("Category", "Unit")]
    public void Predefined_Enemy_HasCorrectDefaults()
    {
        var entity = EntityTemplate.Enemy().Instantiate();

        Assert.Equal(TeamId.Enemy, entity.TeamId);
        Assert.Equal(100f, entity.Health.MaxHealth);
        Assert.NotNull(entity.State);
    }

    [Fact, Trait("Category", "Unit")]
    public void Predefined_Boss_HasCorrectDefaults()
    {
        var entity = EntityTemplate.Boss().Instantiate();

        Assert.Equal(TeamId.Enemy, entity.TeamId);
        Assert.Equal(500f, entity.Health.MaxHealth);
        Assert.True(entity.Tags.HasTag("boss"));
        Assert.True(entity.Tags.HasTag("aware"));
    }

    [Fact, Trait("Category", "Unit")]
    public void Predefined_Minion_HasCorrectDefaults()
    {
        var entity = EntityTemplate.Minion().Instantiate();

        Assert.Equal(TeamId.Enemy, entity.TeamId);
        Assert.Equal(30f, entity.Health.MaxHealth);
        Assert.True(entity.Tags.HasTag("minion"));
    }

    [Fact, Trait("Category", "Unit")]
    public void Predefined_Elite_HasCorrectDefaults()
    {
        var entity = EntityTemplate.Elite().Instantiate();

        Assert.Equal(TeamId.Enemy, entity.TeamId);
        Assert.Equal(200f, entity.Health.MaxHealth);
        Assert.Equal(0.2f, entity.Stats.CritChance);
        Assert.True(entity.Tags.HasTag("elite"));
    }
}
