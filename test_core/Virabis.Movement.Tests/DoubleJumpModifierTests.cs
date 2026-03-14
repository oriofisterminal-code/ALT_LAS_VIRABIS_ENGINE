using System.Numerics;
using Virabis.Movement.Core;
using Virabis.Movement.Core.Modifiers;

namespace Virabis.Movement.Tests;

/// <summary>
/// Unit tests for DoubleJumpModifier.
/// Tests verify jump counter behavior, floor detection, and jump execution.
/// </summary>
public class DoubleJumpModifierTests
{
    private readonly DoubleJumpModifier _modifier = new();
    private const float JumpForce = 5.0f;

    /// <summary>
    /// Test 1.2.1: Yerde sayaç sıfırlanıyor mu? (Counter resets on floor)
    /// Verifies that jump counter resets to 0 when character is on floor.
    /// </summary>
    [Fact]
    public void ModifyVelocity_OnFloor_ResetsJumpCounter()
    {
        // Arrange
        var velocity = new Vector3(0, 0, 0);
        var context = new MovementContext
        {
            IsOnFloor = true,
            JumpRequested = false,
            JumpForce = JumpForce,
            CurrentVelocity = velocity
        };

        // Act - Simulate being on floor
        var result = _modifier.ModifyVelocity(velocity, context);

        // Assert - Counter should be reset, velocity unchanged
        Assert.Equal(velocity, result);
        
        // Verify counter is reset by checking next jump attempt in air
        var airContext = new MovementContext
        {
            IsOnFloor = false,
            JumpRequested = true,
            JumpForce = JumpForce,
            CurrentVelocity = velocity
        };
        
        var jumpResult = _modifier.ModifyVelocity(velocity, airContext);
        // First jump should work (counter was reset)
        Assert.Equal(JumpForce, jumpResult.Y);
    }

    /// <summary>
    /// Test 1.2.2: Havada ikinci zıplama yapılabiliyor mu? (Second jump in air works)
    /// Verifies that a second jump can be performed while in air with jump input.
    /// </summary>
    [Fact]
    public void ModifyVelocity_InAirWithJumpInput_AllowsSecondJump()
    {
        // Arrange
        var velocity = new Vector3(0, 0, 0);
        
        // First, reset counter by being on floor
        var floorContext = new MovementContext
        {
            IsOnFloor = true,
            JumpRequested = false,
            JumpForce = JumpForce,
            CurrentVelocity = velocity
        };
        _modifier.ModifyVelocity(velocity, floorContext);

        // Act - First jump (in air, with jump input)
        var airContext1 = new MovementContext
        {
            IsOnFloor = false,
            JumpRequested = true,
            JumpForce = JumpForce,
            CurrentVelocity = velocity
        };
        var firstJumpResult = _modifier.ModifyVelocity(velocity, airContext1);

        // Assert - First jump should apply force
        Assert.Equal(JumpForce, firstJumpResult.Y);

        // Act - Second jump (still in air, with jump input)
        var airContext2 = new MovementContext
        {
            IsOnFloor = false,
            JumpRequested = true,
            JumpForce = JumpForce,
            CurrentVelocity = firstJumpResult
        };
        var secondJumpResult = _modifier.ModifyVelocity(firstJumpResult, airContext2);

        // Assert - Second jump should also apply force
        Assert.Equal(JumpForce, secondJumpResult.Y);
    }

    /// <summary>
    /// Test 1.2.3: Sayaç doğru çalışıyor mu? (Counter works correctly)
    /// Verifies that jump counter increments correctly and respects max jumps limit.
    /// </summary>
    [Fact]
    public void ModifyVelocity_CounterRespectMaxJumps_NoThirdJump()
    {
        // Arrange
        var velocity = new Vector3(0, 0, 0);
        
        // Reset counter by being on floor
        var floorContext = new MovementContext
        {
            IsOnFloor = true,
            JumpRequested = false,
            JumpForce = JumpForce,
            CurrentVelocity = velocity
        };
        _modifier.ModifyVelocity(velocity, floorContext);

        // Act - First jump
        var airContext1 = new MovementContext
        {
            IsOnFloor = false,
            JumpRequested = true,
            JumpForce = JumpForce,
            CurrentVelocity = velocity
        };
        var firstJumpResult = _modifier.ModifyVelocity(velocity, airContext1);

        // Act - Second jump
        var airContext2 = new MovementContext
        {
            IsOnFloor = false,
            JumpRequested = true,
            JumpForce = JumpForce,
            CurrentVelocity = firstJumpResult
        };
        var secondJumpResult = _modifier.ModifyVelocity(firstJumpResult, airContext2);

        // Act - Attempt third jump (should fail)
        // Use a velocity with different Y to verify it's not changed
        var velocityWithDifferentY = new Vector3(0, 2.5f, 0);
        var airContext3 = new MovementContext
        {
            IsOnFloor = false,
            JumpRequested = true,
            JumpForce = JumpForce,
            CurrentVelocity = velocityWithDifferentY
        };
        var thirdJumpResult = _modifier.ModifyVelocity(velocityWithDifferentY, airContext3);

        // Assert - Third jump should NOT apply force (velocity unchanged)
        Assert.Equal(velocityWithDifferentY, thirdJumpResult);
        Assert.Equal(2.5f, thirdJumpResult.Y);
    }
}
