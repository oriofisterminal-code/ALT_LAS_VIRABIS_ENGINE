using Xunit;
using Virabis.Core.State;
using Virabis.Core.Tags;

namespace Virabis.Core.Tests;

/// <summary>
/// Integration tests for Entity with State Machine and Tags.
/// "Büyük resmi gör" - Dr. Fatma Göz (Fotoğrafçı)
/// </summary>
public class EntityIntegrationTests
{
    // ========================================
    // ENTITY + STATE MACHINE TESTS
    // ========================================

    [Fact, Trait("Category", "Integration")]
    public void Entity_InitializeCombatantStates()
    {
        var entity = new Entity(TeamId.Player, maxHealth: 100);
        entity.InitializeCombatantStates();

        Assert.NotNull(entity.State);
        Assert.Equal(EntityStates.Idle, entity.State.CurrentStateId);
    }

    [Fact, Trait("Category", "Integration")]
    public void Entity_InitializeAIStates()
    {
        var entity = new Entity(TeamId.Enemy, maxHealth: 100);
        entity.InitializeAIStates();

        Assert.NotNull(entity.State);
        Assert.Equal(EntityStates.Idle, entity.State.CurrentStateId);
    }

    [Fact, Trait("Category", "Integration")]
    public void Entity_StateTransition()
    {
        var entity = new Entity(TeamId.Player, maxHealth: 100);
        entity.InitializeCombatantStates();

        var result = entity.TryChangeState(EntityStates.Attacking);

        Assert.True(result);
        Assert.True(entity.IsInState(EntityStates.Attacking));
    }

    [Fact, Trait("Category", "Integration")]
    public void Entity_StateTransition_NotAllowed()
    {
        var entity = new Entity(TeamId.Player, maxHealth: 100);
        entity.InitializeCombatantStates();

        // Dead state can't transition anywhere
        entity.State?.ForceTransition(EntityStates.Dead);
        var result = entity.TryChangeState(EntityStates.Idle);

        Assert.False(result);
        Assert.True(entity.IsInState(EntityStates.Dead));
    }

    [Fact, Trait("Category", "Integration")]
    public void Entity_Update()
    {
        var entity = new Entity(TeamId.Player, maxHealth: 100);
        entity.InitializeCombatantStates();

        entity.Update(0.016f);
        entity.Update(0.016f);

        Assert.Equal(0.032f, entity.State?.TimeInCurrentState, precision: 3);
    }

    // ========================================
    // ENTITY + TAGS TESTS
    // ========================================

    [Fact, Trait("Category", "Integration")]
    public void Entity_AddGameplayTags()
    {
        var entity = new Entity(TeamId.Enemy, maxHealth: 100);
        entity.Tags.AddGameplayTags(
            GameplayTags.Entity.Character.Boss,
            GameplayTags.Status.Alive
        );

        Assert.True(entity.Tags.HasGameplayTagMatch(GameplayTags.Entity.Character.Boss));
        Assert.True(entity.Tags.HasAllTags(GameplayTags.Entity.Character.Boss, GameplayTags.Status.Alive));
    }

    [Fact, Trait("Category", "Integration")]
    public void Entity_TagQuery()
    {
        var entity = new Entity(TeamId.Enemy, maxHealth: 100);
        entity.Tags.AddGameplayTags(
            GameplayTags.Entity.Character.Boss,
            GameplayTags.Status.Alive,
            GameplayTags.Combat.Aware
        );

        var query = TagQuery.Create()
            .HasAny(GameplayTags.Entity.Character.Minion, GameplayTags.Entity.Character.Boss)
            .HasNone(GameplayTags.Status.Dead);

        Assert.True(query.Matches(entity.Tags.GameplayTags));
    }

    // ========================================
    // LEGACY TAG TESTS
    // ========================================

    [Fact, Trait("Category", "Integration")]
    public void Entity_LegacyTags_StillWork()
    {
        var entity = new Entity(TeamId.Enemy, maxHealth: 100);
        entity.Tags.AddTag("aware");
        entity.Tags.AddTag("boss");

        Assert.True(entity.Tags.HasTag("aware"));
        Assert.True(entity.Tags.HasTag("AWARE")); // Case insensitive
        Assert.False(entity.Tags.HasTag("minion"));
    }

    // ========================================
    // COMBINED: STATE + TAGS + HEALTH TESTS
    // ========================================

    [Fact, Trait("Category", "Integration")]
    public void Entity_DeathScenario()
    {
        var entity = new Entity(TeamId.Enemy, maxHealth: 100);
        entity.InitializeCombatantStates();
        entity.Tags.AddGameplayTag(GameplayTags.Status.Alive);

        // Take damage until dead
        entity.Health.ApplyDamage(100);
        entity.State?.ForceTransition(EntityStates.Dead);
        entity.Tags.RemoveGameplayTag(GameplayTags.Status.Alive);
        entity.Tags.AddGameplayTag(GameplayTags.Status.Dead);

        Assert.False(entity.Health.IsAlive);
        Assert.True(entity.IsInState(EntityStates.Dead));
        Assert.True(entity.Tags.HasGameplayTag(GameplayTags.Status.Dead));
    }

    [Fact, Trait("Category", "Integration")]
    public void Entity_CombatScenario()
    {
        var player = new Entity(TeamId.Player, maxHealth: 100);
        player.InitializeCombatantStates();
        player.Tags.AddGameplayTag(GameplayTags.Status.Alive);

        var enemy = new Entity(TeamId.Enemy, maxHealth: 50);
        enemy.InitializeAIStates();
        enemy.Tags.AddGameplayTags(
            GameplayTags.Entity.Character.Minion,
            GameplayTags.Status.Alive
        );

        // Player attacks
        player.TryChangeState(EntityStates.Attacking);
        Assert.True(player.IsInState(EntityStates.Attacking));

        // Enemy becomes alert
        enemy.TryChangeState(EntityStates.Alerted);
        enemy.Tags.AddGameplayTag(GameplayTags.Combat.Aware);
        Assert.True(enemy.Tags.HasGameplayTagMatch(GameplayTags.Combat.Aware));

        // Enemy chases
        enemy.TryChangeState(EntityStates.Chasing);
        Assert.True(enemy.IsInState(EntityStates.Chasing));
    }

    [Fact, Trait("Category", "Integration")]
    public void Entity_StateChange_Events()
    {
        var entity = new Entity(TeamId.Player, maxHealth: 100);
        entity.InitializeCombatantStates();

        StateChangeEvent? lastEvent = null;
        entity.State!.OnStateChange += e => lastEvent = e;

        entity.TryChangeState(EntityStates.Attacking);

        Assert.NotNull(lastEvent);
        Assert.Equal(EntityStates.Idle, lastEvent.Value.PreviousStateId);
        Assert.Equal(EntityStates.Attacking, lastEvent.Value.NewStateId);
    }
}
