using Xunit;
using Virabis.Core.Tags;
using Virabis.Core.State;

namespace Virabis.Core.Tests;

/// <summary>
/// Tests for GameplayTag system.
/// "Cimriyim, gereksiz kod yazmam" - Ayşe Döner (Muhasebeci)
/// </summary>
public class GameplayTagTests
{
    // ========================================
    // TAG CREATION TESTS
    // ========================================

    [Fact, Trait("Category", "Smoke")]
    public void Tag_Create_Simple()
    {
        var tag = GameplayTag.Create("Enemy");
        Assert.Equal("Enemy", tag.Name);
        Assert.Equal(0, tag.Depth);
        Assert.Null(tag.Parent);
    }

    [Fact, Trait("Category", "Unit")]
    public void Tag_Create_Hierarchical()
    {
        var tag = GameplayTag.Create("Enemy.Boss.Flying");
        Assert.Equal("Enemy.Boss.Flying", tag.Name);
        Assert.Equal(2, tag.Depth);
        Assert.NotNull(tag.Parent);
        Assert.Equal("Enemy.Boss", tag.Parent?.Name);
    }

    [Fact, Trait("Category", "Unit")]
    public void Tag_Create_Empty_ReturnsNone()
    {
        var tag = GameplayTag.Create("");
        Assert.Equal(GameplayTag.None, tag);
    }

    [Fact, Trait("Category", "Unit")]
    public void Tag_ImplicitConversion_FromString()
    {
        GameplayTag tag = "Status.Alive";
        Assert.Equal("Status.Alive", tag.Name);
    }

    // ========================================
    // TAG MATCHING TESTS
    // ========================================

    [Fact, Trait("Category", "Unit")]
    public void Tag_Match_ExactMatch()
    {
        var tag1 = GameplayTag.Create("Enemy");
        var tag2 = GameplayTag.Create("Enemy");
        Assert.True(tag1.Matches(tag2));
    }

    [Fact, Trait("Category", "Unit")]
    public void Tag_Match_ParentMatchesChild()
    {
        var parent = GameplayTag.Create("Enemy");
        var child = GameplayTag.Create("Enemy.Boss");
        Assert.True(parent.Matches(child));
    }

    [Fact, Trait("Category", "Unit")]
    public void Tag_Match_ChildDoesNotMatchParent()
    {
        var parent = GameplayTag.Create("Enemy");
        var child = GameplayTag.Create("Enemy.Boss");
        Assert.False(child.Matches(parent));
    }

    [Fact, Trait("Category", "Unit")]
    public void Tag_Match_DeepHierarchy()
    {
        var root = GameplayTag.Create("Entity");
        var deep = GameplayTag.Create("Entity.Character.Boss.Flying");
        Assert.True(root.Matches(deep));
    }

    // ========================================
    // CONTAINER TESTS
    // ========================================

    [Fact, Trait("Category", "Unit")]
    public void Container_AddAndHasTag()
    {
        var container = new GameplayTagContainer();
        container.AddTag("Enemy.Boss");

        Assert.True(container.HasTag("Enemy.Boss"));
        Assert.False(container.HasTag("Enemy.Minion"));
    }

    [Fact, Trait("Category", "Unit")]
    public void Container_HasAny()
    {
        var container = new GameplayTagContainer();
        container.AddTags("Enemy.Boss", "Status.Alive");

        Assert.True(container.HasAny("Enemy.Minion", "Enemy.Boss"));
        Assert.False(container.HasAny("Enemy.Minion", "Status.Dead"));
    }

    [Fact, Trait("Category", "Unit")]
    public void Container_HasAll()
    {
        var container = new GameplayTagContainer();
        container.AddTags("Enemy.Boss", "Status.Alive", "Combat.Aware");

        Assert.True(container.HasAll("Enemy.Boss", "Status.Alive"));
        Assert.False(container.HasAll("Enemy.Boss", "Status.Dead"));
    }

    [Fact, Trait("Category", "Unit")]
    public void Container_HasNone()
    {
        var container = new GameplayTagContainer();
        container.AddTags("Enemy.Boss");

        Assert.True(container.HasNone("Status.Dead", "Status.Stunned"));
        Assert.False(container.HasNone("Enemy.Boss"));
    }

    [Fact, Trait("Category", "Unit")]
    public void Container_RemoveTag()
    {
        var container = new GameplayTagContainer();
        container.AddTag("Enemy.Boss");

        container.RemoveTag("Enemy.Boss");
        Assert.False(container.HasTag("Enemy.Boss"));
    }

    // ========================================
    // TAG QUERY TESTS
    // ========================================

    [Fact, Trait("Category", "Unit")]
    public void Query_HasAny_Matches()
    {
        var container = new GameplayTagContainer();
        container.AddTags("Enemy.Boss", "Status.Alive");

        var query = TagQuery.Create()
            .HasAny("Enemy.Minion", "Enemy.Boss");

        Assert.True(query.Matches(container));
    }

    [Fact, Trait("Category", "Unit")]
    public void Query_Complex_Combination()
    {
        var container = new GameplayTagContainer();
        container.AddTags("Enemy.Boss", "Status.Alive", "Combat.Aware");

        var query = TagQuery.Create()
            .HasAny("Enemy.Minion", "Enemy.Boss")
            .HasAll("Status.Alive", "Combat.Aware")
            .HasNone("Status.Dead", "Status.Stunned");

        Assert.True(query.Matches(container));
    }

    // ========================================
    // PREDEFINED TAGS TESTS
    // ========================================

    [Fact, Trait("Category", "Unit")]
    public void PredefinedTags_Entity()
    {
        Assert.Equal("Entity.Player", GameplayTags.Entity.Player.Name);
        Assert.Equal("Entity.Enemy", GameplayTags.Entity.Enemy.Name);
        Assert.Equal("Entity.Character.Boss", GameplayTags.Entity.Character.Boss.Name);
    }

    [Fact, Trait("Category", "Unit")]
    public void PredefinedTags_Status()
    {
        Assert.Equal("Status.Alive", GameplayTags.Status.Alive.Name);
        Assert.Equal("Status.Dead", GameplayTags.Status.Dead.Name);
        Assert.Equal("Status.Debuff.Poisoned", GameplayTags.Status.Debuff.Poisoned.Name);
    }

    [Fact, Trait("Category", "Unit")]
    public void PredefinedTags_RegisterAll()
    {
        GameplayTags.RegisterAll();

        var enemyTag = GameplayTagRegistry.Get("Entity.Enemy");
        Assert.NotEqual(GameplayTag.None, enemyTag);
    }

    // ========================================
    // TAG REGISTRY TESTS
    // ========================================

    [Fact, Trait("Category", "Unit")]
    public void Registry_GetChildren()
    {
        GameplayTagRegistry.Register("Enemy.Boss");
        GameplayTagRegistry.Register("Enemy.Minion");
        GameplayTagRegistry.Register("Enemy.Boss.Flying");

        var children = GameplayTagRegistry.GetChildren("Enemy").ToList();
        Assert.Equal(2, children.Count);
    }
}
