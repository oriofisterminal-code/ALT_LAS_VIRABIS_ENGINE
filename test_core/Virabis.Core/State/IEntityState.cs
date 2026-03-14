namespace Virabis.Core.State;

/// <summary>
/// Interface for entity states.
/// "Sabitim, her şey yerli yerinde olsun" - Zeynep Kod (Aşçı)
/// </summary>
public interface IEntityState
{
    /// <summary>
    /// Unique identifier for this state.
    /// </summary>
    int Id { get; }

    /// <summary>
    /// Human-readable name for debugging.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// States that can be transitioned to from this state.
    /// </summary>
    IReadOnlyCollection<int> AllowedTransitions { get; }

    /// <summary>
    /// Called when entering this state.
    /// </summary>
    void OnEnter() { }

    /// <summary>
    /// Called when exiting this state.
    /// </summary>
    void OnExit() { }

    /// <summary>
    /// Called every frame while in this state.
    /// </summary>
    void OnUpdate(float deltaTime) { }
}
