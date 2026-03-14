namespace Virabis.Core;

/// <summary>
/// Factory interface for creating Entity instances.
/// Enables deterministic ID generation for testing.
/// </summary>
/// <remarks>
/// v3.0: Added for testability - Entity IDs can now be controlled in tests.
///
/// Usage in production:
/// <code>
/// services.AddSingleton&lt;IEntityFactory, EntityFactory&gt;();
/// </code>
///
/// Usage in tests:
/// <code>
/// var factory = new TestEntityFactory();
/// var entity = factory.Create(TeamId.Player, 100);
/// // entity.Id is now predictable
/// </code>
/// </remarks>
public interface IEntityFactory
{
    /// <summary>
    /// Creates a new Entity with the specified team and max health.
    /// </summary>
    /// <param name="teamId">Team identifier for the entity</param>
    /// <param name="maxHealth">Maximum health points</param>
    /// <returns>A new Entity instance with a unique ID</returns>
    Entity Create(TeamId teamId, float maxHealth);

    /// <summary>
    /// Creates a new Entity with a specific ID (useful for reconstruction).
    /// </summary>
    /// <param name="id">Specific GUID for the entity</param>
    /// <param name="teamId">Team identifier</param>
    /// <param name="maxHealth">Maximum health points</param>
    /// <returns>A new Entity instance with the specified ID</returns>
    Entity CreateWithId(Guid id, TeamId teamId, float maxHealth);
}
