using System.Numerics;
using Virabis.Core.AI;
using Xunit;

namespace Virabis.Core.Tests.AI;

public class EnemyAITests
{
    [Fact]
    public void Update_WithNaNPosition_ReturnsIdleState()
    {
        // Arrange
        var config = new EnemyAIConfig
        {
            DetectionRange = 10f,
            AttackRange = 2f,
            AttackCooldown = 1f
        };
        var ai = new EnemyAI(config);
        
        var enemyPos = new Vector3(float.NaN, 0, 0);
        var playerPos = new Vector3(0, 0, 0);

        // Act
        var command = ai.Update(0.016f, enemyPos, playerPos);

        // Assert
        Assert.Equal(AIState.Idle, ai.CurrentState);
        Assert.Equal(Vector3.Zero, command.MoveDirection);
        Assert.False(command.ShouldAttack);
    }

    [Fact]
    public void Update_WithNegativeDeltaTime_ClampsToZeroAndDoesNotBreakCooldown()
    {
        // Arrange
        var config = new EnemyAIConfig
        {
            DetectionRange = 10f,
            AttackRange = 2f,
            AttackCooldown = 1f
        };
        var ai = new EnemyAI(config);
        
        var enemyPos = Vector3.Zero;
        var playerPos = new Vector3(1, 0, 0); // Within attack range

        // Act - Move to attack state and trigger attack
        ai.Update(0.016f, enemyPos, playerPos);
        ai.Update(0.016f, enemyPos, playerPos);
        var firstAttack = ai.Update(1.5f, enemyPos, playerPos); // Trigger attack
        
        // Act - Call with negative deltaTime
        var afterNegative = ai.Update(-0.5f, enemyPos, playerPos);
        
        // Assert - Cooldown should not go backwards
        Assert.True(firstAttack.ShouldAttack);
        Assert.False(afterNegative.ShouldAttack);
        Assert.True(ai.TimeSinceLastAttack >= 0f);
    }

    [Fact]
    public void SetDead_ThenUpdate_RemainsInDeadState()
    {
        // Arrange
        var config = new EnemyAIConfig
        {
            DetectionRange = 10f,
            AttackRange = 2f,
            AttackCooldown = 1f
        };
        var ai = new EnemyAI(config);
        
        var enemyPos = Vector3.Zero;
        var playerPos = new Vector3(5, 0, 0); // Within detection range

        // Act
        ai.SetDead();
        var command1 = ai.Update(0.016f, enemyPos, playerPos);
        var command2 = ai.Update(0.016f, enemyPos, playerPos);
        var command3 = ai.Update(0.016f, enemyPos, playerPos);

        // Assert
        Assert.Equal(AIState.Dead, ai.CurrentState);
        Assert.Equal(Vector3.Zero, command1.MoveDirection);
        Assert.False(command1.ShouldAttack);
        Assert.Equal(Vector3.Zero, command2.MoveDirection);
        Assert.False(command2.ShouldAttack);
        Assert.Equal(Vector3.Zero, command3.MoveDirection);
        Assert.False(command3.ShouldAttack);
    }

    [Fact]
    public void Update_InAttackState_ShouldAttackFalseBeforeCooldownExpires()
    {
        // Arrange
        var config = new EnemyAIConfig
        {
            DetectionRange = 10f,
            AttackRange = 2f,
            AttackCooldown = 1.5f
        };
        var ai = new EnemyAI(config);
        
        var enemyPos = Vector3.Zero;
        var playerPos = new Vector3(1, 0, 0); // Within attack range

        // Act - Move to attack state
        ai.Update(0.016f, enemyPos, playerPos);
        ai.Update(0.016f, enemyPos, playerPos);
        
        // Trigger first attack
        var firstAttack = ai.Update(1.6f, enemyPos, playerPos);
        
        // Try to attack before cooldown expires
        var beforeCooldown1 = ai.Update(0.5f, enemyPos, playerPos);
        var beforeCooldown2 = ai.Update(0.5f, enemyPos, playerPos);
        
        // Attack after cooldown expires
        var afterCooldown = ai.Update(0.6f, enemyPos, playerPos);

        // Assert
        Assert.True(firstAttack.ShouldAttack);
        Assert.False(beforeCooldown1.ShouldAttack);
        Assert.False(beforeCooldown2.ShouldAttack);
        Assert.True(afterCooldown.ShouldAttack);
    }
}
