using Xunit;
using Virabis.Core.Debugger;
using Virabis.Core.Templates;

namespace Virabis.Core.Tests;

/// <summary>
/// Tests for Debug System.
/// "Her şeyi görürüm, hiçbir şey kaçmaz" - Kemal Dedektif (Roman Yazarı)
/// </summary>
public class DebugSystemTests
{
    public DebugSystemTests()
    {
        DebugOverlay.Reset();
    }

    // ========================================
    // STAT TRACKER TESTS
    // ========================================

    [Fact, Trait("Category", "Smoke")]
    public void StatTracker_Record_StoresValue()
    {
        var tracker = new StatTracker();

        tracker.Record("FPS", 60f);

        var value = tracker.Get("FPS");
        Assert.Equal(60f, value);
    }

    [Fact, Trait("Category", "Unit")]
    public void StatTracker_Increment_AddsToTotal()
    {
        var tracker = new StatTracker();

        tracker.Increment("Kills");
        tracker.Increment("Kills");
        tracker.Increment("Kills", 2);

        var total = tracker.GetTotal("Kills");
        Assert.Equal(4f, total);
    }

    [Fact, Trait("Category", "Unit")]
    public void StatTracker_Average_CalculatesCorrectly()
    {
        var tracker = new StatTracker();

        tracker.Record("FPS", 60f);
        tracker.Record("FPS", 30f);
        tracker.Record("FPS", 90f);

        var avg = tracker.GetAverage("FPS");
        Assert.Equal(60f, avg);
    }

    [Fact, Trait("Category", "Unit")]
    public void StatTracker_Range_ReturnsMinMax()
    {
        var tracker = new StatTracker();

        tracker.Record("Damage", 10f);
        tracker.Record("Damage", 50f);
        tracker.Record("Damage", 30f);

        var (min, max) = tracker.GetRange("Damage");
        Assert.Equal(10f, min);
        Assert.Equal(50f, max);
    }

    [Fact, Trait("Category", "Unit")]
    public void StatTracker_Set_OverridesValue()
    {
        var tracker = new StatTracker();

        tracker.Record("Count", 5f);
        tracker.Set("Count", 100f);

        var value = tracker.Get("Count");
        Assert.Equal(100f, value);
    }

    [Fact, Trait("Category", "Unit")]
    public void StatTracker_Extensions_WorkCorrectly()
    {
        var tracker = new StatTracker();

        tracker.RecordDamageDealt(50f);
        tracker.RecordDamageDealt(30f);
        tracker.RecordKill();
        tracker.RecordFPS(60f);

        Assert.Equal(80f, tracker.GetTotal(StatTracker.Stats.DamageDealt));
        Assert.Equal(1f, tracker.GetTotal(StatTracker.Stats.Kills));
        Assert.Equal(60f, tracker.Get(StatTracker.Stats.FPS));
    }

    [Fact, Trait("Category", "Unit")]
    public void StatTracker_Clear_RemovesStats()
    {
        var tracker = new StatTracker();
        tracker.Record("Test", 100f);

        tracker.Clear();

        Assert.Null(tracker.Get("Test"));
    }

    // ========================================
    // ENTITY DEBUGGER TESTS
    // ========================================

    [Fact, Trait("Category", "Smoke")]
    public void EntityDebugger_Register_TracksEntity()
    {
        var debugger = new EntityDebugger();
        var entity = EntityTemplate.Enemy().Instantiate();

        debugger.Register(entity);

        Assert.Equal(1, debugger.EntityCount);
    }

    [Fact, Trait("Category", "Unit")]
    public void EntityDebugger_Unregister_RemovesEntity()
    {
        var debugger = new EntityDebugger();
        var entity = EntityTemplate.Enemy().Instantiate();

        debugger.Register(entity);
        debugger.Unregister(entity.Id);

        Assert.Equal(0, debugger.EntityCount);
    }

    [Fact, Trait("Category", "Unit")]
    public void EntityDebugger_GetSnapshot_ReturnsCorrectData()
    {
        var debugger = new EntityDebugger();
        var entity = EntityTemplate.Player().Instantiate();

        var snapshot = debugger.GetSnapshot(entity);

        Assert.Equal(entity.Id, snapshot.Id);
        Assert.Equal(TeamId.Player.ToString(), snapshot.Team);
        Assert.Equal(100f, snapshot.MaxHealth);
    }

    [Fact, Trait("Category", "Unit")]
    public void EntityDebugger_GetByTeam_FiltersCorrectly()
    {
        var debugger = new EntityDebugger();

        debugger.Register(EntityTemplate.Player().Instantiate());
        debugger.Register(EntityTemplate.Enemy().Instantiate());
        debugger.Register(EntityTemplate.Enemy().Instantiate());

        var enemies = debugger.GetByTeam(TeamId.Enemy);
        var players = debugger.GetByTeam(TeamId.Player);

        Assert.Equal(2, enemies.Count());
        Assert.Single(players);
    }

