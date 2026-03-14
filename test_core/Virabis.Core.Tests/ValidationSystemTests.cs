using FsCheck;
using FsCheck.Xunit;

namespace Virabis.Core.Tests;

/// <summary>
/// Property-based tests for the validation system.
/// These tests validate the automated validation execution and logging behavior.
/// </summary>
public class ValidationSystemTests
{
    /// <summary>
    /// Represents a validation event that can be logged.
    /// </summary>
    public record ValidationEvent(string TestName, bool Passed, Dictionary<string, object> Details);

    /// <summary>
    /// Represents a validation system that tracks validation events.
    /// This simulates the GDScript validation_system.gd behavior.
    /// </summary>
    public class ValidationSystemSimulator
    {
        private readonly List<string> _logEntries = new();
        private readonly List<ValidationEvent> _validationEvents = new();

        public IReadOnlyList<string> LogEntries => _logEntries.AsReadOnly();
        public IReadOnlyList<ValidationEvent> ValidationEvents => _validationEvents.AsReadOnly();

        /// <summary>
        /// Validates a test with measurable success criteria.
        /// This simulates the validate_test_with_criteria method from validation_system.gd.
        /// </summary>
        public bool ValidateTestWithCriteria(string testName, bool condition, Dictionary<string, object>? successDetails = null, string failureReason = "")
        {
            successDetails ??= new Dictionary<string, object>();

            if (condition)
            {
                // Log success
                _logEntries.Add($"[ValidationSystem] ✓ {testName} passed");
                
                // Emit validation_passed signal (simulated as event storage)
                _validationEvents.Add(new ValidationEvent(testName, true, successDetails));
                
                // Log validation result
                LogValidationResult(testName, true);
            }
            else
            {
                // Log failure
                _logEntries.Add($"[ValidationSystem] ✗ {testName} failed: {failureReason}");
                
                // Emit validation_failed signal (simulated as event storage)
                _validationEvents.Add(new ValidationEvent(testName, false, new Dictionary<string, object> { { "reason", failureReason } }));
                
                // Log validation result
                LogValidationResult(testName, false, failureReason);
            }

            return condition;
        }

        /// <summary>
        /// Logs a validation result to the console.
        /// This simulates the log_validation_result method from validation_system.gd.
        /// </summary>
        private void LogValidationResult(string testName, bool success, string details = "")
        {
            var status = success ? "PASSED" : "FAILED";
            var message = $"[ValidationSystem] [{status}] {testName}";
            if (!string.IsNullOrEmpty(details))
            {
                message += $": {details}";
            }

            _logEntries.Add(message);
        }

        /// <summary>
        /// Validates grapple attachment.
        /// </summary>
        public bool ValidateGrappleAttachment(float distance, float threshold = 1.0f)
        {
            var success = distance < threshold;
            var details = new Dictionary<string, object>
            {
                { "distance", distance },
                { "threshold", threshold }
            };

            return ValidateTestWithCriteria(
                "grapple_attachment",
                success,
                details,
                $"Player too far from anchor ({distance:F2}m > {threshold:F2}m)"
            );
        }

        /// <summary>
        /// Validates slide speed.
        /// </summary>
        public bool ValidateSlideSpeed(float speed, float minSpeed = 6.0f)
        {
            var success = speed >= minSpeed;
            var details = new Dictionary<string, object>
            {
                { "speed", speed },
                { "min_speed", minSpeed }
            };

            return ValidateTestWithCriteria(
                "slide_speed",
                success,
                details,
                $"Slide speed too low ({speed:F2} m/s < {minSpeed:F2} m/s)"
            );
        }

        /// <summary>
        /// Validates wall jump chain.
        /// </summary>
        public bool ValidateWallJumpChain(int jumpCount, int target = 3)
        {
            var success = jumpCount >= target;
            var details = new Dictionary<string, object>
            {
                { "jump_count", jumpCount },
                { "target", target }
            };

            return ValidateTestWithCriteria(
                "wall_jump_chain",
                success,
                details,
                $"Insufficient wall jumps ({jumpCount} < {target})"
            );
        }

