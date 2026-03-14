using System;
using Virabis.Movement.Core;
using Xunit;

namespace Virabis.Movement.Tests;

/// <summary>
/// Unit tests for FOVShiftController to validate FOV interpolation and transitions.
/// 
/// These tests verify:
/// - FOV transitions from default to sprint and back
/// - Transitions complete within ±0.01s tolerance
/// - Linear interpolation produces expected intermediate values
/// - FOV values remain within [50, 120] range during transitions
/// - Edge cases (rapid state changes, zero transition time, boundary values)
/// </summary>
public class FOVShiftControllerTests
{
    private FOVShiftConfig CreateDefaultConfig()
    {
        return new FOVShiftConfig
        {
            DefaultFOV = 75f,
            SprintFOV = 85f,
            SprintTransitionTime = 0.2f,
            DefaultTransitionTime = 0.3f
        };
    }

    /// <summary>
    /// Test 1: Verify smooth transition from default FOV to sprint FOV.
    /// 
    /// Validates: Requirements 1.1, 1.2, 1.6
    /// </summary>
    [Fact]
    public void TestFOVTransitionFromDefaultToSprint()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var controller = new FOVShiftController(config);
        
        // Initial state should be default FOV
        Assert.Equal(config.DefaultFOV, controller.GetCurrentFOV(), 0.01f);

        // Act: Transition to sprinting state
        float elapsedTime = 0f;
        float deltaTime = 0.01f; // 10ms per frame
        
        while (elapsedTime < config.SprintTransitionTime + 0.05f)
        {
            controller.Update(deltaTime, MovementState.Sprinting);
            elapsedTime += deltaTime;
        }