    [Fact, Trait("Category", "Unit")]
    public void EntityDebugger_GetRecentEvents_ReturnsEvents()
    {
        var debugger = new EntityDebugger();
        var entity = EntityTemplate.Enemy().Instantiate();

        debugger.Register(entity);
        debugger.LogEvent("Test", entity.Id, "Test event");

        var events = debugger.GetRecentEvents(5).ToList();

        Assert.Contains(events, e => e.EventType == "Test");
    }

    [Fact, Trait("Category", "Unit")]
    public void EntityDebugger_GetFormattedOutput_ContainsData()
    {
        var debugger = new EntityDebugger();
        var entity = EntityTemplate.Boss().Instantiate();

        debugger.Register(entity, "Big Boss");

        var output = debugger.GetFormattedOutput();

        Assert.Contains("Big Boss", output);
        Assert.Contains("Boss", output);
    }

    // ========================================
    // DEBUG OVERLAY TESTS
    // ========================================

    [Fact, Trait("Category", "Smoke")]
    public void DebugOverlay_Instance_Singleton()
    {
        var instance1 = DebugOverlay.Instance;
        var instance2 = DebugOverlay.Instance;

        Assert.Same(instance1, instance2);
    }

    [Fact, Trait("Category", "Unit")]
    public void DebugOverlay_RegisterEntity_TracksEntity()
    {
        var overlay = new DebugOverlay();
        var entity = EntityTemplate.Player().Instantiate();

        overlay.RegisterEntity(entity, "Player1");

        Assert.Equal(1, overlay.EntityDebugger.EntityCount);
    }

    [Fact, Trait("Category", "Unit")]
    public void DebugOverlay_AddField_CustomField()
    {
        var overlay = new DebugOverlay();
        var counter = 0;

        overlay.AddField("Counter", () => counter.ToString());
        counter = 42;

        var data = overlay.GetDebugData();
        Assert.Contains("Custom_Counter", data.Keys);
    }

    [Fact, Trait("Category", "Unit")]
    public void DebugOverlay_GetCompactText_ReturnsFormattedText()
    {
        var overlay = new DebugOverlay();
        overlay.Stats.RecordFPS(60f);
        overlay.Stats.RecordKill();

        var text = overlay.GetCompactText();

        Assert.Contains("FPS: 60", text);
        Assert.Contains("Kills: 1", text);
    }

    [Fact, Trait("Category", "Unit")]
    public void DebugOverlay_GetOverlayText_ContainsAllSections()
    {
        var overlay = new DebugOverlay();
        var entity = EntityTemplate.Player().Instantiate();

        overlay.RegisterEntity(entity);
        overlay.Stats.RecordFPS(60f);

        var text = overlay.GetOverlayText();

        Assert.Contains("VIRABIS DEBUG", text);
        Assert.Contains("STAT TRACKER", text);
        Assert.Contains("ENTITY DEBUGGER", text);
    }

    [Fact, Trait("Category", "Unit")]
    public void DebugOverlay_GetJson_ReturnsValidJson()
    {
        var overlay = new DebugOverlay();
        overlay.Stats.Record("Test", 123f);

        var json = overlay.GetJson();

        Assert.Contains("\"Test\"", json);
        Assert.Contains("123", json);
    }

    [Fact, Trait("Category", "Unit")]
    public void DebugOverlay_IsEnabled_TogglesOutput()
    {
        var overlay = new DebugOverlay();
        overlay.IsEnabled = false;

        var text = overlay.GetCompactText();

        Assert.Equal("", text);
    }

    // ========================================
    // INTEGRATION TESTS
    // ========================================

    [Fact, Trait("Category", "Integration")]
    public void Debug_System_Integration_Workflow()
    {
        var overlay = new DebugOverlay();

        // Create and register entities
        var player = EntityTemplate.Player().Instantiate();
        var enemy = EntityTemplate.Enemy(50f).Instantiate();
        var boss = EntityTemplate.Boss().Instantiate();

        overlay.RegisterEntity(player, "Hero");
        overlay.RegisterEntity(enemy, "Grunt");
        overlay.RegisterEntity(boss, "Big Boss");

        // Record combat
        overlay.Stats.RecordDamageDealt(25f);
        overlay.Stats.RecordDamageDealt(30f);
        overlay.Stats.RecordKill();

        // Verify
        Assert.Equal(3, overlay.EntityDebugger.EntityCount);
        Assert.Equal(55f, overlay.Stats.GetTotal(StatTracker.Stats.DamageDealt));

        var enemies = overlay.EntityDebugger.GetByTeam(TeamId.Enemy);
        Assert.Equal(2, enemies.Count());

        // Get formatted output
        var output = overlay.GetFormattedOutput();
        Assert.Contains("Hero", output);
        Assert.Contains("DamageDealt: 55", output);
    }

    [Fact, Trait("Category", "Integration")]
    public void Entity_Extension_RegisterForDebug()
    {
        DebugOverlay.Reset();

        var entity = EntityTemplate.Player().Instantiate();
        entity.RegisterForDebug("TestPlayer");

        Assert.Equal(1, DebugOverlay.Instance.EntityDebugger.EntityCount);
    }
}