        /// <summary>
        /// Validates damage calculation.
        /// </summary>
        public bool ValidateDamageCalculation(float expected, float actual, float tolerance = 0.01f)
        {
            var difference = Math.Abs(expected - actual);
            var success = difference <= tolerance;
            var details = new Dictionary<string, object>
            {
                { "expected", expected },
                { "actual", actual },
                { "difference", difference }
            };

            return ValidateTestWithCriteria(
                "damage_calculation",
                success,
                details,
                $"Damage mismatch (expected: {expected:F2}, actual: {actual:F2}, diff: {difference:F2})"
            );
        }

        /// <summary>
        /// Validates friendly fire prevention.
        /// </summary>
        public bool ValidateFriendlyFire(float damage)
        {
            var success = damage == 0.0f;
            var details = new Dictionary<string, object>
            {
                { "damage", damage }
            };

            return ValidateTestWithCriteria(
                "friendly_fire",
                success,
                details,
                $"Friendly fire dealt damage ({damage:F2} > 0)"
            );
        }
    }

    /// <summary>
    /// **Validates: Requirements 13.1**
    /// 
    /// Property 47: Automated Validation Execution
    /// 
    /// For any test with measurable success criteria, the validation system should automatically
    /// check the conditions and emit a validation result.
    /// 
    /// This property ensures that:
    /// 1. When a test condition is true, a validation_passed event is emitted
    /// 2. When a test condition is false, a validation_failed event is emitted
    /// 3. The validation result matches the condition being tested
    /// </summary>
    [Property]
    public Property AutomatedValidationExecution_ShouldEmitValidationResult(string testName, bool condition)
    {
        // Filter out invalid test names
        if (string.IsNullOrWhiteSpace(testName))
        {
            return true.ToProperty().Label("Skipped: Invalid test name");
        }

        // Arrange: Create a validation system
        var validationSystem = new ValidationSystemSimulator();

        // Act: Validate a test with the given condition
        var result = validationSystem.ValidateTestWithCriteria(
            testName,
            condition,
            new Dictionary<string, object> { { "test_data", "sample" } },
            "Test condition was false"
        );

        // Assert: The result should match the condition
        var resultMatches = result == condition;

        // Assert: A validation event should be emitted
        var eventEmitted = validationSystem.ValidationEvents.Count == 1;

        // Assert: The emitted event should have the correct test name and pass/fail status
        var eventCorrect = validationSystem.ValidationEvents.Count > 0 &&
                          validationSystem.ValidationEvents[0].TestName == testName &&
                          validationSystem.ValidationEvents[0].Passed == condition;

        return (resultMatches && eventEmitted && eventCorrect)
            .Label($"Test: {testName}, Condition: {condition}, Result: {result}, Events: {validationSystem.ValidationEvents.Count}")
            .Classify(condition, "passed")
            .Classify(!condition, "failed");
    }

    /// <summary>
    /// **Validates: Requirements 13.7**
    /// 
    /// Property 52: Validation Logging
    /// 
    /// For any validation event (passed or failed), the validation system should write a log entry
    /// to the console with test name and result.
    /// 
    /// This property ensures that:
    /// 1. Every validation event generates at least one log entry
    /// 2. The log entry contains the test name
    /// 3. The log entry indicates whether the test passed or failed
    /// </summary>
    [Property]
    public Property ValidationLogging_ShouldLogAllValidationEvents(string testName, bool condition)
    {
        // Filter out invalid test names
        if (string.IsNullOrWhiteSpace(testName))
        {
            return true.ToProperty().Label("Skipped: Invalid test name");
        }

        // Arrange: Create a validation system
        var validationSystem = new ValidationSystemSimulator();

        // Act: Validate a test
        validationSystem.ValidateTestWithCriteria(
            testName,
            condition,
            new Dictionary<string, object>(),
            "Test failed"
        );

        // Assert: At least one log entry should be created
        var hasLogEntries = validationSystem.LogEntries.Count > 0;

        // Assert: The log entries should contain the test name
        var containsTestName = validationSystem.LogEntries.Any(log => log.Contains(testName));

        // Assert: The log entries should indicate pass/fail status
        var containsStatus = validationSystem.LogEntries.Any(log =>
            log.Contains("PASSED") || log.Contains("FAILED") || log.Contains("✓") || log.Contains("✗")
        );

        // Assert: The status should match the condition
        var statusMatches = condition
            ? validationSystem.LogEntries.Any(log => log.Contains("PASSED") || log.Contains("✓"))
            : validationSystem.LogEntries.Any(log => log.Contains("FAILED") || log.Contains("✗"));

        return (hasLogEntries && containsTestName && containsStatus && statusMatches)
            .Label($"Test: {testName}, Condition: {condition}, Logs: {validationSystem.LogEntries.Count}")
            .Classify(condition, "passed_logged")
            .Classify(!condition, "failed_logged");
    }