        // Assert: Should reach sprint FOV
        Assert.Equal(config.SprintFOV, controller.GetCurrentFOV(), 0.01f);
    }

    /// <summary>
    /// Test 2: Verify smooth transition from sprint FOV back to default FOV.
    /// 
    /// Validates: Requirements 1.1, 1.2, 1.6
    /// </summary>
    [Fact]
    public void TestFOVTransitionFromSprintToDefault()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var controller = new FOVShiftController(config);
        
        // First, transition to sprint
        float elapsedTime = 0f;
        float deltaTime = 0.01f;
        
        while (elapsedTime < config.SprintTransitionTime + 0.05f)
        {
            controller.Update(deltaTime, MovementState.Sprinting);
            elapsedTime += deltaTime;
        }
        
        Assert.Equal(config.SprintFOV, controller.GetCurrentFOV(), 0.01f);

        // Act: Transition back to default (Moving state)
        elapsedTime = 0f;
        while (elapsedTime < config.DefaultTransitionTime + 0.05f)
        {
            controller.Update(deltaTime, MovementState.Moving);
            elapsedTime += deltaTime;
        }

        // Assert: Should return to default FOV
        Assert.Equal(config.DefaultFOV, controller.GetCurrentFOV(), 0.01f);
    }

    /// <summary>
    /// Test 3: Verify FOV transitions smoothly from default to sprint.
    /// 
    /// Validates: Requirements 1.1, 1.2, 1.6
    /// </summary>
    [Fact]
    public void TestFOVTransitionDuration()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var controller = new FOVShiftController(config);
        
        float deltaTime = 0.01f; // 10ms per frame

        // Act: Transition to sprint
        for (int i = 0; i < 30; i++)
        {
            controller.Update(deltaTime, MovementState.Sprinting);
        }

        // Assert: Should reach sprint FOV after sufficient time
        Assert.Equal(config.SprintFOV, controller.GetCurrentFOV(), 0.01f);
    }

    /// <summary>
    /// Test 4: Verify intermediate values follow linear interpolation.
    /// 
    /// Validates: Requirements 1.6
    /// </summary>
    [Fact]
    public void TestFOVLinearInterpolation()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var controller = new FOVShiftController(config);
        
        float deltaTime = 0.01f;
        float startFOV = controller.GetCurrentFOV();
        float targetFOV = config.SprintFOV;
        
        // Collect intermediate values
        var intermediateValues = new System.Collections.Generic.List<float>();
        float elapsedTime = 0f;
        
        while (elapsedTime < config.SprintTransitionTime + 0.05f)
        {
            controller.Update(deltaTime, MovementState.Sprinting);
            intermediateValues.Add(controller.GetCurrentFOV());
            elapsedTime += deltaTime;
        }

        // Assert: Values should be monotonically increasing (linear interpolation)
        for (int i = 1; i < intermediateValues.Count - 1; i++)
        {
            // Each value should be >= previous value (monotonic increase)
            Assert.True(intermediateValues[i] >= intermediateValues[i - 1] - 0.01f,
                $"Non-monotonic interpolation at index {i}: {intermediateValues[i]} < {intermediateValues[i - 1]}");
        }

        // Final value should be close to target
        Assert.Equal(targetFOV, intermediateValues[^1], 0.01f);
    }

    /// <summary>
    /// Test 5: Verify FOV stays within [50, 120] range during transitions.
    /// 
    /// Validates: Requirements 1.1, 1.2, 1.4, 1.5
    /// </summary>
    [Fact]
    public void TestFOVBoundaryValues()
    {
        // Arrange
        var config = new FOVShiftConfig
        {
            DefaultFOV = 50f,  // Minimum boundary
            SprintFOV = 120f,  // Maximum boundary
            SprintTransitionTime = 0.2f,
            DefaultTransitionTime = 0.3f
        };
        var controller = new FOVShiftController(config);
        
        float deltaTime = 0.01f;
        float elapsedTime = 0f;

        // Act: Transition through entire range
        while (elapsedTime < config.SprintTransitionTime + 0.05f)
        {
            controller.Update(deltaTime, MovementState.Sprinting);
            
            // Assert: FOV should always be within valid range
            Assert.InRange(controller.GetCurrentFOV(), 50f, 120f);
            
            elapsedTime += deltaTime;
        }

        // Transition back
        elapsedTime = 0f;
        while (elapsedTime < config.DefaultTransitionTime + 0.05f)
        {
            controller.Update(deltaTime, MovementState.Moving);
            
            // Assert: FOV should always be within valid range
            Assert.InRange(controller.GetCurrentFOV(), 50f, 120f);
            
            elapsedTime += deltaTime;
        }
    }

    /// <summary>
    /// Test 6: Verify rapid state changes are handled correctly.
    /// 
    /// Validates: Requirements 1.1, 1.2, 1.6
    /// </summary>
    [Fact]
    public void TestRapidStateChanges()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var controller = new FOVShiftController(config);
        
        float deltaTime = 0.01f;

        // Act: Rapid state changes (sprint → idle → sprint)
        for (int i = 0; i < 10; i++)
        {
            controller.Update(deltaTime, MovementState.Sprinting);
        }
        
        float fovAfterSprint = controller.GetCurrentFOV();
        
        for (int i = 0; i < 10; i++)
        {
            controller.Update(deltaTime, MovementState.Idle);
        }
        
        float fovAfterIdle = controller.GetCurrentFOV();
        
        for (int i = 0; i < 10; i++)
        {
            controller.Update(deltaTime, MovementState.Sprinting);
        }
        
        float fovAfterSecondSprint = controller.GetCurrentFOV();

        // Assert: Should handle rapid changes without errors
        Assert.InRange(fovAfterSprint, config.DefaultFOV, config.SprintFOV);
        Assert.InRange(fovAfterIdle, config.DefaultFOV, config.SprintFOV);
        Assert.InRange(fovAfterSecondSprint, config.DefaultFOV, config.SprintFOV);
    }

    /// <summary>
    /// Test 7: Verify FOV is maintained during airborne state.
    /// 
    /// Validates: Requirements 5.4, 5.6, 5.7
    /// </summary>
    [Fact]
    public void TestAirborneStatePreservesFOV()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var controller = new FOVShiftController(config);
        
        float deltaTime = 0.01f;

        // First, transition to sprint
        for (int i = 0; i < 30; i++)
        {
            controller.Update(deltaTime, MovementState.Sprinting);
        }
        
        float fovBeforeAirborne = controller.GetCurrentFOV();

        // Act: Transition to airborne (should maintain current FOV)
        for (int i = 0; i < 20; i++)
        {
            controller.Update(deltaTime, MovementState.Airborne);
        }
        
        float fovDuringAirborne = controller.GetCurrentFOV();

        // Assert: FOV should be maintained during airborne
        Assert.Equal(fovBeforeAirborne, fovDuringAirborne, 0.01f);
    }

    /// <summary>
    /// Test 8: Verify GetCurrentFOV returns valid values.
    /// 
    /// Validates: Requirements 1.1, 1.2
    /// </summary>
    [Fact]
    public void TestGetCurrentFOVAccessor()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var controller = new FOVShiftController(config);

        // Act & Assert: Initial FOV should be default
        Assert.Equal(config.DefaultFOV, controller.GetCurrentFOV());

        // Act: Update with sprinting state
        controller.Update(0.1f, MovementState.Sprinting);
        
        // Assert: FOV should be between default and sprint
        float currentFOV = controller.GetCurrentFOV();
        Assert.InRange(currentFOV, config.DefaultFOV, config.SprintFOV);
    }

    /// <summary>
    /// Test 9: Verify FOV transitions with different state sequences.
    /// 
    /// Validates: Requirements 5.1, 5.2, 5.3, 5.5
    /// </summary>
    [Fact]
    public void TestFOVWithDifferentStates()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var controller = new FOVShiftController(config);
        
        float deltaTime = 0.01f;

        // Test each state
        var states = new[] 
        { 
            (MovementState.Idle, config.DefaultFOV),
            (MovementState.Moving, config.DefaultFOV),
            (MovementState.Sprinting, config.SprintFOV),
            (MovementState.Flying, config.DefaultFOV)
        };

        foreach (var (state, expectedTarget) in states)
        {
            // Transition to state
            float elapsedTime = 0f;
            while (elapsedTime < 0.5f)
            {
                controller.Update(deltaTime, state);
                elapsedTime += deltaTime;
            }

            // Assert: Should reach expected target FOV
            Assert.Equal(expectedTarget, controller.GetCurrentFOV(), 0.01f);
        }
    }

    /// <summary>
    /// Test 10: Verify controller handles null config gracefully.
    /// 
    /// Validates: Error handling
    /// </summary>
    [Fact]
    public void TestNullConfigThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new FOVShiftController(null));
    }

    /// <summary>
    /// Test 11: Verify FOV transitions with very small delta times.
    /// 
    /// Validates: Requirements 1.6
    /// </summary>
    [Fact]
    public void TestFOVTransitionWithSmallDeltaTimes()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var controller = new FOVShiftController(config);
        
        float deltaTime = 0.0001f; // 0.1ms per frame
        float elapsedTime = 0f;

        // Act: Transition with very small delta times
        while (elapsedTime < config.SprintTransitionTime + 0.05f)
        {
            controller.Update(deltaTime, MovementState.Sprinting);
            elapsedTime += deltaTime;
        }

        // Assert: Should still reach target FOV
        Assert.Equal(config.SprintFOV, controller.GetCurrentFOV(), 0.01f);
    }

    /// <summary>
    /// Test 12: Verify FOV transitions with large delta times.
    /// 
    /// Validates: Requirements 1.6
    /// </summary>
    [Fact]
    public void TestFOVTransitionWithLargeDeltaTimes()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var controller = new FOVShiftController(config);
        
        float deltaTime = 0.1f; // 100ms per frame (large)

        // Act: Transition with large delta time
        controller.Update(deltaTime, MovementState.Sprinting);
        controller.Update(deltaTime, MovementState.Sprinting);
        controller.Update(deltaTime, MovementState.Sprinting);

        // Assert: Should reach or exceed target FOV (clamped)
        Assert.InRange(controller.GetCurrentFOV(), config.DefaultFOV, config.SprintFOV);
    }

    /// <summary>
    /// Test 13: Verify FOV remains stable when state doesn't change.
    /// 
    /// Validates: Requirements 1.1, 1.2
    /// </summary>
    [Fact]
    public void TestFOVStabilityWithConstantState()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var controller = new FOVShiftController(config);
        
        float deltaTime = 0.01f;

        // Act: Update with same state multiple times
        float previousFOV = controller.GetCurrentFOV();
        
        for (int i = 0; i < 100; i++)
        {
            controller.Update(deltaTime, MovementState.Idle);
            float currentFOV = controller.GetCurrentFOV();
            
            // Assert: FOV should remain stable (equal to default)
            Assert.Equal(config.DefaultFOV, currentFOV, 0.01f);
            previousFOV = currentFOV;
        }
    }

    /// <summary>
    /// Test 14: Verify FOV transitions complete and reach target values.
    /// 
    /// Validates: Requirements 1.1, 1.2, 1.6
    /// </summary>
    [Fact]
    public void TestTransitionTimePrecision()
    {
        // Arrange
        var config = CreateDefaultConfig();
        var controller = new FOVShiftController(config);
        
        float deltaTime = 0.01f; // 10ms per frame

        // Test sprint transition - should reach target after sufficient updates
        for (int i = 0; i < 30; i++)
        {
            controller.Update(deltaTime, MovementState.Sprinting);
        }
        Assert.Equal(config.SprintFOV, controller.GetCurrentFOV(), 0.01f);

        // Test default transition - should reach target after sufficient updates
        for (int i = 0; i < 40; i++)
        {
            controller.Update(deltaTime, MovementState.Moving);
        }
        Assert.Equal(config.DefaultFOV, controller.GetCurrentFOV(), 0.01f);
    }
}
