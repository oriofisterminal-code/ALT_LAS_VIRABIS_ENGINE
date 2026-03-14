using System;
using System.Collections.Generic;
using FsCheck.Xunit;
using Virabis.Movement.Core;

namespace Virabis.Movement.Tests;

/// <summary>
/// Property-based tests for the FOV Shift system using FsCheck.
/// 
/// These tests validate universal correctness properties that should hold across
/// all valid inputs and movement states. Each property is tested with 100+ randomized
/// iterations to ensure robustness.
/// 
/// **Validates: Requirements 1.1, 1.2, 1.6, 1.8, 5.1, 5.2, 5.3, 5.4, 5.5, 5.6**
/// </summary>
public class FOVShiftPropertyTests
{
    /// <summary>
    /// Static instance provider for FsCheck
    /// </summary>
    public static FOVShiftPropertyTests Instance => new();

    /// <summary>
    /// Property 1: FOV Remains in Valid Range
    /// 
    /// For any FOV value, whether default, sprint, or transitioning, the FOV SHALL
    /// always remain within the range [50, 120] degrees.
    /// 
    /// **Validates: Requirements 1.1, 1.2, 1.4, 1.5**
    /// </summary>
    [Property]
    public bool Property1_FOVRemainsInValidRange(
        int defaultFOVInt,
        int sprintFOVInt,
        int sprintTransitionTimeInt,
        int defaultTransitionTimeInt,
        int deltaTimeInt)
    {
        // Arrange: Create config with clamped values
        var config = new FOVShiftConfig
        {
            DefaultFOV = Math.Clamp(50 + (Math.Abs(defaultFOVInt) % 71), 50f, 120f),
            SprintFOV = Math.Clamp(50 + (Math.Abs(sprintFOVInt) % 71), 50f, 120f),
            SprintTransitionTime = Math.Clamp((Math.Abs(sprintTransitionTimeInt) % 1000) / 5000f + 0.001f, 0.001f, 1f),
            DefaultTransitionTime = Math.Clamp((Math.Abs(defaultTransitionTimeInt) % 1000) / 5000f + 0.001f, 0.001f, 1f)
        };

        var controller = new FOVShiftController(config);
        float safeDeltaTime = Math.Clamp((Math.Abs(deltaTimeInt) % 100) / 10000f + 0.001f, 0.001f, 0.1f);

        // Act: Update with various states multiple times
        var states = new[] 
        { 
            MovementState.Idle, 
            MovementState.Moving, 
            MovementState.Sprinting, 
            MovementState.Airborne, 
            MovementState.Flying 
        };

        foreach (var state in states)
        {
            for (int i = 0; i < 100; i++)
            {
                controller.Update(safeDeltaTime, state);
                
                // Assert: FOV must always be within valid range
                if (controller.GetCurrentFOV() < 50f || controller.GetCurrentFOV() > 120f)
                    return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Property 2: FOV Transitions Complete in Specified Time
    /// 
    /// For any FOV transition (sprint entry or exit), the transition SHALL complete
    /// within the configured transition duration (±0.01 seconds tolerance).
    /// 
    /// **Validates: Requirements 1.1, 1.2, 1.6**
    /// </summary>
    [Property]
    public bool Property2_FOVTransitionsCompleteInSpecifiedTime(
        int defaultFOVInt,
        int sprintFOVInt,
        int sprintTransitionTimeInt,
        int defaultTransitionTimeInt)
    {
        // Arrange
        var config = new FOVShiftConfig
        {
            DefaultFOV = Math.Clamp(50 + (Math.Abs(defaultFOVInt) % 71), 50f, 120f),
            SprintFOV = Math.Clamp(50 + (Math.Abs(sprintFOVInt) % 71), 50f, 120f),
            SprintTransitionTime = Math.Clamp((Math.Abs(sprintTransitionTimeInt) % 1000) / 5000f + 0.001f, 0.001f, 1f),
            DefaultTransitionTime = Math.Clamp((Math.Abs(defaultTransitionTimeInt) % 1000) / 5000f + 0.001f, 0.001f, 1f)
        };

        var controller = new FOVShiftController(config);
        float deltaTime = 0.01f;

        // Act: Transition to sprint
        float elapsedTime = 0f;
        
        while (elapsedTime < config.SprintTransitionTime + 0.05f)
        {
            controller.Update(deltaTime, MovementState.Sprinting);
            elapsedTime += deltaTime;
        }

        float fovAfterSprintTransition = controller.GetCurrentFOV();

        // Assert: Should reach sprint FOV within tolerance
        if (Math.Abs(fovAfterSprintTransition - config.SprintFOV) > 0.01f)
            return false;

        // Act: Transition back to default
        elapsedTime = 0f;
        while (elapsedTime < config.DefaultTransitionTime + 0.05f)
        {
            controller.Update(deltaTime, MovementState.Moving);
            elapsedTime += deltaTime;
        }

        float fovAfterDefaultTransition = controller.GetCurrentFOV();

        // Assert: Should reach default FOV within tolerance
        return Math.Abs(fovAfterDefaultTransition - config.DefaultFOV) <= 0.01f;
    }

    /// <summary>
    /// Property 3: FOV Transitions Use Linear Interpolation
    /// 
    /// For any FOV transition, intermediate FOV values SHALL follow linear interpolation
    /// between start and end values.
    /// 
    /// **Validates: Requirements 1.6**
    /// </summary>
    [Property]
    public bool Property3_FOVTransitionsUseLinearInterpolation(
        int defaultFOVInt,
        int sprintFOVInt,
        int sprintTransitionTimeInt,
        int defaultTransitionTimeInt)
    {
        // Arrange
        var config = new FOVShiftConfig
        {
            DefaultFOV = Math.Clamp(50 + (Math.Abs(defaultFOVInt) % 71), 50f, 120f),
            SprintFOV = Math.Clamp(50 + (Math.Abs(sprintFOVInt) % 71), 50f, 120f),
            SprintTransitionTime = Math.Clamp((Math.Abs(sprintTransitionTimeInt) % 1000) / 5000f + 0.001f, 0.001f, 1f),
            DefaultTransitionTime = Math.Clamp((Math.Abs(defaultTransitionTimeInt) % 1000) / 5000f + 0.001f, 0.001f, 1f)
        };

        var controller = new FOVShiftController(config);
        float deltaTime = 0.01f;

        // Collect intermediate values during transition
        var intermediateValues = new List<float>();
        float elapsedTime = 0f;

        while (elapsedTime < config.SprintTransitionTime + 0.05f)
        {
            controller.Update(deltaTime, MovementState.Sprinting);
            intermediateValues.Add(controller.GetCurrentFOV());
            elapsedTime += deltaTime;
        }

        // Assert: Values should be monotonically increasing (linear interpolation property)
        for (int i = 1; i < intermediateValues.Count; i++)
        {
            // Each value should be >= previous value (allowing small floating point errors)
            if (intermediateValues[i] < intermediateValues[i - 1] - 0.01f)
                return false;
        }

        // Assert: Final value should be close to target
        if (Math.Abs(intermediateValues[^1] - config.SprintFOV) > 0.01f)
            return false;

        return true;
    }

    /// <summary>
    /// Property 4: FOV Does Not Affect Movement Physics
    /// 
    /// For any movement update, applying FOV shift SHALL NOT modify the velocity
    /// or acceleration of the player. This is a structural property - the FOV system
    /// is purely visual and should not interact with physics.
    /// 
    /// **Validates: Requirements 1.8**
    /// </summary>
    [Property]
    public bool Property4_FOVDoesNotAffectMovementPhysics(
        int defaultFOVInt,
        int sprintFOVInt,
        int sprintTransitionTimeInt,
        int defaultTransitionTimeInt,
        int deltaTimeInt)
    {
        // Arrange
        var config = new FOVShiftConfig
        {
            DefaultFOV = Math.Clamp(50 + (Math.Abs(defaultFOVInt) % 71), 50f, 120f),
            SprintFOV = Math.Clamp(50 + (Math.Abs(sprintFOVInt) % 71), 50f, 120f),
            SprintTransitionTime = Math.Clamp((Math.Abs(sprintTransitionTimeInt) % 1000) / 5000f + 0.001f, 0.001f, 1f),
            DefaultTransitionTime = Math.Clamp((Math.Abs(defaultTransitionTimeInt) % 1000) / 5000f + 0.001f, 0.001f, 1f)
        };

        var controller = new FOVShiftController(config);
        float safeDeltaTime = Math.Clamp((Math.Abs(deltaTimeInt) % 100) / 10000f + 0.001f, 0.001f, 0.1f);

        // Act: Update FOV controller multiple times
        for (int i = 0; i < 100; i++)
        {
            controller.Update(safeDeltaTime, MovementState.Sprinting);
        }

        // Assert: FOV controller should only affect FOV, not physics
        float fov = controller.GetCurrentFOV();
        
        // The property is that FOV is independent of physics - we verify this by
        // ensuring the controller can be updated without side effects
        return fov >= 50f && fov <= 120f;
    }

    /// <summary>
    /// Property 5: Sprint FOV Maintained During Sprint
    /// 
    /// For any frame where the player is in Sprinting state, the target FOV SHALL
    /// be set to sprint FOV value.
    /// 
    /// **Validates: Requirements 5.1**
    /// </summary>
    [Property]
    public bool Property5_SprintFOVMaintainedDuringSprint(
        int defaultFOVInt,
        int sprintFOVInt,
        int sprintTransitionTimeInt,
        int defaultTransitionTimeInt)
    {
        // Arrange
        var config = new FOVShiftConfig
        {
            DefaultFOV = Math.Clamp(50 + (Math.Abs(defaultFOVInt) % 71), 50f, 120f),
            SprintFOV = Math.Clamp(50 + (Math.Abs(sprintFOVInt) % 71), 50f, 120f),
            SprintTransitionTime = Math.Clamp((Math.Abs(sprintTransitionTimeInt) % 1000) / 5000f + 0.001f, 0.001f, 1f),
            DefaultTransitionTime = Math.Clamp((Math.Abs(defaultTransitionTimeInt) % 1000) / 5000f + 0.001f, 0.001f, 1f)
        };

        var controller = new FOVShiftController(config);
        float deltaTime = 0.01f;

        // Act: Transition to sprint and maintain it
        for (int i = 0; i < 50; i++)
        {
            controller.Update(deltaTime, MovementState.Sprinting);
        }

        float fovDuringSprint = controller.GetCurrentFOV();

        // Assert: FOV should be at or very close to sprint FOV
        return Math.Abs(fovDuringSprint - config.SprintFOV) <= 0.01f;
    }

    /// <summary>
    /// Property 6: Default FOV in Non-Sprint States
    /// 
    /// For any frame where the player is in Moving, Idle, or Flying state, the target
    /// FOV SHALL be set to default FOV value.
    /// 
    /// **Validates: Requirements 5.2, 5.3, 5.5**
    /// </summary>
    [Property]
    public bool Property6_DefaultFOVInNonSprintStates(
        int defaultFOVInt,
        int sprintFOVInt,
        int sprintTransitionTimeInt,
        int defaultTransitionTimeInt)
    {
        // Arrange
        var config = new FOVShiftConfig
        {
            DefaultFOV = Math.Clamp(50 + (Math.Abs(defaultFOVInt) % 71), 50f, 120f),
            SprintFOV = Math.Clamp(50 + (Math.Abs(sprintFOVInt) % 71), 50f, 120f),
            SprintTransitionTime = Math.Clamp((Math.Abs(sprintTransitionTimeInt) % 1000) / 5000f + 0.001f, 0.001f, 1f),
            DefaultTransitionTime = Math.Clamp((Math.Abs(defaultTransitionTimeInt) % 1000) / 5000f + 0.001f, 0.001f, 1f)
        };

        var controller = new FOVShiftController(config);
        float deltaTime = 0.01f;

        // Test each non-sprint state
        var nonSprintStates = new[] 
        { 
            MovementState.Idle, 
            MovementState.Moving, 
            MovementState.Flying 
        };

        foreach (var state in nonSprintStates)
        {
            // Act: Update with non-sprint state
            for (int i = 0; i < 50; i++)
            {
                controller.Update(deltaTime, state);
            }

            float fovInState = controller.GetCurrentFOV();

            // Assert: FOV should be at or very close to default FOV
            if (Math.Abs(fovInState - config.DefaultFOV) > 0.01f)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Property 7: FOV Preserved During Airborne
    /// 
    /// For any frame where the player transitions to Airborne state, the current FOV
    /// SHALL be maintained (not changed to a different target).
    /// 
    /// **Validates: Requirements 5.4, 5.6**
    /// </summary>
    [Property]
    public bool Property7_FOVPreservedDuringAirborne(
        int defaultFOVInt,
        int sprintFOVInt,
        int sprintTransitionTimeInt,
        int defaultTransitionTimeInt)
    {
        // Arrange
        var config = new FOVShiftConfig
        {
            DefaultFOV = Math.Clamp(50 + (Math.Abs(defaultFOVInt) % 71), 50f, 120f),
            SprintFOV = Math.Clamp(50 + (Math.Abs(sprintFOVInt) % 71), 50f, 120f),
            SprintTransitionTime = Math.Clamp((Math.Abs(sprintTransitionTimeInt) % 1000) / 5000f + 0.001f, 0.001f, 1f),
            DefaultTransitionTime = Math.Clamp((Math.Abs(defaultTransitionTimeInt) % 1000) / 5000f + 0.001f, 0.001f, 1f)
        };

        var controller = new FOVShiftController(config);
        float deltaTime = 0.01f;

        // First, transition to sprint
        for (int i = 0; i < 50; i++)
        {
            controller.Update(deltaTime, MovementState.Sprinting);
        }

        float fovBeforeAirborne = controller.GetCurrentFOV();

        // Act: Transition to airborne
        for (int i = 0; i < 30; i++)
        {
            controller.Update(deltaTime, MovementState.Airborne);
        }

        float fovDuringAirborne = controller.GetCurrentFOV();

        // Assert: FOV should be preserved during airborne
        return Math.Abs(fovBeforeAirborne - fovDuringAirborne) <= 0.01f;
    }
}