    /// <summary>
    /// Unit test: Validates that grapple attachment validation works correctly.
    /// </summary>
    [Fact]
    public void GrappleAttachment_WithinThreshold_ShouldPass()
    {
        // Arrange
        var validationSystem = new ValidationSystemSimulator();

        // Act: Validate grapple with distance within threshold
        var result = validationSystem.ValidateGrappleAttachment(distance: 0.5f, threshold: 1.0f);

        // Assert
        Assert.True(result);
        Assert.Single(validationSystem.ValidationEvents);
        Assert.True(validationSystem.ValidationEvents[0].Passed);
        Assert.Contains("grapple_attachment", validationSystem.LogEntries[0]);
    }

    /// <summary>
    /// Unit test: Validates that grapple attachment validation fails when too far.
    /// </summary>
    [Fact]
    public void GrappleAttachment_BeyondThreshold_ShouldFail()
    {
        // Arrange
        var validationSystem = new ValidationSystemSimulator();

        // Act: Validate grapple with distance beyond threshold
        var result = validationSystem.ValidateGrappleAttachment(distance: 1.5f, threshold: 1.0f);

        // Assert
        Assert.False(result);
        Assert.Single(validationSystem.ValidationEvents);
        Assert.False(validationSystem.ValidationEvents[0].Passed);
        Assert.Contains("grapple_attachment", validationSystem.LogEntries[0]);
    }

    /// <summary>
    /// Unit test: Validates that slide speed validation works correctly.
    /// </summary>
    [Fact]
    public void SlideSpeed_AboveMinimum_ShouldPass()
    {
        // Arrange
        var validationSystem = new ValidationSystemSimulator();

        // Act: Validate slide with speed above minimum
        var result = validationSystem.ValidateSlideSpeed(speed: 7.0f, minSpeed: 6.0f);

        // Assert
        Assert.True(result);
        Assert.Single(validationSystem.ValidationEvents);
        Assert.True(validationSystem.ValidationEvents[0].Passed);
    }

    /// <summary>
    /// Unit test: Validates that slide speed validation fails when too slow.
    /// </summary>
    [Fact]
    public void SlideSpeed_BelowMinimum_ShouldFail()
    {
        // Arrange
        var validationSystem = new ValidationSystemSimulator();

        // Act: Validate slide with speed below minimum
        var result = validationSystem.ValidateSlideSpeed(speed: 5.0f, minSpeed: 6.0f);

        // Assert
        Assert.False(result);
        Assert.Single(validationSystem.ValidationEvents);
        Assert.False(validationSystem.ValidationEvents[0].Passed);
    }

    /// <summary>
    /// Unit test: Validates that wall jump chain validation works correctly.
    /// </summary>
    [Fact]
    public void WallJumpChain_MeetsTarget_ShouldPass()
    {
        // Arrange
        var validationSystem = new ValidationSystemSimulator();

        // Act: Validate wall jump chain meeting target
        var result = validationSystem.ValidateWallJumpChain(jumpCount: 3, target: 3);

        // Assert
        Assert.True(result);
        Assert.Single(validationSystem.ValidationEvents);
        Assert.True(validationSystem.ValidationEvents[0].Passed);
    }

