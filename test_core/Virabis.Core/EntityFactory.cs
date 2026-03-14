using Virabis.Core.Abstractions;

namespace Virabis.Core;

/// <summary>
/// Default factory for creating Entity instances.
/// Uses sequential GUID generation for better database performance.
/// </summary>
/// <remarks>
/// v3.0: Added for testability - Entity IDs can now be controlled.
///
/// Sequential GUID pattern:
/// - First 10 bytes: Timestamp for ordering
/// - Last 6 bytes: Random for uniqueness
///
/// Benefits:
/// - Better cache locality
/// - Database-friendly (less page fragmentation)
/// - Deterministic in tests
/// </remarks>
public class EntityFactory : IEntityFactory
{
    private long _counter = 0;

    /// <inheritdoc />
    public Entity Create(TeamId teamId, float maxHealth)
    {
        return new Entity(GenerateSequentialGuid(), teamId, maxHealth);
    }

    /// <inheritdoc />
    public Entity CreateWithId(Guid id, TeamId teamId, float maxHealth)
    {
        return new Entity(id, teamId, maxHealth);
    }

    /// <summary>
    /// Generates a sequential GUID for better performance.
    /// </summary>
    private Guid GenerateSequentialGuid()
    {
        var timestamp = DateTime.UtcNow.Ticks;
        var counter = Interlocked.Increment(ref _counter);

        var bytes = new byte[16];
        BitConverter.TryWriteBytes(bytes.AsSpan(0, 8), timestamp);
        BitConverter.TryWriteBytes(bytes.AsSpan(8, 8), counter);

        return new Guid(bytes);
    }
}

/// <summary>
/// Test factory with deterministic ID generation.
/// </summary>
/// <remarks>
/// Usage in tests:
/// <code>
/// var factory = new TestEntityFactory();
/// factory.SetNextId(new Guid("00000000-0000-0000-0000-000000000001"));
/// var entity = factory.Create(TeamId.Player, 100);
/// Assert.Equal(new Guid("00000000-0000-0000-0000-000000000001"), entity.Id);
/// </code>
/// </remarks>
public class TestEntityFactory : IEntityFactory
{
    private Guid _nextId = Guid.NewGuid();
    private int _sequentialCounter = 0;

    /// <summary>
    /// Sets the next ID to be used for entity creation.
    /// </summary>
    public void SetNextId(Guid id)
    {
        _nextId = id;
    }

    /// <summary>
    /// Resets to sequential ID generation starting from 1.
    /// </summary>
    public void ResetSequential()
    {
        _sequentialCounter = 0;
    }

    /// <inheritdoc />
    public Entity Create(TeamId teamId, float maxHealth)
    {
        _sequentialCounter++;
        var id = new Guid($"00000000-0000-0000-0000-{_sequentialCounter:D12}");
        return new Entity(id, teamId, maxHealth);
    }

    /// <inheritdoc />
    public Entity CreateWithId(Guid id, TeamId teamId, float maxHealth)
    {
        return new Entity(id, teamId, maxHealth);
    }
}