    /// <summary>
    /// Unit test: Validates that wall jump chain validation fails when insufficient.
    /// </summary>
    [Fact]
    public void WallJumpChain_BelowTarget_ShouldFail()
    {
        // Arrange
        var validationSystem = new ValidationSystemSimulator();

        // Act: Validate wall jump chain below target
        var result = validationSystem.ValidateWallJumpChain(jumpCount: 2, target: 3);

        // Assert
        Assert.False(result);
        Assert.Single(validationSystem.ValidationEvents);
        Assert.False(validationSystem.ValidationEvents[0].Passed);
    }

    /// <summary>
    /// Unit test: Validates that damage calculation validation works correctly.
    /// </summary>
    [Fact]
    public void DamageCalculation_WithinTolerance_ShouldPass()
    {
        // Arrange
        var validationSystem = new ValidationSystemSimulator();

        // Act: Validate damage calculation within tolerance
        var result = validationSystem.ValidateDamageCalculation(expected: 10.0f, actual: 10.005f, tolerance: 0.01f);

        // Assert
        Assert.True(result);
        Assert.Single(validationSystem.ValidationEvents);
        Assert.True(validationSystem.ValidationEvents[0].Passed);
    }

    /// <summary>
    /// Unit test: Validates that damage calculation validation fails when outside tolerance.
    /// </summary>
    [Fact]
    public void DamageCalculation_OutsideTolerance_ShouldFail()
    {
        // Arrange
        var validationSystem = new ValidationSystemSimulator();

        // Act: Validate damage calculation outside tolerance
        var result = validationSystem.ValidateDamageCalculation(expected: 10.0f, actual: 10.5f, tolerance: 0.01f);

        // Assert
        Assert.False(result);
        Assert.Single(validationSystem.ValidationEvents);
        Assert.False(validationSystem.ValidationEvents[0].Passed);
    }

    /// <summary>
    /// Unit test: Validates that friendly fire validation works correctly.
    /// </summary>
    [Fact]
    public void FriendlyFire_ZeroDamage_ShouldPass()
    {
        // Arrange
        var validationSystem = new ValidationSystemSimulator();

        // Act: Validate friendly fire with zero damage
        var result = validationSystem.ValidateFriendlyFire(damage: 0.0f);

        // Assert
        Assert.True(result);
        Assert.Single(validationSystem.ValidationEvents);
        Assert.True(validationSystem.ValidationEvents[0].Passed);
    }

    /// <summary>
    /// Unit test: Validates that friendly fire validation fails when damage is dealt.
    /// </summary>
    [Fact]
    public void FriendlyFire_NonZeroDamage_ShouldFail()
    {
        // Arrange
        var validationSystem = new ValidationSystemSimulator();

        // Act: Validate friendly fire with non-zero damage
        var result = validationSystem.ValidateFriendlyFire(damage: 5.0f);

        // Assert
        Assert.False(result);
        Assert.Single(validationSystem.ValidationEvents);
        Assert.False(validationSystem.ValidationEvents[0].Passed);
    }

    /// <summary>
    /// Property test: Validates that multiple validation events are all logged.
    /// </summary>
    [Property]
    public Property MultipleValidations_ShouldLogAllEvents(List<bool> conditions)
    {
        // Filter out empty lists
        if (conditions == null || conditions.Count == 0)
        {
            return true.ToProperty().Label("Skipped: Empty conditions list");
        }

        // Arrange
        var validationSystem = new ValidationSystemSimulator();

        // Act: Perform multiple validations
        foreach (var (condition, index) in conditions.Select((c, i) => (c, i)))
        {
            validationSystem.ValidateTestWithCriteria(
                $"test_{index}",
                condition,
                new Dictionary<string, object>(),
                "Test failed"
            );
        }

        // Assert: Number of validation events should match number of conditions
        var eventCountMatches = validationSystem.ValidationEvents.Count == conditions.Count;

        // Assert: Number of log entries should be at least the number of conditions
        // (each validation creates multiple log entries)
        var hasEnoughLogs = validationSystem.LogEntries.Count >= conditions.Count;

        return (eventCountMatches && hasEnoughLogs)
            .Label($"Conditions: {conditions.Count}, Events: {validationSystem.ValidationEvents.Count}, Logs: {validationSystem.LogEntries.Count}");
    }
}
